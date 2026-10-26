using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents/archive/{incidentId:guid}")]
[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public sealed class IncidentArchiveController(IIncidentArchiveService service) : ControllerBase
{
    [HttpGet("diary")]
    public async Task<IActionResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
        => this.ToActionResult(await service.GetDiary(incidentId, cancellationToken));

    [HttpGet("export.pdf")]
    public async Task<IActionResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
        => this.ToActionResult(await service.ExportPdf(incidentId, cancellationToken));
}
