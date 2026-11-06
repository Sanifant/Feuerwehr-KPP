using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Sends equipment officers inspection reminders and tracks delivery per equipment item and due date.
    /// </summary>
    /// <param name="db">The database context for querying and persisting application records.</param>
    /// <param name="mail">The mail service used to send account or operational notifications.</param>
    /// <param name="clock">The time provider used to evaluate dates and expiry consistently.</param>
    /// <param name="logger">The logger used to record operational failures and diagnostics.</param>
    public sealed class InventoryReminderService(
        FeuerwehrDbContext db,
        IMailService mail,
        TimeProvider clock,
        ILogger<InventoryReminderService> logger) : IInventoryReminderService
    {
        /// <summary>
        /// Sends officers a digest of active equipment due within fourteen days, excluding reminders already delivered for the same due date.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
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
                List<EquipmentReminderDelivery> sent = await db.EquipmentReminderDeliveries.AsNoTracking().Where(x => x.RecipientId == recipient.Id).ToListAsync(ct);
                List<EquipmentDto> pending = due.Where(x => !sent.Any(s => s.EquipmentId == x.Id && s.DueOn == x.NextInspectionOn)).ToList();
                if (pending.Count == 0)
                {
                    continue;
                }

                StringBuilder body = new ("Folgende Geräte sind innerhalb der nächsten zwei Wochen zur Prüfung fällig oder bereits überfällig:\n\n");
                foreach (EquipmentDto? item in pending)
                {
                    string status = item.Status switch { EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", _ => "In Reparatur" };
                    _ = body.AppendLine($"{item.Barcode} | {item.EquipmentTypeName} | {item.Manufacturer} | Seriennummer {item.SerialNumber}");
                    _ = body.AppendLine($"Prüftermin: {item.NextInspectionOn.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} | Status: {status}");
                    _ = body.AppendLine();
                }

                try
                {
                    await mail.SendAsync(recipient.Email!, $"Geräteprüfung: {pending.Count} Gerät(e) fällig", body.ToString());
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Inventarerinnerung konnte nicht zugestellt werden. Erneuter Versuch im nächsten Lauf.");
                    continue;
                }

                db.EquipmentReminderDeliveries.AddRange(pending.Select(x => new EquipmentReminderDelivery
                {
                    Id = Guid.NewGuid(),
                    EquipmentId = x.Id,
                    RecipientId = recipient.Id,
                    DueOn = x.NextInspectionOn,
                    SentAtUtc = now,
                }));
                // Erst nach erfolgreicher SMTP-Annahme markieren; fehlgeschlagene Empfänger bleiben offen.
                _ = await db.SaveChangesAsync(ct);
            }
        }
    }
    }
