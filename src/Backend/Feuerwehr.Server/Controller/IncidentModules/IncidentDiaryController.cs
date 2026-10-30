using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules;

[Route("api/incidents/{incidentId:guid}/diary")]
[ApiController]
[Authorize]
public class IncidentDiaryController(IIncidentDiaryService service) : ControllerBase
{
    [HttpGet("entries")]
    [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> GetEntries(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetEntries(incidentId, cancellationToken));
    }

    [HttpGet("categories")]
    [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
    /// <summary>
    /// Gets the categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.GetCategories(cancellationToken));
    }

    [HttpPost("entries")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    /// <summary>
    /// Creates the entry.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> CreateEntry(Guid incidentId, [FromBody] CreateDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CreateEntry(incidentId, request, cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/revisions")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    /// <summary>
    /// Updates the entry.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="entryId">The entry Id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> UpdateEntry(Guid incidentId, Guid entryId, [FromBody] UpdateDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.UpdateEntry(incidentId, entryId, request, cancellationToken));
    }

    [HttpPost("entries/{entryId:guid}/cancel")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    /// <summary>
    /// Executes cancel Entry.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="entryId">The entry Id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> CancelEntry(Guid incidentId, Guid entryId, [FromBody] CancelDiaryEntryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.CancelEntry(incidentId, entryId, request, cancellationToken));
    }

    [HttpGet("export.pdf")]
    [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
    /// <summary>
    /// Executes export Diary Pdf.
    /// </summary>
    /// <param name="incidentId">The incident Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await service.ExportDiaryPdf(incidentId, cancellationToken));
    }
}

