namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    [SettingsModule("test", "Testmodul", ApiPaths = new[] { "/api/test-module" }, PagePaths = new[] { "/test-module" })]
    [SettingDefinition("TEST_EMAIL", "Benachrichtigung", typeof(EmailSettingValidator), Mandatory = true)]
    [SettingDefinition("TEST_NUMBER", "Anzahl", typeof(PositiveIntegerSettingValidator))]
    [SettingDefinition("TEST_TEXT", "Text", typeof(TextSettingValidator))]
    internal sealed class TestSettingsModule
    {
    }
}
