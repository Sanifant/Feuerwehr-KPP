namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Blocks all declared API paths of modules with missing mandatory configuration.</summary>
    public sealed class SettingsModuleMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, SettingsCatalog catalog, ISettingsRuntime runtime)
        {
            if (context.User.Identity?.IsAuthenticated == true && !context.Request.Path.StartsWithSegments("/api/Settings"))
            {
                List<string> moduleIds = catalog.Modules.Where(module => module.ApiPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
                    .Select(module => module.Id).ToList();
                if (moduleIds.Count != 0)
                {
                    IReadOnlyList<SettingsModuleStatusDto> statuses = await runtime.GetModuleStatusesAsync(context.RequestAborted);
                    SettingsModuleStatusDto? blocked = statuses.FirstOrDefault(module => moduleIds.Contains(module.Id) && !module.IsAvailable);
                    if (blocked is not null)
                    {
                        await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Modul nicht konfiguriert", detail: $"Für das Modul {blocked.Name} fehlen gültige Pflichtparameter.",
                            extensions: new Dictionary<string, object?> { ["moduleId"] = blocked.Id, ["missingParameters"] = blocked.MissingParameters })
                            .ExecuteAsync(context);
                        return;
                    }
                }
            }

            await next(context);
        }
    }
}
