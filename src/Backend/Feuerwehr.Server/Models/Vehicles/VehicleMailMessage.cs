namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleMailMessage
    {
        public Guid Id { get; set; }

        public Guid ReservationId { get; set; }

        public string Kind { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string TextBody { get; set; } = string.Empty;

        public string HtmlBody { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime NextAttemptAtUtc { get; set; }

        public DateTime? LeaseUntilUtc { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public int Attempts { get; set; }

        public string? LastError { get; set; }
    }
}
