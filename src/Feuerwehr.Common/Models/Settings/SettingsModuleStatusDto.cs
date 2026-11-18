namespace de.openelp.feuerwehr.Common.Models.Settings
{
    /// <summary>Module availability without exposing configured values.</summary>
    public sealed record SettingsModuleStatusDto(string Id, string Name, IReadOnlyList<string> PagePaths, bool IsAvailable, IReadOnlyList<string> MissingParameters);
}
