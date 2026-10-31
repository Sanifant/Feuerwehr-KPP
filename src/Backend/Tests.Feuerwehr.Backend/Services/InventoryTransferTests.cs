using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
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
        /// <summary>
        /// Executes invalid Csv Is Rejected Before Changing Any Data.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="expected">The expected.</param>
        public async Task InvalidCsvIsRejectedBeforeChangingAnyData(string content, string expected)
        {
            await using FeuerwehrDbContext db = Database();
            InventoryTransferService service = new InventoryTransferService(db, TimeProvider.System);
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            ServiceResult response = await service.Import(InventoryTransferKind.Equipment, stream, CancellationToken.None);
            Assert.Equal(400, response.Code);
            Assert.Contains(Assert.IsType<InventoryImportResponse>(response.Value).Errors, x => x.Description.Contains(expected));
            Assert.Empty(db.Equipment);
        }

        [Fact]
        /// <summary>
        /// Executes category Batch Is Atomic And Invalid Json Entries Have Readable Errors.
        /// </summary>
        public async Task CategoryBatchIsAtomicAndInvalidJsonEntriesHaveReadableErrors()
        {
            await using FeuerwehrDbContext db = Database();
            InventoryTransferService service = new InventoryTransferService(db, TimeProvider.System);
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("[{\"name\":\"Neu\"},{\"name\":\" \"}]"));
            ServiceResult response = await service.Import(InventoryTransferKind.Categories, stream, CancellationToken.None);
            Assert.Equal(400, response.Code);
            InventoryImportError error = Assert.Single(Assert.IsType<InventoryImportResponse>(response.Value).Errors);
            Assert.Equal(2, error.LineNumber); Assert.Contains("Kategoriename", error.Description);
            Assert.Empty(db.InventoryCategories);
        }

        private static FeuerwehrDbContext Database() => new(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

}
