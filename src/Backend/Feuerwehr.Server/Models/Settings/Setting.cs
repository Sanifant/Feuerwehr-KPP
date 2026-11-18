namespace de.openelp.feuerwehr.Server.Models.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>A global configuration parameter with last-modification metadata.</summary>
    public sealed class Setting
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public SettingType Type { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public ApplicationUser? ModifiedByUser { get; set; }
    }
}
