namespace De.Openelp.Feuerwehr.Server.Models.Vehicles
{
    /// <summary>
    /// Stores a reservation notification and its delivery attempts for background processing.
    /// </summary>
    public sealed class VehicleMailMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vehicle mail message.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the vehicle reservation.
        /// </summary>
        public Guid ReservationId { get; set; }

        /// <summary>
        /// Gets or sets the reservation notification kind used when dispatching email.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination email address.
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email subject line.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plain-text version of the email body.
        /// </summary>
        public string TextBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML version of the email body.
        /// </summary>
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC time at which the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the earliest UTC time for the next delivery attempt.
        /// </summary>
        public DateTime NextAttemptAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the editing lease expires.
        /// </summary>
        public DateTime? LeaseUntilUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the notification was sent.
        /// </summary>
        public DateTime? SentAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the number of outbox delivery attempts.
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Gets or sets the error recorded by the most recent failed delivery attempt.
        /// </summary>
        public string? LastError { get; set; }
    }
}
