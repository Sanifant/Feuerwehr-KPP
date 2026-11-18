namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Owns all value validation; the type is input metadata only.</summary>
    public interface ISettingValidator
    {
        SettingType Type { get; }

        string? Validate(string? value);
    }
}
