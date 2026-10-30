using System.Globalization;
using System.Net;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Server.Services;

public interface IVehicleMailComposer
{
    VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request);
}

public sealed class VehicleMailComposer(IConfiguration configuration, TimeProvider clock) : IVehicleMailComposer
{
    public VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request)
    {
        var title = request ? "Neue MTW-Reservierungsanfrage" : "MTW-Buchung bestätigt";
        var dates = $"{reservation.From.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} bis einschließlich {reservation.To.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";
        var lines = $"Fahrzeug: {reservation.RadioCallSign}\nFeuerwehr: {reservation.FireDepartmentName}\n" +
            $"Anfragende Person: {reservation.RequesterName}\nZeitraum: {dates}\nBuchungsnummer: {reservation.Id}\n";
        if (reservation.Comment is not null) lines += $"\nKommentar / Buchungsgrund:\n{reservation.Comment}\n";
        var instruction = request ? "Die Anfrage ist unverbindlich. Bitte in der MTW-Übersicht prüfen und bestätigen oder ablehnen." :
            "Die Buchung ist verbindlich bestätigt.";
        var link = "";
        if (Uri.TryCreate(configuration["Frontend:BaseUrl"], UriKind.Absolute, out var baseUri) &&
            (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp) && string.IsNullOrEmpty(baseUri.Query) && string.IsNullOrEmpty(baseUri.Fragment))
            link = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), "vehicles").AbsoluteUri;
        static string Encode(string text) => WebUtility.HtmlEncode(text);
        var html = "<!doctype html><html lang=\"de\"><body style=\"margin:0;background:#f8fafc;font-family:Arial,sans-serif;color:#1f2937\">" +
            "<table role=\"presentation\" style=\"width:100%;max-width:640px;margin:24px auto;background:#fff;border:1px solid #cbd5e1\"><tr><td style=\"padding:24px;border-top:6px solid #b91c1c\">" +
            "<img src=\"cid:feuerwehr-logo\" width=\"80\" alt=\"Feuerwehr\"><h1 style=\"font-size:24px;color:#b91c1c\">" + Encode(title) + "</h1>" +
            "<p>" + Encode(instruction) + "</p><p style=\"line-height:1.7\">" + Encode(lines).Replace("\n", "<br>") + "</p>" +
            (link.Length == 0 ? "" : "<p><a style=\"color:#b91c1c\" href=\"" + Encode(link) + "\">MTW-Übersicht öffnen</a></p>") +
            "</td></tr></table></body></html>";
        var now = clock.GetUtcNow().UtcDateTime;
        return new VehicleMailMessage
        {
            Id = Guid.NewGuid(), ReservationId = reservation.Id, Kind = request ? "Requested" : "Confirmed", Recipient = recipient,
            Subject = $"{title}: {reservation.RadioCallSign}", TextBody = $"{title}\n\n{instruction}\n\n{lines}\n{link}",
            HtmlBody = html, CreatedAtUtc = now, NextAttemptAtUtc = now
        };
    }
}

public interface IVehicleMailDispatcher
{
    Task Dispatch(CancellationToken ct);
}

public sealed class VehicleMailDispatcher(FeuerwehrDbContext db, IHtmlMailService mail, TimeProvider clock,
    ILogger<VehicleMailDispatcher> logger) : IVehicleMailDispatcher
{
    public async Task Dispatch(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await db.VehicleMailMessages
            .Where(x => x.SentAtUtc == null && x.NextAttemptAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now))
            .OrderBy(x => x.CreatedAtUtc).Take(20).ToListAsync(ct);
        foreach (var message in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var leaseUntil = clock.GetUtcNow().UtcDateTime.AddMinutes(5);
            if (db.Database.IsRelational())
            {
                var claimed = await db.VehicleMailMessages.Where(x => x.Id == message.Id && x.SentAtUtc == null && x.NextAttemptAtUtc <= now &&
                    (x.LeaseUntilUtc == null || x.LeaseUntilUtc < now)).ExecuteUpdateAsync(update => update
                        .SetProperty(x => x.LeaseUntilUtc, leaseUntil).SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
                if (claimed == 0) continue;
            }
            else
            {
                message.LeaseUntilUtc = leaseUntil;
                message.Attempts++;
                db.VehicleMailMessages.Update(message);
                await db.SaveChangesAsync(ct);
            }
            DateTime? sent = null;
            string? error = null;
            try
            {
                await mail.SendHtmlAsync(message.Recipient, message.Subject, message.TextBody, message.HtmlBody);
                sent = clock.GetUtcNow().UtcDateTime;
            }
            catch (Exception exception) when (!ct.IsCancellationRequested)
            {
                // Store the exception type only: SMTP errors can contain recipient data.
                error = exception.GetType().Name;
                logger.LogWarning("MTW-E-Mail {MessageId} konnte nicht versandt werden ({Error}); erneuter Versuch folgt.", message.Id, error);
            }
            var next = clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts + 1, 6))));
            if (db.Database.IsRelational())
                await db.VehicleMailMessages.Where(x => x.Id == message.Id && x.LeaseUntilUtc == leaseUntil).ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.SentAtUtc, sent).SetProperty(x => x.LastError, error)
                    .SetProperty(x => x.NextAttemptAtUtc, next).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), ct);
            else
            {
                message.SentAtUtc = sent; message.LastError = error; message.NextAttemptAtUtc = next; message.LeaseUntilUtc = null;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}

public sealed class VehicleMailWorker(IServiceScopeFactory scopes, ILogger<VehicleMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IVehicleMailDispatcher>().Dispatch(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "MTW-Mailversand fehlgeschlagen."); }
            try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
