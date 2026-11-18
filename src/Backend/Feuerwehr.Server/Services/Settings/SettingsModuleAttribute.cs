namespace de.openelp.feuerwehr.Server.Services.Settings
{
    /// <summary>Identifies a settings module and the API and web paths protected by its mandatory parameters.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SettingsModuleAttribute(string id, string name) : Attribute
    {
        public string Id { get; } = id;

        public string Name { get; } = name;

        public string[] ApiPaths { get; set; } = [];

        public string[] PagePaths { get; set; } = [];
    }
}
