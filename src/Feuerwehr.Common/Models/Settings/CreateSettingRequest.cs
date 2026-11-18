namespace de.openelp.feuerwehr.Common.Models.Settings
{
    /// <summary>Creates a named global setting with typed validation.</summary>
    public sealed class CreateSettingRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public SettingType Type { get; set; }
    }
}
