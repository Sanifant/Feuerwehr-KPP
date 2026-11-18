namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    [SettingsModule("duplicate", "Duplicate")]
    [SettingDefinition("test_email", "Duplicate", typeof(EmailSettingValidator))]
    internal sealed class DuplicateKeySettingsModule
    {
    }
}
