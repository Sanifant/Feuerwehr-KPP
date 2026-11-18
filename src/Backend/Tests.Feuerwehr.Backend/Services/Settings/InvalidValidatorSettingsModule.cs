namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;

    [SettingsModule("invalid", "Invalid")]
    [SettingDefinition("TEST_INVALID", "Invalid", typeof(string))]
    internal sealed class InvalidValidatorSettingsModule
    {
    }
}
