using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests inventory transfer behavior and its expected outcomes.
    /// </summary>
    public sealed class InventoryTransferTests
    {
        /// <summary>
        /// Verifies that invalid CSV is rejected before changing any data.
        /// </summary>
        /// <param name="content">The content to validate or return in the generated response.</param>
        /// <param name="expected">The expected result or value for the scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
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
            await using FeuerwehrDbContext db = Database();
            InventoryTransferService service = new (db, TimeProvider.System);
            using MemoryStream stream = new (Encoding.UTF8.GetBytes(content));
            ServiceResult response = await service.Import(InventoryTransferKind.Equipment, stream, CancellationToken.None);
            Assert.Equal(400, response.Code);
            Assert.Contains(Assert.IsType<InventoryImportResponse>(response.Value).Errors, x => x.Description.Contains(expected));
            Assert.Empty(db.Equipment);
        }

        /// <summary>
        /// Verifies that category batch is atomic and invalid JSON entries have readable errors.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CategoryBatchIsAtomicAndInvalidJsonEntriesHaveReadableErrors()
        {
            await using FeuerwehrDbContext db = Database();
            InventoryTransferService service = new (db, TimeProvider.System);
            using MemoryStream stream = new (Encoding.UTF8.GetBytes("[{\"name\":\"Neu\"},{\"name\":\" \"}]"));
            ServiceResult response = await service.Import(InventoryTransferKind.Categories, stream, CancellationToken.None);
            Assert.Equal(400, response.Code);
            InventoryImportError error = Assert.Single(Assert.IsType<InventoryImportResponse>(response.Value).Errors);
            Assert.Equal(2, error.LineNumber);
            Assert.Contains("Kategoriename", error.Description);
            Assert.Empty(db.InventoryCategories);
        }

        private static FeuerwehrDbContext Database() => new (new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
}
