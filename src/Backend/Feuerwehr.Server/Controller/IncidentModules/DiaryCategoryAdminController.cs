using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    [Route("api/diary-categories")]
    [ApiController]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public class DiaryCategoryAdminController(IDiaryCategoryAdminService service) : ControllerBase
    {
        [HttpGet]
        /// <summary>
        /// Gets the all.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetAll(cancellationToken));
        }

        [HttpPost]
        /// <summary>
        /// Executes upsert.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<IActionResult> Upsert([FromBody] UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            return this.ToActionResult(await service.Upsert(request, cancellationToken));
        }

    }

}
