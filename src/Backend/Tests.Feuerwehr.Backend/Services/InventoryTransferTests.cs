using System.Text;
using de.openelp.feuerwehr.Common.Models.Inventory;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public sealed class InventoryTransferTests
{
    [Theory]
    [InlineData("barcode;barcode;serialNumber;manufacturer;equipmentType\nKPP-001;KPP-001;A;B;Test", "mehrfach")]
    [InlineData("barcode;serialNumber;manufacturer\nKPP-001;A;B", "equipmentType")]
    [InlineData("barcode;serialNumber;manufacturer;equipmentType\nKPP-000;A;B;Test", "KPP-001")]
    [InlineData("barcode;serialNumber;manufacturer;equipmentType\nKPP-1000;A;B;Test", "KPP-001")]
    [InlineData("barcode;serialNumber;manufacturer;equipmentType;isActive\nKPP-001;A;B;Test;vielleicht", "isActive")]
    [InlineData("barcode;serialNumber;manufacturer;equipmentType;lastInspectedOn\nKPP-001;A;B;Test;29.10.2026", "JJJJ-MM-TT")]
    [InlineData("barcode;serialNumber;manufacturer;equipmentType\nKPP-001;\"Nicht geschlossen;B;Test", "CSV-Zeile")]
    public async Task InvalidCsvIsRejectedBeforeChangingAnyData(string content, string expected)
    {
        await using var db = Database();
        var service = new InventoryTransferService(db, TimeProvider.System);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var response = await service.Import(InventoryTransferKind.Equipment, stream, CancellationToken.None);
        Assert.Equal(400, response.Code);
        Assert.Contains(Assert.IsType<InventoryImportResponse>(response.Value).Errors, x => x.Description.Contains(expected));
        Assert.Empty(db.Equipment);
    }

    [Fact]
    public async Task CategoryBatchIsAtomicAndInvalidJsonEntriesHaveReadableErrors()
    {
        await using var db = Database();
        var service = new InventoryTransferService(db, TimeProvider.System);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[{\"name\":\"Neu\"},{\"name\":\" \"}]"));
        var response = await service.Import(InventoryTransferKind.Categories, stream, CancellationToken.None);
        Assert.Equal(400, response.Code);
        var error = Assert.Single(Assert.IsType<InventoryImportResponse>(response.Value).Errors);
        Assert.Equal(2, error.LineNumber); Assert.Contains("Kategoriename", error.Description);
        Assert.Empty(db.InventoryCategories);
    }

    private static FeuerwehrDbContext Database() => new(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
