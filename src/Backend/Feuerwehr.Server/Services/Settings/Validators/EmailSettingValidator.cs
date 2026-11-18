namespace de.openelp.feuerwehr.Server.Services.Settings.Validators
{
    using System.Net.Mail;
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Accepts exactly one nonempty email address without a display name.</summary>
    public sealed class EmailSettingValidator : ISettingValidator
    {
        public SettingType Type => SettingType.Email;

        public string? Validate(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && !value.Contains('\r') && !value.Contains('\n')
                && MailAddress.TryCreate(value, out MailAddress? address)
                && string.Equals(address.Address, value, StringComparison.Ordinal)
                ? null : "Bitte genau eine gültige E-Mail-Adresse ohne Anzeigenamen angeben.";
        }
    }
}
