using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for incident archive operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The incident archive service handling the requested operations.</param>
    [Route("api/incidents/archive/{incidentId:guid}")]
    [ApiController]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public sealed class IncidentArchiveController(IIncidentArchiveService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves the archived incident diary with its retained revisions.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the diary entries and revisions, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("diary")]
        public async Task<IActionResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
            => this.ToActionResult(await service.GetDiary(incidentId, cancellationToken));

        /// <summary>
        /// Exports the archived incident diary as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export.pdf")]
        public async Task<IActionResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
            => this.ToActionResult(await service.ExportPdf(incidentId, cancellationToken));
    }
}
