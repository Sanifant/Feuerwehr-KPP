using System;
using System.Collections.Generic;
using System.Text;

namespace Feuerwehr.App.Models
{
    public class HydrantPin
    {
        public string Title { get; set; } = string.Empty;

        public string Diameter { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
