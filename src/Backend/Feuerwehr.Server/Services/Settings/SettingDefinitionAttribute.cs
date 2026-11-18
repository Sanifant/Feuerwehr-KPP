namespace de.openelp.feuerwehr.Server.Services.Settings
{
    /// <summary>Declares a module parameter. Its key is also its environment variable name.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class SettingDefinitionAttribute(string key, string name, Type validatorType) : Attribute
    {
        public string Key { get; } = key;

        public string Name { get; } = name;

        public Type ValidatorType { get; } = validatorType;

        public bool Mandatory { get; set; }
    }
}
