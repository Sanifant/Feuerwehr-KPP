using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
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
}
