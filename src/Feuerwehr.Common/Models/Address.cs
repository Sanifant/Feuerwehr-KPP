namespace de.openelp.feuerwehr.Common.Models
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
            return $"{this.Street} {this.HouseNumber}, {this.PostalCode} {this.City}".Trim();
        }
    }
}
