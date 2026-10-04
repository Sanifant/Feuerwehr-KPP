using System.Text.Json;
using Feuerwehr.Common.Models;
using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models.IncidentModules;
using Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents/{incidentId:guid}/map")]
[ApiController]
[Authorize]
public class SituationMapController(
    FeuerwehrDbContext dbContext,
    IUserContextAccessor userContext,
    ICommandIdempotencyService idempotency,
    IOutboxService outboxService,
    IMapLeaseService leaseService) : ControllerBase
{
    [HttpGet("state")]
    [Authorize(Policy = PolicyNames.CanViewSituationMap)]
    public async Task<IActionResult> GetState(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var elements = await dbContext.MapElements.AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(ToMapElementDtoExpression)
            .ToListAsync(cancellationToken);

        var lease = await dbContext.EditorLeases.AsNoTracking().FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
        var now = DateTime.UtcNow;

        var leaseDto = lease is null
            ? null
            : new EditorLeaseDto(
                lease.IncidentId,
                lease.UserId,
                lease.UserDisplayName,
                lease.SessionId,
                lease.LeaseToken,
                lease.ExpiresAtUtc,
                lease.LastHeartbeatAtUtc,
                lease.UserId == userContext.UserId,
                lease.ExpiresAtUtc <= now);

        return Ok(new
        {
            incidentId,
            incidentStatus = incident.Status,
            revision = incident.Revision,
            serverUtc = now,
            lease = leaseDto,
            elements,
            hydrants = await dbContext.Hydrants.AsNoTracking().Select(h => new { h.Id, h.Longitude, h.Latitude, h.Status, h.NominalDiameter }).ToListAsync(cancellationToken),
        });
    }

    [HttpPost("lease/acquire")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> AcquireLease(Guid incidentId, [FromBody] AcquireLeaseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var payload = new { incidentId, request.SessionId };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "map.lease.acquire", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        if (incident.Status == IncidentStatus.Closed)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist bereits abgeschlossen.");
        }

        var now = DateTime.UtcNow;
        var lease = await dbContext.EditorLeases.FirstOrDefaultAsync(x => x.IncidentId == incidentId, cancellationToken);
        if (lease is not null && lease.ExpiresAtUtc > now)
        {
            if (lease.UserId == userContext.UserId && lease.SessionId == request.SessionId)
            {
                var ownLeaseResponse = new { lease = ToLeaseDto(lease, now), serverUtc = now };
                dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                    request.CommandId,
                    "map.lease.acquire",
                    incidentId.ToString(),
                    payloadHash,
                    ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, ownLeaseResponse),
                    userContext.UserId));
                await dbContext.SaveChangesAsync(cancellationToken);
                return Ok(ownLeaseResponse);
            }

            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Lagekarte wird bereits bearbeitet.", detail: $"Aktiv: {lease.UserDisplayName} bis {lease.ExpiresAtUtc:O}");
        }

        if (lease is null)
        {
            lease = new EditorLease
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
            };
            dbContext.EditorLeases.Add(lease);
        }

        lease.UserId = userContext.UserId;
        lease.UserDisplayName = userContext.DisplayName;
        lease.SessionId = request.SessionId;
        lease.LeaseToken = Guid.NewGuid().ToString("N");
        lease.LastHeartbeatAtUtc = now;
        lease.ExpiresAtUtc = now.Add(IncidentModuleTime.LeaseDuration);

        var response = new { lease = ToLeaseDto(lease, now), serverUtc = now };

        dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
            request.CommandId,
            "map.lease.acquire",
            incidentId.ToString(),
            payloadHash,
            ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
            userContext.UserId));

        outboxService.AddMessage("map", "map.lease.changed", incidentId.ToString(), new
        {
            incidentId,
            leaseOwner = lease.UserDisplayName,
            leaseSessionId = lease.SessionId,
            expiresAtUtc = lease.ExpiresAtUtc,
            serverUtc = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("lease/heartbeat")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> Heartbeat(Guid incidentId, [FromBody] LeaseHeartbeatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var payload = new { incidentId, request.SessionId, request.LeaseToken };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "map.lease.heartbeat", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        try
        {
            var lease = await leaseService.RequireValidLeaseAsync(incidentId, request.SessionId, request.LeaseToken, userContext.UserId, cancellationToken);
            var now = DateTime.UtcNow;
            lease.LastHeartbeatAtUtc = now;
            lease.ExpiresAtUtc = now.Add(IncidentModuleTime.LeaseDuration);

            var response = new { lease = ToLeaseDto(lease, now), serverUtc = now };
            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "map.lease.heartbeat",
                incidentId.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("map", "map.lease.changed", incidentId.ToString(), new
            {
                incidentId,
                leaseOwner = lease.UserDisplayName,
                leaseSessionId = lease.SessionId,
                expiresAtUtc = lease.ExpiresAtUtc,
                serverUtc = now,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPost("lease/release")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> Release(Guid incidentId, [FromBody] ReleaseLeaseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var payload = new { incidentId, request.SessionId, request.LeaseToken };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "map.lease.release", incidentId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        try
        {
            var lease = await leaseService.RequireValidLeaseAsync(incidentId, request.SessionId, request.LeaseToken, userContext.UserId, cancellationToken);
            dbContext.EditorLeases.Remove(lease);
            var response = new { released = true, serverUtc = DateTime.UtcNow };

            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "map.lease.release",
                incidentId.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("map", "map.lease.changed", incidentId.ToString(), new
            {
                incidentId,
                leaseOwner = (string?)null,
                leaseSessionId = (string?)null,
                expiresAtUtc = (DateTime?)null,
                serverUtc = DateTime.UtcNow,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPost("elements")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> UpsertElement(Guid incidentId, [FromBody] UpsertMapElementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryValidateGeometry(request.GeometryJson, request.ElementType, out var geoError))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: geoError);
        }

        var payload = new
        {
            incidentId,
            request.ElementId,
            request.ElementType,
            request.SymbolId,
            request.Category,
            request.Label,
            request.RadioCallName,
            request.Strength,
            request.Note,
            request.ColorHex,
            request.GeometryJson,
            request.ExpectedVersion,
            request.SessionId,
            request.LeaseToken,
        };

        var targetId = request.ElementId?.ToString() ?? "new";
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "map.element.upsert", targetId, payload, userContext.UserId, cancellationToken);
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
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist abgeschlossen.");
            }

            await leaseService.RequireValidLeaseAsync(incidentId, request.SessionId, request.LeaseToken, userContext.UserId, cancellationToken);

            var now = DateTime.UtcNow;
            var isCreate = request.ElementId is null || request.ElementId == Guid.Empty;

            MapElement element;
            MapElement? beforeSnapshot = null;
            string action;

            if (isCreate)
            {
                if (request.ElementType == MapElementType.Symbol && string.IsNullOrWhiteSpace(request.SymbolId))
                {
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Für Symbol-Elemente ist symbolId erforderlich.");
                }

                element = new MapElement
                {
                    Id = Guid.NewGuid(),
                    IncidentId = incidentId,
                    ElementType = request.ElementType,
                    SymbolId = request.SymbolId,
                    Category = request.Category,
                    Label = request.Label,
                    RadioCallName = request.RadioCallName,
                    Strength = request.Strength,
                    Note = request.Note,
                    ColorHex = string.IsNullOrWhiteSpace(request.ColorHex) ? "#d62828" : request.ColorHex,
                    GeometryJson = request.GeometryJson,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    UpdatedByUserId = userContext.UserId,
                    UpdatedByDisplayName = userContext.DisplayName,
                };
                dbContext.MapElements.Add(element);
                action = "create";
            }
            else
            {
                element = await dbContext.MapElements.FirstOrDefaultAsync(x => x.Id == request.ElementId && x.IncidentId == incidentId, cancellationToken)
                    ?? throw new InvalidOperationException("Element nicht gefunden.");

                beforeSnapshot = Clone(element);
                if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != element.Version)
                {
                    return Problem(statusCode: StatusCodes.Status409Conflict, title: "Konflikt: veraltete Elementversion.");
                }

                var changed = false;
                changed |= SetValue(element.SymbolId, request.SymbolId);
                changed |= SetValue(element.Category, request.Category);
                changed |= SetValue(element.Label, request.Label);
                changed |= SetValue(element.RadioCallName, request.RadioCallName);
                changed |= SetValue(element.Strength, request.Strength);
                changed |= SetValue(element.Note, request.Note);
                changed |= SetValue(element.ColorHex, string.IsNullOrWhiteSpace(request.ColorHex) ? "#d62828" : request.ColorHex);
                changed |= SetValue(element.GeometryJson, request.GeometryJson);

                if (!changed)
                {
                    return Ok(new { element = ToMapElementDto(element), unchanged = true, serverUtc = now });
                }

                element.Version += 1;
                element.UpdatedAtUtc = now;
                element.UpdatedByUserId = userContext.UserId;
                element.UpdatedByDisplayName = userContext.DisplayName;
                action = "update";
            }

            incident.Revision += 1;

            dbContext.MapAuditEvents.Add(new MapAuditEvent
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                CommandId = request.CommandId,
                ElementId = element.Id,
                Action = action,
                UserId = userContext.UserId,
                UserDisplayName = userContext.DisplayName,
                OccurredAtUtc = now,
                BeforeStateJson = JsonSerializer.Serialize(beforeSnapshot is null ? null : ToMapElementDto(beforeSnapshot), JsonOptionsFactory.Default),
                AfterStateJson = JsonSerializer.Serialize(ToMapElementDto(element), JsonOptionsFactory.Default),
            });

            var response = new { element = ToMapElementDto(element), incidentRevision = incident.Revision, serverUtc = now };
            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "map.element.upsert",
                element.Id.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("map", "map.element.changed", incidentId.ToString(), new { incidentId, incidentRevision = incident.Revision, elementId = element.Id, action, serverUtc = now });

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpDelete("elements/{elementId:guid}")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> DeleteElement(Guid incidentId, Guid elementId, [FromBody] DeleteMapElementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var payload = new { incidentId, elementId, request.ExpectedVersion, request.SessionId, request.LeaseToken };
        var (existingCommand, payloadHash) = await idempotency.FindExistingAsync(request.CommandId, "map.element.delete", elementId.ToString(), payload, userContext.UserId, cancellationToken);
        if (existingCommand is not null)
        {
            return StatusCode(existingCommand.ResponseStatusCode, JsonDocument.Parse(existingCommand.ResponseJson).RootElement.Clone());
        }

        try
        {
            var incident = await dbContext.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
            if (incident is null)
            {
                return NotFound();
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Einsatz ist abgeschlossen.");
            }

            await leaseService.RequireValidLeaseAsync(incidentId, request.SessionId, request.LeaseToken, userContext.UserId, cancellationToken);

            var element = await dbContext.MapElements.FirstOrDefaultAsync(x => x.Id == elementId && x.IncidentId == incidentId, cancellationToken)
                ?? throw new InvalidOperationException("Element nicht gefunden.");

            if (request.ExpectedVersion != element.Version)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Konflikt: veraltete Elementversion.");
            }

            var now = DateTime.UtcNow;
            var before = ToMapElementDto(element);
            dbContext.MapElements.Remove(element);
            incident.Revision += 1;

            dbContext.MapAuditEvents.Add(new MapAuditEvent
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                CommandId = request.CommandId,
                ElementId = element.Id,
                Action = "delete",
                UserId = userContext.UserId,
                UserDisplayName = userContext.DisplayName,
                OccurredAtUtc = now,
                BeforeStateJson = JsonSerializer.Serialize(before, JsonOptionsFactory.Default),
                AfterStateJson = "null",
            });

            var response = new { deleted = true, elementId, incidentRevision = incident.Revision, serverUtc = now };
            dbContext.ProcessedCommands.Add(idempotency.BuildStoredCommand(
                request.CommandId,
                "map.element.delete",
                elementId.ToString(),
                payloadHash,
                ResponseHelpers.BuildEnvelope(StatusCodes.Status200OK, response),
                userContext.UserId));

            outboxService.AddMessage("map", "map.element.changed", incidentId.ToString(), new { incidentId, incidentRevision = incident.Revision, elementId, action = "delete", serverUtc = now });

            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpGet("audit")]
    [Authorize(Policy = PolicyNames.CanViewSituationMap)]
    public async Task<IActionResult> GetAudit(Guid incidentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var events = await dbContext.MapAuditEvents.AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderByDescending(x => x.SequenceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MapAuditEventDto(
                x.Id,
                x.IncidentId,
                x.SequenceNumber,
                x.CommandId,
                x.ElementId,
                x.Action,
                x.UserId,
                x.UserDisplayName,
                x.OccurredAtUtc,
                x.BeforeStateJson,
                x.AfterStateJson))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [HttpGet("audit/export.csv")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> ExportAuditCsv(Guid incidentId, CancellationToken cancellationToken)
    {
        var events = await dbContext.MapAuditEvents.AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);

        var bytes = CsvExportHelpers.BuildMapAuditCsv(events);
        return File(bytes, "text/csv; charset=utf-8", $"lagekartenlog-{incidentId:N}.csv");
    }

    [HttpGet("export.pdf")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> ExportMapPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var elements = await dbContext.MapElements.AsNoTracking().Where(x => x.IncidentId == incidentId).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var lines = new List<string>
        {
            $"Einsatz: {incident.Name}",
            $"Status: {incident.Status}",
            $"Stand UTC: {DateTime.UtcNow:O}",
            "Kartenquelle: OpenStreetMap contributors (https://www.openstreetmap.org/copyright)",
            ""
        };

        if (elements.Count == 0)
        {
            lines.Add("Keine Lagekartenelemente vorhanden.");
        }
        else
        {
            lines.AddRange(elements.Select(x => $"{x.ElementType}: {x.Label ?? x.SymbolId ?? x.Id.ToString()} | Version {x.Version} | {x.GeometryJson}"));
        }

        var pdfBytes = MinimalPdfBuilder.BuildSimplePdf("Lagekarte", lines);
        return File(pdfBytes, "application/pdf", $"lagekarte-{incidentId:N}.pdf");
    }

    private static bool TryValidateGeometry(string geometryJson, MapElementType type, out string? error)
    {
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(geometryJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var geometryType) || !root.TryGetProperty("coordinates", out var coordinates))
            {
                error = "GeoJSON muss 'type' und 'coordinates' enthalten.";
                return false;
            }

            var typeValue = geometryType.GetString();
            if (type == MapElementType.Symbol && typeValue != "Point")
            {
                error = "Symbol muss eine Point-Geometrie verwenden.";
                return false;
            }

            if (type == MapElementType.Polygon && typeValue != "Polygon")
            {
                error = "Fläche muss eine Polygon-Geometrie verwenden.";
                return false;
            }

            if (typeValue == "Point")
            {
                var lon = coordinates[0].GetDouble();
                var lat = coordinates[1].GetDouble();
                if (lon is < -180 or > 180 || lat is < -90 or > 90)
                {
                    error = "Point-Koordinaten sind außerhalb von WGS84.";
                    return false;
                }
            }
            else if (typeValue == "Polygon")
            {
                foreach (var ring in coordinates.EnumerateArray())
                {
                    foreach (var coordinate in ring.EnumerateArray())
                    {
                        if (coordinate.GetArrayLength() < 2)
                        {
                            error = "Polygon enthält ungültige Koordinaten.";
                            return false;
                        }

                        var lon = coordinate[0].GetDouble();
                        var lat = coordinate[1].GetDouble();
                        if (lon is < -180 or > 180 || lat is < -90 or > 90)
                        {
                            error = "Polygon-Koordinaten sind außerhalb von WGS84.";
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch (Exception)
        {
            error = "Ungültige GeoJSON-Geometrie.";
            return false;
        }
    }

    private static MapElement Clone(MapElement source)
    {
        return new MapElement
        {
            Id = source.Id,
            IncidentId = source.IncidentId,
            ElementType = source.ElementType,
            SymbolId = source.SymbolId,
            Category = source.Category,
            Label = source.Label,
            RadioCallName = source.RadioCallName,
            Strength = source.Strength,
            Note = source.Note,
            ColorHex = source.ColorHex,
            GeometryJson = source.GeometryJson,
            Version = source.Version,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedByUserId = source.UpdatedByUserId,
            UpdatedByDisplayName = source.UpdatedByDisplayName,
        };
    }

    private static bool SetValue(string? current, string? next)
    {
        if (string.Equals(current, next, StringComparison.Ordinal))
        {
            return false;
        }

        current = next;
        return true;
    }

    private static EditorLeaseDto ToLeaseDto(EditorLease lease, DateTime now) =>
        new(lease.IncidentId, lease.UserId, lease.UserDisplayName, lease.SessionId, lease.LeaseToken, lease.ExpiresAtUtc, lease.LastHeartbeatAtUtc, true, lease.ExpiresAtUtc <= now);

    private static MapElementDto ToMapElementDto(MapElement x)
        => new(x.Id, x.IncidentId, x.ElementType, x.SymbolId, x.Category, x.Label, x.RadioCallName, x.Strength, x.Note, x.ColorHex, x.GeometryJson, x.Version, x.UpdatedAtUtc, x.UpdatedByDisplayName);

    private static readonly System.Linq.Expressions.Expression<Func<MapElement, MapElementDto>> ToMapElementDtoExpression =
        x => new MapElementDto(x.Id, x.IncidentId, x.ElementType, x.SymbolId, x.Category, x.Label, x.RadioCallName, x.Strength, x.Note, x.ColorHex, x.GeometryJson, x.Version, x.UpdatedAtUtc, x.UpdatedByDisplayName);
}
