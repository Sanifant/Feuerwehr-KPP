namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;

    [SettingsModule("custom", "Custom")]
    [SettingDefinition("TEST_CUSTOM", "Custom", typeof(CustomSettingValidator), Mandatory = true)]
    internal sealed class CustomValidatorSettingsModule
    {
    }
}
