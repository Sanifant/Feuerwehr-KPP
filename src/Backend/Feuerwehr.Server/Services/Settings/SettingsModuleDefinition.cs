namespace de.openelp.feuerwehr.Server.Services.Settings
{
    /// <summary>Immutable module metadata discovered from attributes.</summary>
    public sealed record SettingsModuleDefinition(string Id, string Name, IReadOnlyList<string> ApiPaths, IReadOnlyList<string> PagePaths);
}
