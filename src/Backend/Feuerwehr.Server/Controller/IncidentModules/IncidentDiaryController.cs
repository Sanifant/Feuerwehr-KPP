using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for incident diary operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The incident diary service handling the requested operations.</param>
    [Route("api/incidents/{incidentId:guid}/diary")]
    [ApiController]
    [Authorize]
    public class IncidentDiaryController(IIncidentDiaryService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves the incident diary entries and their current revisions.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the diary entries and revisions, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("entries")]
        [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
        public async Task<IActionResult> GetEntries(Guid incidentId, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetEntries(incidentId, cancellationToken));
        }

        /// <summary>
        /// Retrieves incident diary categories visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the incident diary categories, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("categories")]
        [Authorize(Policy = PolicyNames.CanViewIncidentDiary)]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetCategories(cancellationToken));
        }

        /// <summary>
        /// Creates a numbered diary entry and its initial revision, command outcome, and live event.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The create diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("entries")]
        [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
        public async Task<IActionResult> CreateEntry(Guid incidentId, [FromBody] CreateDiaryEntryRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.CreateEntry(incidentId, request, cancellationToken));
        }

        /// <summary>
        /// Appends a diary revision after validating the caller's expected revision.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="entryId">The identifier of the diary entry.</param>
        /// <param name="request">The update diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("entries/{entryId:guid}/revisions")]
        [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
        public async Task<IActionResult> UpdateEntry(Guid incidentId, Guid entryId, [FromBody] UpdateDiaryEntryRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.UpdateEntry(incidentId, entryId, request, cancellationToken));
        }

        /// <summary>
        /// Cancels a diary entry by recording a new revision and retaining its previous content.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="entryId">The identifier of the diary entry.</param>
        /// <param name="request">The cancel diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("entries/{entryId:guid}/cancel")]
        [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
        public async Task<IActionResult> CancelEntry(Guid incidentId, Guid entryId, [FromBody] CancelDiaryEntryRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.CancelEntry(incidentId, entryId, request, cancellationToken));
        }

        /// <summary>
        /// Exports the incident diary as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export.pdf")]
        [Authorize(Policy = PolicyNames.CanEditIncidentDiary)]
        public async Task<IActionResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.ExportDiaryPdf(incidentId, cancellationToken));
        }
    }
}
