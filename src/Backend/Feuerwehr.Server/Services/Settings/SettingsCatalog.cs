namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>Discovers module declarations and rejects invalid or ambiguous definitions at startup.</summary>
    public sealed class SettingsCatalog
    {
        public SettingsCatalog(IEnumerable<Type> types)
        {
            List<SettingDefinition> parameters = [];
            List<SettingsModuleDefinition> modules = [];
            foreach (Type type in types)
            {
                SettingsModuleAttribute? module = type.GetCustomAttribute<SettingsModuleAttribute>();
                List<SettingDefinitionAttribute> declarations = type.GetCustomAttributes<SettingDefinitionAttribute>().ToList();
                if (module is null)
                {
                    if (declarations.Count != 0)
                    {
                        throw new InvalidOperationException($"Settings on {type.FullName} require a SettingsModule attribute.");
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(module.Id) || string.IsNullOrWhiteSpace(module.Name)
                    || modules.Any(x => x.Id == module.Id))
                {
                    throw new InvalidOperationException("Settings modules must have unique, nonempty identifiers and names.");
                }

                foreach (string path in module.ApiPaths.Concat(module.PagePaths))
                {
                    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path == "/" || path.EndsWith('/')
                        || path.Contains('?') || path.Contains('#'))
                    {
                        throw new InvalidOperationException($"Invalid settings module path for {module.Id}.");
                    }
                }

                modules.Add(new SettingsModuleDefinition(module.Id, module.Name,
                    Array.AsReadOnly(module.ApiPaths.ToArray()), Array.AsReadOnly(module.PagePaths.ToArray())));
                foreach (SettingDefinitionAttribute declaration in declarations)
                {
                    if (string.IsNullOrEmpty(declaration.Key) || declaration.Key.Length > 200 || !Regex.IsMatch(declaration.Key, "\\A[A-Za-z_][A-Za-z0-9_]*\\z")
                        || string.IsNullOrWhiteSpace(declaration.Name)
                        || parameters.Any(x => string.Equals(x.Key, declaration.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Settings must have unique environment variable keys and nonempty names.");
                    }

                    if (!typeof(ISettingValidator).IsAssignableFrom(declaration.ValidatorType)
                        || declaration.ValidatorType.IsAbstract || !declaration.ValidatorType.IsClass
                        || declaration.ValidatorType.ContainsGenericParameters)
                    {
                        throw new InvalidOperationException($"Setting {declaration.Key} requires a concrete ISettingValidator class.");
                    }

                    parameters.Add(new SettingDefinition(declaration.Key, declaration.Name, declaration.ValidatorType, declaration.Mandatory, module.Id));
                }
            }

            this.Definitions = parameters.AsReadOnly();
            this.Modules = modules.AsReadOnly();
        }

        public IReadOnlyList<SettingDefinition> Definitions { get; }

        public IReadOnlyList<SettingsModuleDefinition> Modules { get; }

        public SettingDefinition? Find(string? key)
        {
            return this.Definitions.SingleOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal));
        }
    }
}
