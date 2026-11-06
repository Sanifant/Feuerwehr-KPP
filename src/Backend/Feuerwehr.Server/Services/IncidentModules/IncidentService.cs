using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Implements incident operations for the API application layer.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    /// <param name="userContext">The accessor for the authenticated caller's identity and permissions.</param>
    /// <param name="idempotency">The command store that checks retries against their original payloads.</param>
    /// <param name="outboxService">The service that stages live events in the current database unit of work.</param>
    public sealed class IncidentService(FeuerwehrDbContext dbContext,
        IUserContextAccessor userContext,
        ICommandIdempotencyService idempotency,
        IOutboxService outboxService) : IIncidentService
    {

        /// <summary>
        /// Retrieves the current active incident, when one exists.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the active incident, when present, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken)
        {
            Incident? incident = await dbContext.Incidents
                .Where(x => x.Status == IncidentStatus.Active)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (incident is null)
            {
                return ServiceResult.NotFound();
            }

            return ServiceResult.Ok(ToSummary(incident));
        }

        /// <summary>
        /// Retrieves a page of closed incidents from the archive.
        /// </summary>
        /// <param name="page">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in a page.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the requested page of closed incidents, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            if (!userContext.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            List<IncidentSummaryDto> list = await dbContext.Incidents.AsNoTracking()
                .Where(x => x.Status == IncidentStatus.Closed)
                .OrderByDescending(x => x.ClosedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => ToSummary(x))
                .ToListAsync(cancellationToken);

            return ServiceResult.Ok(list);
        }

        /// <summary>
        /// Creates an active incident, records the command outcome, and stages its live update event.
        /// </summary>
        /// <param name="request">The create incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the created incident and its initial editing lease, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken)
        {
            if (request.AlarmedAtUtc is null || request.AlarmedAtUtc == DateTimeOffset.MinValue)
            {
                return ServiceResult.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bitte einen gültigen Alarmierungszeitpunkt angeben.");
            }

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
                AlarmedAtUtc = request.AlarmedAtUtc.Value.UtcDateTime,
                request.OriginLatitude,
                request.OriginLongitude,
                request.SessionId,
            };
            (ProcessedCommand? existingCommand, string? payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "incident.create", "active", payload, userContext.UserId, cancellationToken);
            if (existingCommand is not null)
            {
                return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
            }

            bool hasActive = await dbContext.Incidents.AnyAsync(x => x.Status == IncidentStatus.Active, cancellationToken);
            if (hasActive)
            {
                return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Es existiert bereits ein aktiver Einsatz.");
            }

            DateTime now = DateTime.UtcNow;
            Incident incident = new ()
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
                AlarmedAtUtc = request.AlarmedAtUtc.Value.UtcDateTime,
                Revision = 1,
            };

            EditorLease lease = new ()
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

            _ = dbContext.Incidents.Add(incident);
            _ = dbContext.EditorLeases.Add(lease);

            IncidentCreatedResponse response = new (ToSummary(incident), ToLeaseDto(lease), now);

            _ = dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "incident.create",
                "active",
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status201Created, response),
                userContext.UserId));

            outboxService.AddMessage("incident", "incident.created", incident.Id.ToString(), new IncidentChangedEvent(incident.Id, incident.Revision, now));
            outboxService.AddMessage("map", "map.lease.changed", incident.Id.ToString(), new InitialLeaseChangedEvent(incident.Id, userContext.DisplayName, lease.ExpiresAtUtc, now));

            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult.StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>
        /// Closes the active incident after checking its revision and records the command and live event consistently.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The close incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the closed incident and updated revision, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken)
        {

            var payload = new { request.LeaseToken, request.SessionId, incidentId };
            (ProcessedCommand? existingCommand, string? payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "incident.close", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
            if (existingCommand is not null)
            {
                return ServiceResult.StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
            }

            try
            {
                Incident? incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
                if (incident is null)
                {
                    return ServiceResult.NotFound();
                }

                if (incident.Status == IncidentStatus.Closed)
                {
                    return ServiceResult.Problem(statusCode: StatusCodes.Status409Conflict, title: "Der Einsatz ist bereits abgeschlossen.");
                }

                EditorLease? lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
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

                DateTime now = DateTime.UtcNow;
                incident.Status = IncidentStatus.Closed;
                incident.ClosedAtUtc = now;
                incident.ClosedByUserId = userContext.UserId;
                incident.ClosedByDisplayName = userContext.DisplayName;
                incident.Revision += 1;

                _ = dbContext.EditorLeases.Remove(lease);

                IncidentClosedResponse response = new (ToSummary(incident), now);
                _ = dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                    request.CommandId,
                    "incident.close",
                    incidentId.ToString(),
                    payloadHash,
                    ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                    userContext.UserId));

                outboxService.AddMessage("incident", "incident.closed", incident.Id.ToString(), new IncidentChangedEvent(incident.Id, incident.Revision, now));

                _ = await dbContext.SaveChangesAsync(cancellationToken);

                return ServiceResult.Ok(response);
            }
            catch
            {
                throw;
            }
        }

        private static IncidentSummaryDto ToSummary(Incident incident) => new (
            incident.Id,
            incident.Name,
            incident.IncidentNumber,
            incident.Keyword,
            incident.Description,
            incident.OriginLongitude,
            incident.OriginLatitude,
            incident.Status,
            incident.CreatedAtUtc,
            incident.AlarmedAtUtc,
            incident.ClosedAtUtc,
            incident.Revision);

        private static EditorLeaseDto ToLeaseDto(EditorLease lease) => new (
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
}
