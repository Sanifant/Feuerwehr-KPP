namespace de.openelp.feuerwehr.Server.Controller.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Authorization;
    using de.openelp.feuerwehr.Server.Services.Settings;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>Admin-only API for global configuration parameters.</summary>
    [ApiController]
    [Route("api/Settings")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public sealed class SettingsController(ISettingsService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.GetAsync(cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateSettingRequest request, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.CreateAsync(request, cancellationToken));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, UpdateSettingRequest request, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.UpdateAsync(id, request, cancellationToken));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            return this.ToActionResult(await service.DeleteAsync(id, cancellationToken));
        }
    }
}
