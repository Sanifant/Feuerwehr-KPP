namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings;

    public sealed class SettingsCatalogTests
    {
        [Fact]
        public void DiscoversDefinitionsAndAllModulePaths()
        {
            SettingsCatalog catalog = new SettingsCatalog([typeof(TestSettingsModule)]);
            SettingsModuleDefinition module = Assert.Single(catalog.Modules);
            Assert.Equal("test", module.Id);
            Assert.Equal(["/api/test-module"], module.ApiPaths);
            Assert.Equal(["/test-module"], module.PagePaths);
            Assert.True(catalog.Find("TEST_EMAIL")!.Mandatory);
            Assert.Null(catalog.Find("test_email"));
        }

        [Fact]
        public void RejectsDuplicateKeysDuplicateModuleIdsMissingModulesAndInvalidValidators()
        {
            Assert.Throws<InvalidOperationException>(() => new SettingsCatalog([typeof(TestSettingsModule), typeof(DuplicateKeySettingsModule)]));
            Assert.Throws<InvalidOperationException>(() => new SettingsCatalog([typeof(TestSettingsModule), typeof(TestSettingsModule)]));
            Assert.Throws<InvalidOperationException>(() => new SettingsCatalog([typeof(MissingModuleSettingDefinition)]));
            Assert.Throws<InvalidOperationException>(() => new SettingsCatalog([typeof(InvalidValidatorSettingsModule)]));
        }

        [Fact]
        public void ProductionDoesNotPredefineAnyParameterOrModule()
        {
            SettingsCatalog catalog = new SettingsCatalog([typeof(EmptySettingsTestModule)]);
            Assert.Empty(catalog.Definitions);
            Assert.Empty(catalog.Modules);
        }
    }
}
