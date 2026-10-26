using de.openelp.feuerwehr.Common.Models.IncidentModules;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Hubs;
using de.openelp.feuerwehr.Server.Models.IncidentModules;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules;

public static class IncidentModuleTime
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
}

public static class JsonOptionsFactory
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

public interface IUserContextAccessor
{
    string UserId { get; }
    string DisplayName { get; }
    bool IsAdmin { get; }
}

public sealed class HttpUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
{
    public string UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public string DisplayName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Unbekannt";

    public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(Authorization.Roles.Admin) == true;
}

public interface ICommandIdempotencyService
{
    Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken);
    ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId);
}

public sealed class CommandIdempotencyService(FeuerwehrDbContext dbContext) : ICommandIdempotencyService
{
    public async Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));

        var existing = await dbContext.ProcessedCommands
            .FirstOrDefaultAsync(x => x.CommandId == commandId, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.UserId, userId, StringComparison.Ordinal) ||
                !string.Equals(existing.Scope, scope, StringComparison.Ordinal) ||
                !string.Equals(existing.TargetId ?? string.Empty, targetId ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CommandId wurde bereits mit anderer Nutzlast oder anderem Benutzer verwendet.");
            }
        }

        return (existing, hash);
    }

    public ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId)
    {
        return new ProcessedCommand
        {
            Id = Guid.NewGuid(),
            CommandId = commandId,
            UserId = userId,
            Scope = scope,
            TargetId = targetId,
            PayloadHash = payloadHash,
            ResponseStatusCode = response.StatusCode,
            ResponseJson = response.JsonPayload,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

public interface IOutboxService
{
    void AddMessage(string aggregateType, string eventType, string aggregateId, object payload);
}

public sealed class OutboxService(FeuerwehrDbContext dbContext) : IOutboxService
{
    public void AddMessage(string aggregateType, string eventType, string aggregateId, object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            EventType = eventType,
            AggregateId = aggregateId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default),
            CreatedAtUtc = DateTime.UtcNow,
        });
    }
}

public static class ResponseHelpers
{
    public static CommandEnvelopeResponse BuildEnvelope(int statusCode, object payload)
    {
        return new CommandEnvelopeResponse
        {
            StatusCode = statusCode,
            JsonPayload = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default),
        };
    }
}

public interface IMapLeaseService
{
    Task<EditorLease> RequireValidLeaseAsync(Guid incidentId, string sessionId, string leaseToken, string userId, CancellationToken cancellationToken);
}

public sealed class MapLeaseService(FeuerwehrDbContext dbContext) : IMapLeaseService
{
    public async Task<EditorLease> RequireValidLeaseAsync(Guid incidentId, string sessionId, string leaseToken, string userId, CancellationToken cancellationToken)
    {
        var lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken)
                    ?? throw new InvalidOperationException("Keine aktive Bearbeitungssperre vorhanden.");

        var now = DateTime.UtcNow;
        if (lease.ExpiresAtUtc <= now)
        {
            throw new InvalidOperationException("Die Bearbeitungssperre ist abgelaufen.");
        }

        if (!string.Equals(lease.UserId, userId, StringComparison.Ordinal)
            || !string.Equals(lease.SessionId, sessionId, StringComparison.Ordinal)
            || !string.Equals(lease.LeaseToken, leaseToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Diese Sitzung besitzt keine gültige Bearbeitungssperre.");
        }

        return lease;
    }
}

public sealed class OutboxDispatcher(
    IServiceProvider serviceProvider,
    ILogger<OutboxDispatcher> logger,
    IHubContext<IncidentHub, IIncidentClient> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();

                var batch = await db.OutboxMessages
                    .Where(x => x.ProcessedAtUtc == null)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in batch)
                {
                    try
                    {
                        await PublishMessageAsync(message, stoppingToken);
                        message.ProcessedAtUtc = DateTime.UtcNow;
                        message.DeliveryAttempts += 1;
                        message.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        message.DeliveryAttempts += 1;
                        message.LastError = ex.Message;
                        logger.LogWarning(ex, "Outbox dispatch failed for {OutboxId}", message.Id);
                    }
                }

                if (batch.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.PayloadJson);
        var payload = doc.RootElement.Clone();

        switch (message.AggregateType)
        {
            case "incident":
                await hubContext.Clients.Group($"incident:{message.AggregateId}").IncidentUpdated(payload);
                break;
            case "map":
                await hubContext.Clients.Group($"map:{message.AggregateId}").MapUpdated(payload);
                break;
            case "diary":
                await hubContext.Clients.Group($"diary:{message.AggregateId}").DiaryUpdated(payload);
                break;
            default:
                await hubContext.Clients.Group($"incident:{message.AggregateId}").IncidentUpdated(payload);
                break;
        }

        _ = cancellationToken;
    }
}

public static class CsvExportHelpers
{
    public static string SanitizeCsvCell(string? value)
    {
        var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        if (normalized.StartsWith("=") || normalized.StartsWith("+") || normalized.StartsWith("-") || normalized.StartsWith("@"))
        {
            normalized = $"'{normalized}";
        }

        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }

    public static byte[] BuildMapAuditCsv(IEnumerable<MapAuditEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sequence;OccurredAtUtc;Action;User;ElementId;CommandId;Before;After");
        foreach (var e in events)
        {
            sb.AppendLine(string.Join(';',
                e.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                SanitizeCsvCell(e.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                SanitizeCsvCell(e.Action),
                SanitizeCsvCell(e.UserDisplayName),
                SanitizeCsvCell(e.ElementId?.ToString()),
                SanitizeCsvCell(e.CommandId),
                SanitizeCsvCell(e.BeforeStateJson),
                SanitizeCsvCell(e.AfterStateJson)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

public static class MinimalPdfBuilder
{
    public static byte[] BuildSimplePdf(string title, IEnumerable<string> lines)
    {
        var pages = Paginate(lines.SelectMany(WrapLine));
        var fontId = 3 + pages.Count * 2;
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        void WriteObject(int id, string obj)
        {
            writer.Flush();
            offsets.Add(ms.Position);
            writer.Write($"{id} 0 obj\n{obj}\nendobj\n");
        }

        WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        var pageReferences = string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{3 + index * 2} 0 R"));
        WriteObject(2, $"<< /Type /Pages /Kids [{pageReferences}] /Count {pages.Count} >>");
        for (var index = 0; index < pages.Count; index++)
        {
            var pageId = 3 + index * 2;
            var contentId = pageId + 1;
            var content = new StringBuilder();
            content.Append($"BT /F1 16 Tf 50 790 Td ({Escape(title)}) Tj ET\n");
            var y = 760;
            foreach (var line in pages[index])
            {
                content.Append($"BT /F1 9.5 Tf 50 {y} Td ({Escape(line)}) Tj ET\n");
                y -= 12;
            }
            content.Append($"BT /F1 9 Tf 50 32 Td (Seite {index + 1} / {pages.Count}) Tj ET\n");
            var contentText = content.ToString();
            WriteObject(pageId, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents {contentId} 0 R /Resources << /Font << /F1 {fontId} 0 R >> >> >>");
            WriteObject(contentId, $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream");
        }
        WriteObject(fontId, "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");

        writer.Flush();
        var xrefPos = ms.Position;
        writer.Write($"xref\n0 {offsets.Count}\n");
        writer.Write("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            writer.Write($"{offsets[i]:D10} 00000 n \n");
        }

        writer.Write($"trailer << /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
        writer.Flush();

        return ms.ToArray();
    }

    private static List<string[]> Paginate(IEnumerable<string> lines)
    {
        var pages = new List<string[]>();
        var page = new List<string>();
        var block = new List<string>();
        void FlushBlock(bool separator)
        {
            // Keep an entry together when it fits on one page; long entries still continue.
            if (block.Count <= 56 && page.Count + block.Count > 56)
            {
                pages.Add(page.ToArray());
                page.Clear();
            }
            foreach (var line in block)
            {
                if (page.Count == 56)
                {
                    pages.Add(page.ToArray());
                    page.Clear();
                }
                page.Add(line);
            }
            block.Clear();
            if (separator && page.Count is > 0 and < 56 && page[^1] != "") page.Add("");
        }
        foreach (var line in lines)
        {
            if (line.Length == 0) FlushBlock(true);
            else block.Add(line);
        }
        FlushBlock(false);
        if (page.Count > 0) pages.Add(page.ToArray());
        if (pages.Count == 0) pages.Add([]);
        return pages;
    }

    private static IEnumerable<string> WrapLine(string input)
    {
        foreach (var paragraph in (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ").Split('\n'))
        {
            var remaining = paragraph;
            while (remaining.Length > 86)
            {
                var split = remaining.LastIndexOf(' ', 86, 87);
                if (split <= 0) split = 86;
                yield return remaining[..split];
                remaining = remaining[split..].TrimStart();
            }
            yield return remaining;
        }
    }

    private static string Escape(string input)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1252).GetBytes(input);
        var result = new StringBuilder();
        foreach (var value in bytes)
        {
            if (value is (byte)'\\' or (byte)'(' or (byte)')') result.Append('\\').Append((char)value);
            else if (value is < 32 or > 126) result.Append('\\').Append(Convert.ToString(value, 8).PadLeft(3, '0'));
            else result.Append((char)value);
        }
        return result.ToString();
    }
}
