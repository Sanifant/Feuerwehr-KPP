using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles;

public sealed class VehicleBookingDay
{
    public Guid VehicleId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ReservationId { get; set; }
    public VehicleReservation Reservation { get; set; } = null!;
}
