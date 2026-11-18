namespace de.openelp.feuerwehr.Common.Models.Settings
{
    /// <summary>Module-declared parameter metadata for administration.</summary>
    public sealed record SettingDefinitionDto(string Key, string Name, string ModuleId, string ModuleName, SettingType Type, bool Mandatory);
}
