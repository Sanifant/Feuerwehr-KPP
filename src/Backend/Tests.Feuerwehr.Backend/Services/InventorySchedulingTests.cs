using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public sealed class InventorySchedulingTests
{
    [Theory]
    [InlineData("2026-01-31", null, 1, "2026-02-28")]
    [InlineData("2024-01-31", null, 1, "2024-02-29")]
    [InlineData("2024-01-31", "2026-02-28", 1, "2026-03-28")]
    /// <summary>
    /// Executes due Date Uses Calendar Months And Last Inspection When Present.
    /// </summary>
    /// <param name="created">The created.</param>
    /// <param name="inspected">The inspected.</param>
    /// <param name="interval">The interval.</param>
    /// <param name="expected">The expected.</param>
    public async Task DueDateUsesCalendarMonthsAndLastInspectionWhenPresent(string created, string? inspected, int interval, string expected)
    {
        await using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new InventoryCategory { Id = Guid.NewGuid(), Name = "Armaturen", NormalizedName = "ARMATUREN" };
        var type = new EquipmentType { Id = Guid.NewGuid(), Category = category, CategoryId = category.Id, Name = "Strahlrohr", InspectionIntervalMonths = interval, InspectionInstructions = "Sichtprüfung" };
        var item = new Equipment { Id = Guid.NewGuid(), BarcodeNumber = 1, EquipmentType = type, EquipmentTypeId = type.Id, CreatedOn = DateOnly.Parse(created), LastInspectedOn = inspected is null ? null : DateOnly.Parse(inspected) };
        db.Equipment.Add(item); await db.SaveChangesAsync();
        var service = new InventoryService(db, Mock.Of<IUserContextAccessor>(), TimeProvider.System);
        var response = await service.Get(item.Id, CancellationToken.None);
        Assert.Equal(DateOnly.Parse(expected), Assert.IsType<EquipmentDetailDto>(response.Value).Equipment.NextInspectionOn);
    }
}

