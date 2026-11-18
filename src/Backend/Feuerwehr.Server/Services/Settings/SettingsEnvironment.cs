namespace de.openelp.feuerwehr.Server.Services.Settings
{
    public sealed class SettingsEnvironment : ISettingsEnvironment
    {
        public string? GetValue(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }
    }
}
