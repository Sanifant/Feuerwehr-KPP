using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles;

public sealed class Vehicle
{
    public Guid Id { get; set; }
    public int FireDepartmentId { get; set; }
    public FireDepartment FireDepartment { get; set; } = null!;
    public string RadioCallSign { get; set; } = "";
    public string? Name { get; set; }
    public string? LicensePlate { get; set; }
    public Guid? VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public Guid VehicleStatusId { get; set; } = VehicleStatusDefaults.ReadyId;
    public VehicleStatus VehicleStatus { get; set; } = null!;
    public DateTime? StatusChangedAtUtc { get; set; }
    public string EquipmentOfficerId { get; set; } = "";
    public ApplicationUser EquipmentOfficer { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

public sealed class VehicleType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

public static class VehicleStatusDefaults
{
    public static readonly Guid ReadyId = Guid.Parse("5032f99d-4352-4000-8000-000000000001");
    public static readonly Guid MaintenanceId = Guid.Parse("5032f99d-4352-4000-8000-000000000002");
    public static readonly Guid DeregisteredId = Guid.Parse("5032f99d-4352-4000-8000-000000000003");
}

public sealed class VehicleStatus
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public bool AllowsBooking { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

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

public sealed class VehicleBookingDay
{
    public Guid VehicleId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ReservationId { get; set; }
    public VehicleReservation Reservation { get; set; } = null!;
}

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

public sealed class VehicleMailMessage
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string Kind { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string TextBody { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

