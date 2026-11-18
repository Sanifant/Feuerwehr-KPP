namespace de.openelp.feuerwehr.Server.Controller.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>Provides configuration availability without exposing setting values.</summary>
    [ApiController]
    [Route("api/Settings/modules")]
    [Authorize]
    public sealed class SettingsModulesController(ISettingsRuntime runtime) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
        {
            return this.Ok(await runtime.GetModuleStatusesAsync(cancellationToken));
        }
    }
}
