using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public static class VehicleStatusDefaults
    {
        public static readonly Guid ReadyId = Guid.Parse("5032f99d-4352-4000-8000-000000000001");
        public static readonly Guid MaintenanceId = Guid.Parse("5032f99d-4352-4000-8000-000000000002");
        public static readonly Guid DeregisteredId = Guid.Parse("5032f99d-4352-4000-8000-000000000003");
    }
}
