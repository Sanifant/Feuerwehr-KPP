namespace de.openelp.feuerwehr.Server.Models
{
    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Gets the formatted Address.
        /// </summary>
        public string GetFormattedAddress()
        {
            return $"{Street} {HouseNumber}, {PostalCode} {City}".Trim();
        }
    }
}
