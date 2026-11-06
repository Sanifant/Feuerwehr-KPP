using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
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
    public async Task<IActionResult> Upsert([FromBody] UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.ToActionResult(await service.Upsert(request, cancellationToken));
    }

}
