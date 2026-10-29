using de.openelp.feuerwehr.Common.Models.Inventory;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Server.Models.Inventory;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public sealed class InventoryReminderTests
{
    [Fact]
    public async Task FailedDeliveryIsRetried_WhileSuccessfulDeliveryIsDeduplicatedPerRecipientAndDueDate()
    {
        await using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var today = new DateOnly(2026, 10, 29);
        var category = new InventoryCategory { Id = Guid.NewGuid(), Name = "Armaturen", NormalizedName = "ARMATUREN" };
        var type = new EquipmentType { Id = Guid.NewGuid(), Category = category, CategoryId = category.Id, Name = "Strahlrohr", InspectionIntervalMonths = 12, InspectionInstructions = "Sichtprüfung" };
        var item = new Equipment { Id = Guid.NewGuid(), BarcodeNumber = 1, EquipmentType = type, EquipmentTypeId = type.Id, CreatedOn = today.AddDays(15).AddYears(-1), SerialNumber = "123", Manufacturer = "Test" };
        db.Equipment.Add(item);
        var role = new IdentityRole(Roles.EquipmentOfficer); db.Roles.Add(role);
        foreach (var (id, active) in new[] { ("good", true), ("retry", true), ("inactive", false) })
        {
            db.Users.Add(new ApplicationUser { Id = id, UserName = id, Email = $"{id}@example.test", IsActive = active });
            db.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = id });
        }
        await db.SaveChangesAsync();
        var mail = new Mock<IMailService>();
        var attempts = 0;
        mail.Setup(x => x.SendAsync("retry@example.test", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => ++attempts == 1 ? Task.FromException(new IOException("SMTP down")) : Task.CompletedTask);
        var clock = new FixedClock(today);
        var service = new InventoryReminderService(db, mail.Object, clock, NullLogger<InventoryReminderService>.Instance);
        await service.SendDueReminders(CancellationToken.None);
        mail.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        item.CreatedOn = today.AddDays(14).AddYears(-1); await db.SaveChangesAsync();
        await service.SendDueReminders(CancellationToken.None);
        Assert.Single(db.EquipmentReminderDeliveries);
        Assert.Equal("good", db.EquipmentReminderDeliveries.Single().RecipientId);
        await service.SendDueReminders(CancellationToken.None);
        Assert.Equal(2, db.EquipmentReminderDeliveries.Count());
        mail.Verify(x => x.SendAsync("good@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        mail.Verify(x => x.SendAsync("retry@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        mail.Verify(x => x.SendAsync("inactive@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        item.LastInspectedOn = today; await db.SaveChangesAsync();
        clock.Date = today.AddYears(1).AddDays(-14);
        await service.SendDueReminders(CancellationToken.None);
        Assert.Equal(4, db.EquipmentReminderDeliveries.Count());
    }

    private sealed class FixedClock(DateOnly date) : TimeProvider
    {
        public DateOnly Date { get; set; } = date;
        public override DateTimeOffset GetUtcNow() => new(Date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));
    }
}
