namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    [SettingDefinition("TEST_ORPHAN", "Orphan", typeof(EmailSettingValidator))]
    internal sealed class MissingModuleSettingDefinition
    {
    }
}
