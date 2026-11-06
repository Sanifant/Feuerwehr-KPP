using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests inventory scheduling behavior and its expected outcomes.
    /// </summary>
    public sealed class InventorySchedulingTests
    {
        /// <summary>
        /// Verifies that due date uses calendar months and last inspection when present.
        /// </summary>
        /// <param name="created">The equipment registration date supplied by the scenario.</param>
        /// <param name="inspected">The most recent inspection date, or null when no inspection exists.</param>
        /// <param name="interval">The inspection interval in calendar months used in this scenario.</param>
        /// <param name="expected">The expected result or value for the scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("2026-01-31", null, 1, "2026-02-28")]
        [InlineData("2024-01-31", null, 1, "2024-02-29")]
        [InlineData("2024-01-31", "2026-02-28", 1, "2026-03-28")]
        public async Task DueDateUsesCalendarMonthsAndLastInspectionWhenPresent(string created, string? inspected, int interval, string expected)
        {
            await using FeuerwehrDbContext db = new (new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            InventoryCategory category = new ()
            { Id = Guid.NewGuid(), Name = "Armaturen", NormalizedName = "ARMATUREN" };
            EquipmentType type = new ()
            { Id = Guid.NewGuid(), Category = category, CategoryId = category.Id, Name = "Strahlrohr", InspectionIntervalMonths = interval, InspectionInstructions = "Sichtprüfung" };
            Equipment item = new ()
            { Id = Guid.NewGuid(), BarcodeNumber = 1, EquipmentType = type, EquipmentTypeId = type.Id, CreatedOn = DateOnly.Parse(created), LastInspectedOn = inspected is null ? null : DateOnly.Parse(inspected) };
            _ = db.Equipment.Add(item);
            _ = await db.SaveChangesAsync();
            InventoryService service = new (db, Mock.Of<IUserContextAccessor>(), TimeProvider.System);
            ServiceResult response = await service.Get(item.Id, CancellationToken.None);
            Assert.Equal(DateOnly.Parse(expected), Assert.IsType<EquipmentDetailDto>(response.Value).Equipment.NextInspectionOn);
        }
    }
}
