using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleEvent
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public Guid? ReservationId { get; set; }
        public string ActorId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime OccurredAtUtc { get; set; }
    }
}
