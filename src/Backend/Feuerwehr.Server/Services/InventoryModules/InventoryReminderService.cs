namespace de.openelp.feuerwehr.Server.Services.InventoryModules
{
    using System.Globalization;
    using System.Text;
    using de.openelp.feuerwehr.Common.Models.Inventory;
    using de.openelp.feuerwehr.Server.Authorization;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models.Inventory;
    using de.openelp.feuerwehr.Server.Models;
    using Microsoft.EntityFrameworkCore;

    public sealed class InventoryReminderService(FeuerwehrDbContext db, IMailQueue queue, TimeProvider clock) : IInventoryReminderService
    {
        public async Task SendDueReminders(CancellationToken ct)
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            DateOnly today = DateOnly.FromDateTime(now);
            List<Equipment> items = await db.Equipment.AsNoTracking().Include(x => x.EquipmentType).ThenInclude(x => x.Category)
                .Where(x => x.IsActive).ToListAsync(ct);
            List<EquipmentDto> due = items.Select(InventoryService.ToDto).Where(x => x.NextInspectionOn <= today.AddDays(14))
                .OrderBy(x => x.NextInspectionOn).ThenBy(x => x.Barcode).ToList();
            if (due.Count == 0)
            {
                return;
            }

            var recipients = await (from user in db.Users
                                    join membership in db.UserRoles on user.Id equals membership.UserId
                                    join role in db.Roles on membership.RoleId equals role.Id
                                    where user.IsActive && user.Email != null && role.Name == Roles.EquipmentOfficer
                                    select new { user.Id, user.Email }).Distinct().ToListAsync(ct);
            foreach (var recipient in recipients)
            {
                ct.ThrowIfCancellationRequested();
                List<EquipmentReminderDelivery> queued = await db.EquipmentReminderDeliveries.AsNoTracking().Where(x => x.RecipientId == recipient.Id).ToListAsync(ct);
                List<EquipmentDto> pending = due.Where(x => !queued.Any(s => s.EquipmentId == x.Id && s.DueOn == x.NextInspectionOn)).ToList();
                if (pending.Count == 0)
                {
                    continue;
                }

                StringBuilder body = new StringBuilder("Folgende Geräte sind innerhalb der nächsten zwei Wochen zur Prüfung fällig oder bereits überfällig:\n\n");
                foreach (EquipmentDto item in pending)
                {
                    string status = item.Status switch { EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", _ => "In Reparatur" };
                    body.AppendLine($"{item.Barcode} | {item.EquipmentTypeName} | {item.Manufacturer} | Seriennummer {item.SerialNumber}");
                    body.AppendLine($"Prüftermin: {item.NextInspectionOn.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} | Status: {status}");
                    body.AppendLine();
                }

                queue.Enqueue(new MailMessage
                {
                    Source = "inventory",
                    Recipient = recipient.Email!,
                    Subject = $"Geräteprüfung: {pending.Count} Gerät(e) fällig",
                    TextBody = body.ToString(),
                }, null);

                db.EquipmentReminderDeliveries.AddRange(pending.Select(x => new EquipmentReminderDelivery
                {
                    Id = Guid.NewGuid(),
                    EquipmentId = x.Id,
                    RecipientId = recipient.Id,
                    DueOn = x.NextInspectionOn,
                    QueuedAtUtc = now,
                }));
                // Save the queue entry and reminder receipts together; the central worker handles retries.
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
