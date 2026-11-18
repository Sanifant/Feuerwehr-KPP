namespace de.openelp.feuerwehr.Common.Models.Settings
{
    /// <summary>A global setting and its last editor.</summary>
    public sealed record SettingDto(
        Guid Id,
        string Key,
        string Name,
        string Value,
        SettingType Type,
        DateTime ModifiedOn,
        string? ModifiedBy,
        string ModifiedByName);
}
