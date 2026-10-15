using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents/{incidentId:guid}/diary")]
[ApiController]
[Authorize]
public class IncidentDiaryController(IIncidentDiaryService service) : ControllerBase
{
    [HttpGet("entries")]
    [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
    public async Task<IActionResult> GetEntries(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetEntries(incidentId, cancellationToken));
    }

    [HttpGet("categories")]
    [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetCategories(cancellationToken));
    }

    [HttpPost("entries")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    public async Task<IActionResult> CreateEntry(Guid incidentId, [FromBody] CreateDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CreateEntry(incidentId, request, cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/revisions")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    public async Task<IActionResult> UpdateEntry(Guid incidentId, Guid entryId, [FromBody] UpdateDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.UpdateEntry(incidentId, entryId, request, cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/cancel")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    public async Task<IActionResult> CancelEntry(Guid incidentId, Guid entryId, [FromBody] CancelDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CancelEntry(incidentId, entryId, request, cancellationToken));
    }

    [HttpGet("export.pdf")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    public async Task<IActionResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.ExportDiaryPdf(incidentId, cancellationToken));
    }
}
