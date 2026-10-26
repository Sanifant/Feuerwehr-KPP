using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using System.Text.Json;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;

using de.openelp.feuerwehr.Server.Services;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules;

public sealed class IncidentDiaryService(FeuerwehrDbContext dbContext,
    IUserContextAccessor userContext,
    ICommandIdempotencyService idempotency,
    IOutboxService outboxService) : IIncidentDiaryService
{

    public async Task<ServiceResult> GetEntries(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null) return ServiceResult.NotFound();
        if (incident.Status == IncidentStatus.Closed && !userContext.IsAdmin) return ServiceResult.Forbid();

        var entries = await dbContext.DiaryEntries.AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.EntryNumber)
            .ToListAsync(cancellationToken);

        var entryIds = entries.Select(x => x.Id).ToList();
        var revisions = await dbContext.DiaryEntryRevisions.AsNoTracking()
            .Where(x => entryIds.Contains(x.DiaryEntryId))
            .OrderBy(x => x.RevisionNumber)
            .ToListAsync(cancellationToken);

        var grouped = revisions.GroupBy(x => x.DiaryEntryId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<DiaryRevisionDto>)x
                    .Select(ToRevisionDto)
                    .OrderBy(y => y.RevisionNumber)
                    .ToList());

        var response = entries.Select(e =>
        {
            var revs = grouped.GetValueOrDefault(e.Id, []);
            var current = revs.OrderByDescending(x => x.RevisionNumber).FirstOrDefault();
            if (current is null)
            {
                return null;
            }

            return new DiaryEntryDto(e.Id, e.IncidentId, e.EntryNumber, e.CurrentRevision, e.IsCanceled, e.CancellationReason, e.CreatedAtUtc, e.UpdatedAtUtc, current, revs);
        }).Where(x => x is not null).Cast<DiaryEntryDto>().ToList();

        return ServiceResult.Ok(response);

    }

    public async Task<ServiceResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await dbContext.DiaryCategories.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DiaryCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return ServiceResult.Ok(categories);

    }

    public async Task<ServiceResult> CreateEntry(Guid incidentId, CreateDiaryEntryRequest request, CancellationToken cancellationToken)
    {

        var payload = new { incidentId, request.EventTimestampUtc, request.CategoryCode, request.Text, request.Sender, request.Recipient, request.TransmissionType };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "diary.entry.create", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return ServiceResult.NotFound();
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist abgeschlossen.");
        }

        var category = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == request.CategoryCode, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Kategorie ist nicht verfügbar.");
        }

        var now = DateTime.UtcNow;
        var nextNumber = await dbContext.Database.SqlQuery<long>($"SELECT nextval('\"DiaryEntryNumberSeq\"') AS \"Value\"").SingleAsync(cancellationToken);

        var entry = new DiaryEntry
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            EntryNumber = nextNumber,
            CurrentRevision = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var revision = new DiaryEntryRevision
        {
            Id = Guid.NewGuid(),
            DiaryEntryId = entry.Id,
            RevisionNumber = 1,
            RevisionType = DiaryRevisionType.Created,
            EventTimestampUtc = request.EventTimestampUtc,
            CategoryCode = category.Code,
            CategoryNameSnapshot = category.Name,
            Text = request.Text,
            Sender = request.Sender,
            Recipient = request.Recipient,
            TransmissionType = request.TransmissionType,
            CreatedAtUtc = now,
            CreatedByUserId = userContext.UserId,
            CreatedByDisplayName = userContext.DisplayName,
        };

        dbContext.DiaryEntries.Add(entry);
        dbContext.DiaryEntryRevisions.Add(revision);
        incident.Revision += 1;

        var response = new DiaryEntryCreatedResponse(entry.Id, entry.EntryNumber, entry.CurrentRevision, incident.Revision, now);

        dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
            request.CommandId,
            "diary.entry.create",
            entry.Id.ToString(),
            payloadHash,
            ResponseHelpers.BuildEnvelope(StatusCodes.Status201Created, response),
            userContext.UserId));

        outboxService.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), new DiaryEntryChangedEvent(incidentId, entry.Id, entry.EntryNumber, "create", incident.Revision, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.StatusCode(StatusCodes.Status201Created, response);

    }

    public async Task<ServiceResult> UpdateEntry(Guid incidentId, Guid entryId, UpdateDiaryEntryRequest request, CancellationToken cancellationToken)
    {

        var payload = new { incidentId, entryId, request.ExpectedRevision, request.EventTimestampUtc, request.CategoryCode, request.Text, request.Sender, request.Recipient, request.TransmissionType };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "diary.entry.update", entryId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return ServiceResult.NotFound();
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist abgeschlossen.");
        }

        var entry = await dbContext.DiaryEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.IncidentId == incidentId, cancellationToken);
        if (entry is null)
        {
            return ServiceResult.NotFound();
        }

        if (entry.CurrentRevision != request.ExpectedRevision)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Konflikt: veraltete Tagebuchversion.");
        }

        var category = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == request.CategoryCode, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Kategorie ist nicht verfügbar.");
        }

        var now = DateTime.UtcNow;
        entry.CurrentRevision += 1;
        entry.UpdatedAtUtc = now;

        dbContext.DiaryEntryRevisions.Add(new DiaryEntryRevision
        {
            Id = Guid.NewGuid(),
            DiaryEntryId = entry.Id,
            RevisionNumber = entry.CurrentRevision,
            RevisionType = DiaryRevisionType.Edited,
            EventTimestampUtc = request.EventTimestampUtc,
            CategoryCode = category.Code,
            CategoryNameSnapshot = category.Name,
            Text = request.Text,
            Sender = request.Sender,
            Recipient = request.Recipient,
            TransmissionType = request.TransmissionType,
            CreatedAtUtc = now,
            CreatedByUserId = userContext.UserId,
            CreatedByDisplayName = userContext.DisplayName,
        });

        incident.Revision += 1;
        var response = new DiaryEntryUpdatedResponse(entry.Id, entry.CurrentRevision, incident.Revision, now);

        dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
            request.CommandId,
            "diary.entry.update",
            entry.Id.ToString(),
            payloadHash,
            ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
            userContext.UserId));

        outboxService.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), new DiaryEntryChangedEvent(incidentId, entry.Id, entry.EntryNumber, "update", incident.Revision, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(response);

    }

    public async Task<ServiceResult> CancelEntry(Guid incidentId, Guid entryId, CancelDiaryEntryRequest request, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Stornierungsbegründung ist erforderlich.");
        }

        var payload = new { incidentId, entryId, request.ExpectedRevision, request.Reason };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "diary.entry.cancel", entryId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return ServiceResult.NotFound();
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist abgeschlossen.");
        }

        var entry = await dbContext.DiaryEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.IncidentId == incidentId, cancellationToken);
        if (entry is null)
        {
            return ServiceResult.NotFound();
        }

        if (entry.CurrentRevision != request.ExpectedRevision)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Konflikt: veraltete Tagebuchversion.");
        }

        var lastRevision = await dbContext.DiaryEntryRevisions
            .Where(x => x.DiaryEntryId == entry.Id)
            .OrderByDescending(x => x.RevisionNumber)
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        entry.CurrentRevision += 1;
        entry.UpdatedAtUtc = now;
        entry.IsCanceled = true;
        entry.CancellationReason = request.Reason.Trim();

        dbContext.DiaryEntryRevisions.Add(new DiaryEntryRevision
        {
            Id = Guid.NewGuid(),
            DiaryEntryId = entry.Id,
            RevisionNumber = entry.CurrentRevision,
            RevisionType = DiaryRevisionType.Canceled,
            EventTimestampUtc = lastRevision.EventTimestampUtc,
            CategoryCode = lastRevision.CategoryCode,
            CategoryNameSnapshot = lastRevision.CategoryNameSnapshot,
            Text = lastRevision.Text,
            Sender = lastRevision.Sender,
            Recipient = lastRevision.Recipient,
            TransmissionType = lastRevision.TransmissionType,
            CancellationReason = request.Reason.Trim(),
            CreatedAtUtc = now,
            CreatedByUserId = userContext.UserId,
            CreatedByDisplayName = userContext.DisplayName,
        });

        incident.Revision += 1;

        var response = new DiaryEntryCanceledResponse(entry.Id, entry.CurrentRevision, true, incident.Revision, now);
        dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
            request.CommandId,
            "diary.entry.cancel",
            entry.Id.ToString(),
            payloadHash,
            ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
            userContext.UserId));

        outboxService.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), new DiaryEntryChangedEvent(incidentId, entry.Id, entry.EntryNumber, "cancel", incident.Revision, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(response);

    }

    public async Task<ServiceResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return ServiceResult.NotFound();
        }

        if (incident.Status == IncidentStatus.Closed && !userContext.IsAdmin) return ServiceResult.Forbid();

        var diary = await GetEntries(incidentId, cancellationToken);
        if (diary.Code != StatusCodes.Status200OK) return diary;
        var entries = (IReadOnlyCollection<DiaryEntryDto>)diary.Value!;

        var lines = new List<string>
        {
            $"Einsatz: {incident.Name}",
            $"Einsatznummer: {incident.IncidentNumber ?? "-"}",
            $"Stichwort: {incident.Keyword ?? "-"}",
            $"Alarmierung UTC: {incident.AlarmedAtUtc:dd.MM.yyyy HH:mm:ss}",
            $"Angelegt UTC: {incident.CreatedAtUtc:dd.MM.yyyy HH:mm:ss}",
            $"Beendet UTC: {incident.ClosedAtUtc:dd.MM.yyyy HH:mm:ss}",
            $"Beendet durch: {incident.ClosedByDisplayName ?? "-"}",
            $"Beschreibung: {incident.Description ?? "-"}",
            $"Exportiert UTC: {DateTime.UtcNow:O}",
            ""
        };
        foreach (var entry in entries)
        {
            var revision = entry.CurrentRevisionData;
            var marker = revision.RevisionType == DiaryRevisionType.Canceled ? " [STORNIERT]" : revision.RevisionType == DiaryRevisionType.Edited ? " [KORRIGIERT]" : string.Empty;
            lines.Add($"Eintrag #{entry.EntryNumber}{marker}");
            AddRevisionLines(lines, revision);
            if (entry.Revisions.Count > 1)
            {
                lines.Add("Versionshistorie:");
                foreach (var previous in entry.Revisions.Where(x => x.RevisionNumber != entry.CurrentRevision))
                    AddRevisionLines(lines, previous);
            }
            lines.Add("");
        }
        if (entries.Count == 0) lines.Add("Keine Tagebucheinträge vorhanden.");

        var pdfBytes = MinimalPdfBuilder.BuildSimplePdf("Einsatztagebuch", lines);
        return ServiceResult.File(pdfBytes, "application/pdf", $"einsatztagebuch-{incidentId:N}.pdf");

    }

    private static void AddRevisionLines(List<string> lines, DiaryRevisionDto revision)
    {
        lines.Add($"Version {revision.RevisionNumber} | Ereignis UTC: {revision.EventTimestampUtc:dd.MM.yyyy HH:mm:ss}");
        lines.Add($"Kategorie: {revision.CategoryNameSnapshot} | Übermittlung: {revision.TransmissionType ?? "-"}");
        lines.Add($"Absender: {revision.Sender ?? "-"} | Empfänger: {revision.Recipient ?? "-"}");
        lines.Add(revision.Text);
        if (!string.IsNullOrWhiteSpace(revision.CancellationReason)) lines.Add($"Grund: {revision.CancellationReason}");
        lines.Add($"Erfasst UTC: {revision.CreatedAtUtc:dd.MM.yyyy HH:mm:ss} durch {revision.CreatedByDisplayName}");
    }

    private static DiaryRevisionDto ToRevisionDto(DiaryEntryRevision x)
        => new(
            x.Id,
            x.RevisionNumber,
            x.RevisionType,
            x.EventTimestampUtc,
            x.CategoryCode,
            x.CategoryNameSnapshot,
            x.Text,
            x.Sender,
            x.Recipient,
            x.TransmissionType,
            x.CancellationReason,
            x.CreatedAtUtc,
            x.CreatedByDisplayName);

}

public interface IIncidentDiaryService
{
    Task<ServiceResult> GetEntries(Guid incidentId, CancellationToken cancellationToken);
    Task<ServiceResult> GetCategories(CancellationToken cancellationToken);
    Task<ServiceResult> CreateEntry(Guid incidentId, CreateDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateEntry(Guid incidentId, Guid entryId, UpdateDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> CancelEntry(Guid incidentId, Guid entryId, CancelDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken);
}
