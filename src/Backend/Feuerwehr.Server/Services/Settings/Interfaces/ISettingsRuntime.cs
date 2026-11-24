namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Reads validated configuration and module availability without administrator impersonation.</summary>
    public interface ISettingsRuntime
    {
        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

        public Task<IReadOnlyList<SettingsModuleStatusDto>> GetModuleStatusesAsync(CancellationToken cancellationToken = default);
    }
}
