using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Net;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.Server.Models
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
        public string GetFormattedAddress() =>
            $"{Street} {HouseNumber}, {PostalCode} {City}".Trim();
    }
}
