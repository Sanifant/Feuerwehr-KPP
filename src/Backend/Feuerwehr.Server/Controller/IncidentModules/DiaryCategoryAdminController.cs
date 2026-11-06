using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for diary category operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The diary category admin service handling the requested operations.</param>
    [Route("api/diary-categories")]
    [ApiController]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public class DiaryCategoryAdminController(IDiaryCategoryAdminService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves diary category records visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the matching diary category records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetAll(cancellationToken));
        }

        /// <summary>
        /// Creates or updates diary category records after validating input and concurrency state.
        /// </summary>
        /// <param name="request">The upsert diary category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the diary category operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return this.ValidationProblem(this.ModelState);
            }

            return this.ToActionResult(await service.Upsert(request, cancellationToken));
        }
    }
}
