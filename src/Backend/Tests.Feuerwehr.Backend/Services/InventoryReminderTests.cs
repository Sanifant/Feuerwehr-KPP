using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests inventory reminder behavior and its expected outcomes.
    /// </summary>
    public sealed class InventoryReminderTests
    {
        /// <summary>
        /// Verifies that failed delivery is retried while successful delivery is deduplicated per recipient and due date.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task FailedDeliveryIsRetried_WhileSuccessfulDeliveryIsDeduplicatedPerRecipientAndDueDate()
        {
            await using FeuerwehrDbContext db = new (new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            DateOnly today = new (2026, 10, 29);
            InventoryCategory category = new ()
            { Id = Guid.NewGuid(), Name = "Armaturen", NormalizedName = "ARMATUREN" };
            EquipmentType type = new ()
            { Id = Guid.NewGuid(), Category = category, CategoryId = category.Id, Name = "Strahlrohr", InspectionIntervalMonths = 12, InspectionInstructions = "Sichtprüfung" };
            Equipment item = new ()
            { Id = Guid.NewGuid(), BarcodeNumber = 1, EquipmentType = type, EquipmentTypeId = type.Id, CreatedOn = today.AddDays(15).AddYears(-1), SerialNumber = "123", Manufacturer = "Test" };
            _ = db.Equipment.Add(item);
            IdentityRole role = new (Roles.EquipmentOfficer);
            _ = db.Roles.Add(role);
            foreach ((string? id, bool active) in new[] { ("good", true), ("retry", true), ("inactive", false) })
            {
                _ = db.Users.Add(new ApplicationUser { Id = id, UserName = id, Email = $"{id}@example.test", IsActive = active });
                _ = db.UserRoles.Add(new IdentityUserRole<string> { RoleId = role.Id, UserId = id });
            }

            _ = await db.SaveChangesAsync();
            Mock<IMailService> mail = new ();
            int attempts = 0;
            _ = mail.Setup(x => x.SendAsync("retry@example.test", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => ++attempts == 1 ? Task.FromException(new IOException("SMTP down")) : Task.CompletedTask);
            FixedClock clock = new (today);
            InventoryReminderService service = new (db, mail.Object, clock, NullLogger<InventoryReminderService>.Instance);
            await service.SendDueReminders(CancellationToken.None);
            mail.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            item.CreatedOn = today.AddDays(14).AddYears(-1);
            _ = await db.SaveChangesAsync();
            await service.SendDueReminders(CancellationToken.None);
            _ = Assert.Single(db.EquipmentReminderDeliveries);
            Assert.Equal("good", db.EquipmentReminderDeliveries.Single().RecipientId);
            await service.SendDueReminders(CancellationToken.None);
            Assert.Equal(2, db.EquipmentReminderDeliveries.Count());
            mail.Verify(x => x.SendAsync("good@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            mail.Verify(x => x.SendAsync("retry@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            mail.Verify(x => x.SendAsync("inactive@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            item.LastInspectedOn = today;
            _ = await db.SaveChangesAsync();
            clock.Date = today.AddYears(1).AddDays(-14);
            await service.SendDueReminders(CancellationToken.None);
            Assert.Equal(4, db.EquipmentReminderDeliveries.Count());
        }

        /// <summary>
        /// Supplies a fixed UTC instant for deterministic tests.
        /// </summary>
        /// <param name="date">The calendar date represented by this record.</param>
        private sealed class FixedClock(DateOnly date) : TimeProvider
        {
            /// <summary>
            /// Gets or sets the calendar date represented by this record.
            /// </summary>
            public DateOnly Date { get; set; } = date;

            /// <summary>
            /// Returns the deterministic UTC instant configured for this test clock.
            /// </summary>
            /// <returns>The UTC instant configured by the test clock.</returns>
            public override DateTimeOffset GetUtcNow() => new (this.Date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));
        }
    }
}
