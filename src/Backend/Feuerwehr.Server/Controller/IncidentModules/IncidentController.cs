using System.Text.Json;
using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models.IncidentModules;
using Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents")]
[ApiController]
[Authorize]
public class IncidentController(
    FeuerwehrDbContext dbContext,
    IUserContextAccessor userContext,
    ICommandIdempotencyService idempotency,
    IOutboxService outboxService) : ControllerBase
{
    [HttpGet("active")]
    [Authorize(Policy = PolicyNames.CanAccessIncidentModule)]
    public async Task<ActionResult<IncidentSummaryDto>> GetActiveIncident(CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents
            .Where(x => x.Status == IncidentStatus.Active)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        return Ok(ToSummary(incident));
    }

    [HttpGet("archive")]
    [Authorize(Policy = PolicyNames.CanAccessIncidentModule)]
    public async Task<ActionResult<IReadOnlyCollection<IncidentSummaryDto>>> GetArchive([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
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

        return Ok(list);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.OriginLatitude is < -90 or > 90 || request.OriginLongitude is < -180 or > 180)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Ungültige Koordinaten.");
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
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var hasActive = await dbContext.Incidents.AnyAsync(x => x.Status == IncidentStatus.Active, cancellationToken);
            if (hasActive)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Es existiert bereits ein aktiver Einsatz.");
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

            var response = new
            {
                incident = ToSummary(incident),
                lease = ToLeaseDto(lease),
                serverUtc = now,
            };

            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "incident.create",
                "active",
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status201Created, response),
                userContext.UserId));

            outboxService.AddMessage("incident", "incident.created", incident.Id.ToString(), new { incidentId = incident.Id, revision = incident.Revision, serverUtc = now });
            outboxService.AddMessage("map", "map.lease.changed", incident.Id.ToString(), new { incidentId = incident.Id, leaseOwner = userContext.DisplayName, expiresAtUtc = lease.ExpiresAtUtc, serverUtc = now });

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Es existiert bereits ein aktiver Einsatz.");
        }
    }

    [HttpPost("{incidentId:guid}/close")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> CloseIncident(Guid incidentId, [FromBody] CloseIncidentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var payload = new { request.LeaseToken, request.SessionId, incidentId };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "incident.close", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
            if (incident is null)
            {
                return NotFound();
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Der Einsatz ist bereits abgeschlossen.");
            }

            var lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
            if (lease is null || lease.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Keine gültige Lagekarten-Sperre vorhanden.");
            }

            if (!string.Equals(lease.UserId, userContext.UserId, StringComparison.Ordinal)
                || !string.Equals(lease.SessionId, request.SessionId, StringComparison.Ordinal)
                || !string.Equals(lease.LeaseToken, request.LeaseToken, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var now = DateTime.UtcNow;
            incident.Status = IncidentStatus.Closed;
            incident.ClosedAtUtc = now;
            incident.ClosedByUserId = userContext.UserId;
            incident.ClosedByDisplayName = userContext.DisplayName;
            incident.Revision += 1;

            dbContext.EditorLeases.Remove(lease);

            var response = new { incident = ToSummary(incident), serverUtc = now };
            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "incident.close",
                incidentId.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("incident", "incident.closed", incident.Id.ToString(), new { incidentId = incident.Id, revision = incident.Revision, serverUtc = now });

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(response);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
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
