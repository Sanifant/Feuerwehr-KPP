namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleEvent
    {
        public Guid Id { get; set; }

        public Guid VehicleId { get; set; }

        public Guid? ReservationId { get; set; }

        public string ActorId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; }
    }
}
