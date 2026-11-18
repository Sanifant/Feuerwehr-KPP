namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings;

    internal sealed class CustomSettingValidator : ISettingValidator
    {
        public SettingType Type => SettingType.Email;

        public string? Validate(string? value)
        {
            return value == "approved" ? null : "Custom rule rejected the value.";
        }
    }
}
