using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for incident operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The incident service handling the requested operations.</param>
    [Route("api/incidents")]
    [ApiController]
    [Authorize]
    public class IncidentController(IIncidentService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves the current active incident, when one exists.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the active incident, when present, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("active")]
        [Authorize(Policy = PolicyNames.CanAccessIncidentModule)]
        public async Task<ActionResult<IncidentSummaryDto>> GetActiveIncident(CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetActiveIncident(cancellationToken));
        }

        /// <summary>
        /// Retrieves a page of closed incidents from the archive.
        /// </summary>
        /// <param name="page">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in a page.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the requested page of closed incidents, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("archive")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        public async Task<ActionResult<IReadOnlyCollection<IncidentSummaryDto>>> GetArchive([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
        {
            return this.ToActionResult(await service.GetArchive(page, pageSize, cancellationToken));
        }

        /// <summary>
        /// Creates an active incident, records the command outcome, and stages its live update event.
        /// </summary>
        /// <param name="request">The create incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the created incident and its initial editing lease, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.CreateIncident(request, cancellationToken));
        }

        /// <summary>
        /// Closes the active incident after checking its revision and records the command and live event consistently.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The close incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the closed incident and updated revision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("{incidentId:guid}/close")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> CloseIncident(Guid incidentId, [FromBody] CloseIncidentRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.CloseIncident(incidentId, request, cancellationToken));
        }
    }
}
