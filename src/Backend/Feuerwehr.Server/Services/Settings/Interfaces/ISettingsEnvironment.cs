namespace de.openelp.feuerwehr.Server.Services.Settings
{
    /// <summary>Reads process environment variables when the application initializes settings.</summary>
    public interface ISettingsEnvironment
    {
        string? GetValue(string key);
    }
}
