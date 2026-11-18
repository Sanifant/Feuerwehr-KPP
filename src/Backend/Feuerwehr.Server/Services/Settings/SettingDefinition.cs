namespace de.openelp.feuerwehr.Server.Services.Settings
{
    /// <summary>A parameter declared in application code, never a persisted entity.</summary>
    public sealed record SettingDefinition(string Key, string Name, Type ValidatorType, bool Mandatory, string ModuleId);
}
