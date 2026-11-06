using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleReservation
    {
        public Guid Id { get; set; }
        public Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public Guid CommandId { get; set; }
        public string PayloadHash { get; set; } = "";
        // Snapshot identifiers and names preserve history when an account is removed or renamed.
        public string RequesterId { get; set; } = "";
        public string RequesterName { get; set; } = "";
        public string RequesterEmail { get; set; } = "";
        public string RadioCallSign { get; set; } = "";
        public string FireDepartmentName { get; set; } = "";
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public string? Comment { get; set; }
        public VehicleReservationStatus Status { get; set; }
        public bool IsDirectBooking { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
        public string? DecidedById { get; set; }
        public int Version { get; set; } = 1;
    }
}
