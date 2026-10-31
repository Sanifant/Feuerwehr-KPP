using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    [Route("api/incidents/archive/{incidentId:guid}")]
    [ApiController]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    /// <summary>
    /// Executes incident Archive Controller.
    /// </summary>
    /// <param name="service">The service.</param>
    public sealed class IncidentArchiveController(IIncidentArchiveService service) : ControllerBase
    {
        [HttpGet("diary")]
        /// <summary>
        /// Gets the diary.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<IActionResult> GetDiary(Guid incidentId, CancellationToken cancellationToken)
            => this.ToActionResult(await service.GetDiary(incidentId, cancellationToken));

        [HttpGet("export.pdf")]
        /// <summary>
        /// Executes export Pdf.
        /// </summary>
        /// <param name="incidentId">The incident Id.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<IActionResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken)
            => this.ToActionResult(await service.ExportPdf(incidentId, cancellationToken));
    }

}
