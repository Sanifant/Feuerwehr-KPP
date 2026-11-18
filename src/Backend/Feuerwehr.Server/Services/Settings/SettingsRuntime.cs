namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Data;
    using Microsoft.EntityFrameworkCore;

    public sealed class SettingsRuntime(FeuerwehrDbContext db, SettingsCatalog catalog, IServiceProvider services) : ISettingsRuntime
    {
        public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
        {
            SettingDefinition definition = catalog.Find(key) ?? throw new ArgumentException("Unknown setting key.", nameof(key));
            string? value = await db.Settings.AsNoTracking().Where(x => x.Name == key).Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            return value is not null && this.Validator(definition).Validate(value) is null ? value : null;
        }

        public async Task<IReadOnlyList<SettingsModuleStatusDto>> GetModuleStatusesAsync(CancellationToken cancellationToken = default)
        {
            if (catalog.Modules.Count == 0)
            {
                return [];
            }

            string[] keys = catalog.Definitions.Where(x => x.Mandatory).Select(x => x.Key).ToArray();
            Dictionary<string, string> values = await db.Settings.AsNoTracking().Where(x => keys.Contains(x.Name))
                .ToDictionaryAsync(x => x.Name, x => x.Value, cancellationToken);
            return catalog.Modules.Select(module =>
            {
                List<string> missing = catalog.Definitions.Where(x => x.ModuleId == module.Id && x.Mandatory
                    && (!values.TryGetValue(x.Key, out string? value) || this.Validator(x).Validate(value) is not null))
                    .Select(x => x.Name).ToList();
                return new SettingsModuleStatusDto(module.Id, module.Name, module.PagePaths, missing.Count == 0, missing);
            }).ToList();
        }

        private ISettingValidator Validator(SettingDefinition definition)
        {
            return (ISettingValidator)services.GetRequiredService(definition.ValidatorType);
        }
    }
}
