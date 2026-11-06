using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for situation map operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The situation map service handling the requested operations.</param>
    [Route("api/incidents/{incidentId:guid}/map")]
    [ApiController]
    [Authorize]
    public class SituationMapController(ISituationMapService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves the incident's map elements, hydrants, symbols, and current editing lease.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the incident's map state and editing lease, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("state")]
        [Authorize(Policy = PolicyNames.CanViewSituationMap)]
        public async Task<IActionResult> GetState(Guid incidentId, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetState(incidentId, cancellationToken));
        }

        /// <summary>
        /// Attempts to acquire the incident's map editing lease for the requesting user session.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The acquire lease input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the granted or renewed editing lease, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("lease/acquire")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> AcquireLease(Guid incidentId, [FromBody] AcquireLeaseRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.AcquireLease(incidentId, request, cancellationToken));
        }

        /// <summary>
        /// Renews the editing lease after validating its owner, session, and token.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The lease heartbeat input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the granted or renewed editing lease, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("lease/heartbeat")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> Heartbeat(Guid incidentId, [FromBody] LeaseHeartbeatRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.Heartbeat(incidentId, request, cancellationToken));
        }

        /// <summary>
        /// Releases the editing lease owned by the requesting session.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The release lease input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing confirmation of the lease release, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("lease/release")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> Release(Guid incidentId, [FromBody] ReleaseLeaseRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.Release(incidentId, request, cancellationToken));
        }

        /// <summary>
        /// Creates or updates a map element after lease and version checks and persists its audit and live event.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The upsert map element input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the map change outcome and updated incident revision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("elements")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> UpsertElement(Guid incidentId, [FromBody] UpsertMapElementRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.UpsertElement(incidentId, request, cancellationToken));
        }

        /// <summary>
        /// Deletes a map element after lease and version checks while retaining its audit history.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="elementId">The identifier of the map element.</param>
        /// <param name="request">The delete map element input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the map change outcome and updated incident revision, or validation, authorization, or conflict details on failure.</returns>
        [HttpDelete("elements/{elementId:guid}")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> DeleteElement(Guid incidentId, Guid elementId, [FromBody] DeleteMapElementRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.DeleteElement(incidentId, elementId, request, cancellationToken));
        }

        /// <summary>
        /// Retrieves a page of situation-map audit events for the incident.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="page">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in a page.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the requested page of map audit events, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("audit")]
        [Authorize(Policy = PolicyNames.CanViewSituationMap)]
        public async Task<IActionResult> GetAudit(Guid incidentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
        {
            return this.ToActionResult(await service.GetAudit(incidentId, page, pageSize, cancellationToken));
        }

        /// <summary>
        /// Exports the incident's situation-map audit trail as CSV.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("audit/export.csv")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> ExportAuditCsv(Guid incidentId, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.ExportAuditCsv(incidentId, cancellationToken));
        }

        /// <summary>
        /// Exports the incident's situation-map information as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export.pdf")]
        [Authorize(Policy = PolicyNames.CanEditSituationMap)]
        public async Task<IActionResult> ExportMapPdf(Guid incidentId, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.ExportMapPdf(incidentId, cancellationToken));
        }
    }
}
