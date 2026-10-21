using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents/{incidentId:guid}/map")]
[ApiController]
[Authorize]
public class SituationMapController(ISituationMapService service) : ControllerBase
{
    [HttpGet("state")]
    [Authorize(Policy = PolicyNames.CanViewSituationMap)]
    public async Task<IActionResult> GetState(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetState(incidentId, cancellationToken));
    }

    [HttpPost("lease/acquire")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> AcquireLease(Guid incidentId, [FromBody] AcquireLeaseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.AcquireLease(incidentId, request, cancellationToken));
    }

    [HttpPost("lease/heartbeat")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> Heartbeat(Guid incidentId, [FromBody] LeaseHeartbeatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.Heartbeat(incidentId, request, cancellationToken));
    }

    [HttpPost("lease/release")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> Release(Guid incidentId, [FromBody] ReleaseLeaseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.Release(incidentId, request, cancellationToken));
    }

    [HttpPost("elements")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> UpsertElement(Guid incidentId, [FromBody] UpsertMapElementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.UpsertElement(incidentId, request, cancellationToken));
    }

    [HttpDelete("elements/{elementId:guid}")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> DeleteElement(Guid incidentId, Guid elementId, [FromBody] DeleteMapElementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.DeleteElement(incidentId, elementId, request, cancellationToken));
    }

    [HttpGet("audit")]
    [Authorize(Policy = PolicyNames.CanViewSituationMap)]
    public async Task<IActionResult> GetAudit(Guid incidentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        return this.ToActionResult(await service.GetAudit(incidentId, page, pageSize, cancellationToken));
    }

    [HttpGet("audit/export.csv")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> ExportAuditCsv(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.ExportAuditCsv(incidentId, cancellationToken));
    }

    [HttpGet("export.pdf")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> ExportMapPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.ExportMapPdf(incidentId, cancellationToken));
    }
}
