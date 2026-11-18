namespace de.openelp.feuerwehr.Common.Models.Settings
{
    /// <summary>Updates only the value of an existing setting.</summary>
    public sealed class UpdateSettingRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}
