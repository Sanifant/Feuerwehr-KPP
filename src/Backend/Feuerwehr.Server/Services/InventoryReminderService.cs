using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models.Inventory;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace de.openelp.feuerwehr.Server.Services;

public interface IInventoryReminderService
{
    Task SendDueReminders(CancellationToken ct);
}

public sealed class InventoryReminderService(FeuerwehrDbContext db, IMailService mail, TimeProvider clock,
    ILogger<InventoryReminderService> logger) : IInventoryReminderService
{
    public async Task SendDueReminders(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var items = await db.Equipment.AsNoTracking().Include(x => x.EquipmentType).ThenInclude(x => x.Category)
            .Where(x => x.IsActive).ToListAsync(ct);
        var due = items.Select(InventoryService.ToDto).Where(x => x.NextInspectionOn <= today.AddDays(14))
            .OrderBy(x => x.NextInspectionOn).ThenBy(x => x.Barcode).ToList();
        if (due.Count == 0) return;

        var recipients = await (from user in db.Users
                                join membership in db.UserRoles on user.Id equals membership.UserId
                                join role in db.Roles on membership.RoleId equals role.Id
                                where user.IsActive && user.Email != null && role.Name == Roles.EquipmentOfficer
                                select new { user.Id, user.Email }).Distinct().ToListAsync(ct);
        foreach (var recipient in recipients)
        {
            ct.ThrowIfCancellationRequested();
            var sent = await db.EquipmentReminderDeliveries.AsNoTracking().Where(x => x.RecipientId == recipient.Id).ToListAsync(ct);
            var pending = due.Where(x => !sent.Any(s => s.EquipmentId == x.Id && s.DueOn == x.NextInspectionOn)).ToList();
            if (pending.Count == 0) continue;
            var body = new StringBuilder("Folgende Geräte sind innerhalb der nächsten zwei Wochen zur Prüfung fällig oder bereits überfällig:\n\n");
            foreach (var item in pending)
            {
                var status = item.Status switch { EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", _ => "In Reparatur" };
                body.AppendLine($"{item.Barcode} | {item.EquipmentTypeName} | {item.Manufacturer} | Seriennummer {item.SerialNumber}");
                body.AppendLine($"Prüftermin: {item.NextInspectionOn.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} | Status: {status}");
                body.AppendLine();
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
                Id = Guid.NewGuid(), EquipmentId = x.Id, RecipientId = recipient.Id, DueOn = x.NextInspectionOn, SentAtUtc = now,
            }));
            // Erst nach erfolgreicher SMTP-Annahme markieren; fehlgeschlagene Empfänger bleiben offen.
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class InventoryReminderWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<InventoryReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
                // Eine dedizierte PostgreSQL-Verbindung hält die Sperre auch über mehrere SaveChanges hinweg.
                // Damit senden mehrere API-Instanzen nicht gleichzeitig dieselbe Zusammenfassung.
                await using var mutex = new NpgsqlConnection(db.Database.GetConnectionString());
                await mutex.OpenAsync(stoppingToken);
                await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(305002014)", mutex);
                if (await command.ExecuteScalarAsync(stoppingToken) is true)
                {
                    try { await scope.ServiceProvider.GetRequiredService<IInventoryReminderService>().SendDueReminders(stoppingToken); }
                    finally
                    {
                        // Nicht an den bereits abgebrochenen Worker-Token binden. Auch Dispose schließt die Verbindung.
                        await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(305002014)", mutex) { CommandTimeout = 5 };
                        await release.ExecuteNonQueryAsync(CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Fehler beim Versand der Inventarerinnerungen."); }

            try { await Task.Delay(TimeSpan.FromHours(6), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
