using de.openelp.feuerwehr.Common.Models;

namespace de.openelp.feuerwehr.App.Models
{
    public class HydrantPin
    {
        public string Title { get; set; } = string.Empty;

        public string Diameter { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
        public HydrantStatus Status { get; set; }
    }
}
