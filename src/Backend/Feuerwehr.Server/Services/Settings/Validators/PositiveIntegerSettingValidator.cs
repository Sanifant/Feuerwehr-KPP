namespace de.openelp.feuerwehr.Server.Services.Settings.Validators
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Accepts positive natural numbers without machine-integer size restrictions.</summary>
    public sealed class PositiveIntegerSettingValidator : ISettingValidator
    {
        public SettingType Type => SettingType.Number;

        public string? Validate(string? value)
        {
            return !string.IsNullOrEmpty(value)
                && value.All(character => character >= '0' && character <= '9')
                && value.Any(character => character != '0')
                ? null : "Bitte eine positive natürliche Zahl größer als Null ohne Vorzeichen oder Dezimalstellen angeben.";
        }
    }
}
