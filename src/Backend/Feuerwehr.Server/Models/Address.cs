namespace De.Openelp.Feuerwehr.Server.Models
{
    /// <summary>
    /// Describes a street address with optional additional location information.
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Gets or sets the street name.
        /// </summary>
        public string Street { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the building number, including any suffix.
        /// </summary>
        public string HouseNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code of the address.
        /// </summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the municipality of the address.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional instructions for locating the address.
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Formats the street, house number, postal code, city, and optional additional address information.
        /// </summary>
        /// <returns>The formatted human-readable street address.</returns>
        public string GetFormattedAddress() =>
            $"{this.Street} {this.HouseNumber}, {this.PostalCode} {this.City}".Trim();
    }
}
