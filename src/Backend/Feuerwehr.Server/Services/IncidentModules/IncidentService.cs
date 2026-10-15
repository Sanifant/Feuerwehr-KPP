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

public sealed class IncidentService(FeuerwehrDbContext dbContext,
    IUserContextAccessor userContext,
    ICommandIdempotencyService idempotency,
    IOutboxService outboxService) : IIncidentService
{

    public async Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents
            .Where(x => x.Status == IncidentStatus.Active)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (incident is null)
        {
            return ServiceResult.NotFound();
        }

        return ServiceResult.Ok(ToSummary(incident));

    }

    public async Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await dbContext.Incidents
            .Where(x => x.Status == IncidentStatus.Closed)
            .OrderByDescending(x => x.ClosedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToSummary(x))
            .ToListAsync(cancellationToken);

        return ServiceResult.Ok(list);

    }

    public async Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken)
    {

        if (request.OriginLatitude is < -90 or > 90 || request.OriginLongitude is < -180 or > 180)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Ungültige Koordinaten.");
        }

        var payload = new
        {
            request.Name,
            request.IncidentNumber,
            request.Keyword,
            request.Description,
            request.OriginLatitude,
            request.OriginLongitude,
            request.SessionId,
        };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "incident.create", "active", payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        var hasActive = await dbContext.Incidents.AnyAsync(x => x.Status == IncidentStatus.Active, cancellationToken);
        if (hasActive)
        {
            return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Es existiert bereits ein aktiver Einsatz.");
        }

        var now = DateTime.UtcNow;
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IncidentNumber = request.IncidentNumber?.Trim(),
            Keyword = request.Keyword?.Trim(),
            Description = request.Description?.Trim(),
            OriginLatitude = request.OriginLatitude,
            OriginLongitude = request.OriginLongitude,
            Status = IncidentStatus.Active,
            CreatedAtUtc = now,
            Revision = 1,
        };

        var lease = new EditorLease
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            UserId = userContext.UserId,
            UserDisplayName = userContext.DisplayName,
            SessionId = request.SessionId,
            LeaseToken = Guid.NewGuid().ToString("N"),
            LastHeartbeatAtUtc = now,
            ExpiresAtUtc = now.Add(IncidentModuleTime.LeaseDuration),
        };

        dbContext.Incidents.Add(incident);
        dbContext.EditorLeases.Add(lease);

        var response = new IncidentCreatedResponse(ToSummary(incident), ToLeaseDto(lease), now);

        dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
            request.CommandId,
            "incident.create",
            "active",
            payloadHash,
            ResponseHelpers.BuildEnvelope(StatusCodes.Status201Created, response),
            userContext.UserId));

        outboxService.AddMessage("incident", "incident.created", incident.Id.ToString(), new IncidentChangedEvent(incident.Id, incident.Revision, now));
        outboxService.AddMessage("map", "map.lease.changed", incident.Id.ToString(), new InitialLeaseChangedEvent(incident.Id, userContext.DisplayName, lease.ExpiresAtUtc, now));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.StatusCode(StatusCodes.Status201Created);

    }

    public async Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken)
    {

        var payload = new { request.LeaseToken, request.SessionId, incidentId };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "incident.close", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        try
        {
            var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
            if (incident is null)
            {
                return ServiceResult.NotFound();
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Der Einsatz ist bereits abgeschlossen.");
            }

            var lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
            if (lease is null || lease.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Keine gültige Lagekarten-Sperre vorhanden.");
            }

            if (!string.Equals(lease.UserId, userContext.UserId, StringComparison.Ordinal)
                || !string.Equals(lease.SessionId, request.SessionId, StringComparison.Ordinal)
                || !string.Equals(lease.LeaseToken, request.LeaseToken, StringComparison.Ordinal))
            {
                return ServiceResult.Forbid();
            }

            var now = DateTime.UtcNow;
            incident.Status = IncidentStatus.Closed;
            incident.ClosedAtUtc = now;
            incident.ClosedByUserId = userContext.UserId;
            incident.ClosedByDisplayName = userContext.DisplayName;
            incident.Revision += 1;

            dbContext.EditorLeases.Remove(lease);

            var response = new IncidentClosedResponse(ToSummary(incident), now);
            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "incident.close",
                incidentId.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("incident", "incident.closed", incident.Id.ToString(), new IncidentChangedEvent(incident.Id, incident.Revision, now));

            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(response);
        }
        catch
        {
            throw;
        }

    }

    private static IncidentSummaryDto ToSummary(Incident incident) => new(
        incident.Id,
        incident.Name,
        incident.IncidentNumber,
        incident.Keyword,
        incident.Description,
        incident.OriginLongitude,
        incident.OriginLatitude,
        incident.Status,
        incident.CreatedAtUtc,
        incident.ClosedAtUtc,
        incident.Revision);

    private static EditorLeaseDto ToLeaseDto(EditorLease lease) => new(
        lease.IncidentId,
        lease.UserId,
        lease.UserDisplayName,
        lease.SessionId,
        lease.LeaseToken,
        lease.ExpiresAtUtc,
        lease.LastHeartbeatAtUtc,
        true,
        false);

}

public interface IIncidentService
{
    Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken);
    Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken);
}
