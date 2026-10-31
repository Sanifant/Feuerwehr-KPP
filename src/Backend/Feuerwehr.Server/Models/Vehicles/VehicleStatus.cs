using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleStatus
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public bool AllowsBooking { get; set; }
        public bool IsActive { get; set; } = true;
        public int Version { get; set; } = 1;
    }
}
