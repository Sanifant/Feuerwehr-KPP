using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Models.Inventory
{
    public sealed class EquipmentReminderDelivery
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public Equipment Equipment { get; set; } = null!;
        public string RecipientId { get; set; } = "";
        public DateOnly DueOn { get; set; }
        public DateTime SentAtUtc { get; set; }
    }
}
