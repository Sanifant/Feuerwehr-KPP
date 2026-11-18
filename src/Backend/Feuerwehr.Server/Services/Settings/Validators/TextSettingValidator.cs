namespace de.openelp.feuerwehr.Server.Services.Settings.Validators
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Rejects empty and whitespace-only text.</summary>
    public sealed class TextSettingValidator : ISettingValidator
    {
        public SettingType Type => SettingType.Text;

        public string? Validate(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Bitte einen Wert angeben." : null;
        }
    }
}
