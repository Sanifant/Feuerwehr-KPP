using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents")]
[ApiController]
[Authorize]
public class IncidentController(IIncidentService service) : ControllerBase
{
    [HttpGet("active")]
    [Authorize(Policy = PolicyNames.CanAccessIncidentModule)]
    public async Task<ActionResult<IncidentSummaryDto>> GetActiveIncident(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetActiveIncident(cancellationToken));
    }

    [HttpGet("archive")]
    [Authorize(Policy = PolicyNames.CanAccessIncidentModule)]
    public async Task<ActionResult<IReadOnlyCollection<IncidentSummaryDto>>> GetArchive([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return this.ToActionResult(await service.GetArchive(page, pageSize, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CreateIncident(request, cancellationToken));
    }

    [HttpPost("{incidentId:guid}/close")]
    [Authorize(Policy = PolicyNames.CanEditSituationMap)]
    public async Task<IActionResult> CloseIncident(Guid incidentId, [FromBody] CloseIncidentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CloseIncident(incidentId, request, cancellationToken));
    }
}
