namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    /// <summary>
    /// Tracks a successful inspection reminder for one recipient, equipment item, and due date.
    /// </summary>
    public sealed class EquipmentReminderDelivery
    {
        /// <summary>
        /// Gets or sets the unique identifier of the equipment reminder delivery.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the equipment item.
        /// </summary>
        public Guid EquipmentId { get; set; }

        /// <summary>
        /// Gets or sets the equipment record associated with this equipment reminder delivery.
        /// </summary>
        public Equipment Equipment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the identifier of the user receiving the reminder.
        /// </summary>
        public string RecipientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inspection due date covered by this reminder.
        /// </summary>
        public DateOnly DueOn { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the notification was sent.
        /// </summary>
        public DateTime SentAtUtc { get; set; }
    }
}
