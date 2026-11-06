using System.Text;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests hydrant import behavior and its expected outcomes.
    /// </summary>
    public class HydrantImportTests
    {
        private const string Header = "street;houseNumber;postalCode;city;latitude;longitude;nominalDiameter;type;waterSource;status;notes";
        private const string Row = "DorfstraÃŸe;12;02361;Testort;53.929134;10.638436;80;1;1;1;";

        /// <summary>
        /// Creates an isolated database for hydrant import assertions.
        /// </summary>
        /// <returns>The configured PostgreSQL database context.</returns>
        public static FeuerwehrDbContext CreateDatabase() => new (new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        /// <summary>
        /// Validates and imports hydrant import records, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="db">The database context for querying and persisting application records.</param>
        /// <param name="csv">The readable CSV stream to validate and import.</param>
        /// <returns>A task whose result is an application result containing the created and updated record counts and any import errors, or validation, authorization, or conflict details on failure.</returns>
        public static async Task<ServiceResult> Import(FeuerwehrDbContext db, string csv)
        {
            using MemoryStream stream = new (Encoding.UTF8.GetBytes(csv));
            return await new HydrantService(db).ImportAsync(stream);
        }

        /// <summary>
        /// Verifies that published example imports every row and allows empty notes.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task PublishedExample_ImportsEveryRowAndAllowsEmptyNotes()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            string csv = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "hydranten-import.csv"));
            ServiceResult result = await Import(db, csv);

            Assert.Equal(200, result.Code);
            HydrantImportResponse response = Assert.IsType<HydrantImportResponse>(result.Value);
            Assert.Equal(2, response.ImportedCount);
            Assert.Empty(response.Errors);
            List<Server.Models.Hydrant> entities = db.Hydrants.OrderBy(h => h.Id).ToList();
            Assert.Equal(2, entities.Count);
            Assert.Equal("Dorfstraße", entities[0].Address.Street);
            Assert.Equal("Beim Gerätehaus", entities[0].Notes);
            Assert.Null(entities[1].Notes);
        }

        /// <summary>
        /// Verifies that omitted notes column and out of range coordinates are accepted.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task OmittedNotesColumnAndOutOfRangeCoordinates_AreAccepted()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            ServiceResult result = await Import(
                db,
                Header.Replace(";notes", string.Empty) + "\n" + "DorfstraÃŸe;12;02361;Testort;123;456;80;1;1;1");

            Assert.Equal(200, result.Code);
            Server.Models.Hydrant entity = Assert.Single(db.Hydrants);
            Assert.Equal(123, entity.Latitude);
            Assert.Equal(456, entity.Longitude);
            Assert.Equal("02361", entity.Address.PostalCode);
            Assert.Null(entity.Notes);
        }

        /// <summary>
        /// Verifies that missing required value rejects whole file including earlier valid rows.
        /// </summary>
        /// <param name="column">The CSV column whose validation is exercised.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("street")]
        [InlineData("houseNumber")]
        [InlineData("postalCode")]
        [InlineData("city")]
        [InlineData("latitude")]
        [InlineData("longitude")]
        [InlineData("nominalDiameter")]
        [InlineData("type")]
        [InlineData("waterSource")]
        [InlineData("status")]
        public async Task MissingRequiredValue_RejectsWholeFileIncludingEarlierValidRows(string column)
        {
            using FeuerwehrDbContext db = CreateDatabase();
            string[] fields = Row.Split(';');
            fields[Array.IndexOf(Header.Split(';'), column)] = " ";
            ServiceResult result = await Import(db, Header + "\n" + Row + "\n" + string.Join(';', fields));

            Assert.Equal(400, result.Code);
            HydrantImportResponse response = Assert.IsType<HydrantImportResponse>(result.Value);
            Assert.Equal(0, response.ImportedCount);
            Assert.Contains(response.Errors, error => error.LineNumber == 3 && error.Description.Contains(column));
            Assert.Empty(db.Hydrants);
            Assert.Empty(db.ChangeTracker.Entries());
        }

        /// <summary>
        /// Verifies that missing rows or required headers are rejected.
        /// </summary>
        /// <param name="csv">The readable CSV stream to validate and import.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("")]
        [InlineData("street;houseNumber")]
        [InlineData("street;street")]
        [InlineData(Header)]
        public async Task MissingRowsOrRequiredHeaders_AreRejected(string csv)
        {
            using FeuerwehrDbContext db = CreateDatabase();
            ServiceResult response = await Import(db, csv);
            Assert.Equal(400, response.Code);
            Assert.NotEmpty(Assert.IsType<HydrantImportResponse>(response.Value).Errors);
            Assert.Empty(db.Hydrants);
        }

        /// <summary>
        /// Verifies that quoted semicolons quotes and multiline notes are preserved.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task QuotedSemicolonsQuotesAndMultilineNotes_ArePreserved()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            string csv = Header + "\r\n" + Row + "\"Beim Tor; Hinweis: \"\"frei\"\"\nZweite Zeile\"";
            Assert.Equal(200, (await Import(db, csv)).Code);
            Assert.Equal("Beim Tor; Hinweis: \"frei\"\nZweite Zeile", Assert.Single(db.Hydrants).Notes);
        }

        /// <summary>
        /// Verifies that invalid CSV or storage types are rejected.
        /// </summary>
        /// <param name="row">The CSV row supplied to the import validator.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("DorfstraÃŸe;12;02361;Testort;abc;10;80;1;1;1;")]
        [InlineData("DorfstraÃŸe;12;02361;Testort;53;10;80;0;1;1;")]
        [InlineData("DorfstraÃŸe;12;02361;Testort;53;10;80;1;1;1;\"nicht geschlossen")]
        [InlineData("DorfstraÃŸe;12;02361;Testort;53;10;80;1;1;1;Notiz;zusätzlich")]
        public async Task InvalidCsvOrStorageTypes_AreRejected(string row)
        {
            using FeuerwehrDbContext db = CreateDatabase();
            Assert.Equal(400, (await Import(db, Header + "\n" + row)).Code);
            Assert.Empty(db.Hydrants);
        }

        /// <summary>
        /// Verifies that bom zero coordinates and omitted trailing notes are accepted.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task BomZeroCoordinatesAndOmittedTrailingNotes_AreAccepted()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            string csv = "\uFEFF" + Header + "\r\n" + "DorfstraÃŸe;12;02361;Testort;0;0;80;1;1;1";
            Assert.Equal(200, (await Import(db, csv)).Code);
            Assert.Equal(0, Assert.Single(db.Hydrants).Latitude);
        }

        /// <summary>
        /// Verifies that cancellation does not persist anything.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Cancellation_DoesNotPersistAnything()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            using MemoryStream stream = new (Encoding.UTF8.GetBytes(Header + "\n" + Row));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new HydrantService(db).ImportAsync(stream, new CancellationToken(true)));
            Assert.Empty(db.Hydrants);
        }

        /// <summary>
        /// Verifies that invalid utf8 is reported as an import error.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task InvalidUtf8_IsReportedAsAnImportError()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            using MemoryStream stream = new ([0xff, 0xff, 0xff]);
            ServiceResult result = await new HydrantService(db).ImportAsync(stream);
            Assert.Equal(400, result.Code);
            Assert.Contains(Assert.IsType<HydrantImportResponse>(result.Value).Errors, error => error.Description.Contains("UTF-8"));
            Assert.Empty(db.Hydrants);
        }

        /// <summary>
        /// Verifies that excessive row count does not import partial data.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ExcessiveRowCount_DoesNotImportPartialData()
        {
            using FeuerwehrDbContext db = CreateDatabase();
            ServiceResult result = await Import(db, Header + "\n" + string.Join('\n', Enumerable.Repeat(Row, 10_001)));
            Assert.Equal(400, result.Code);
            Assert.Contains(Assert.IsType<HydrantImportResponse>(result.Value).Errors, error => error.Description.Contains("10.000"));
            Assert.Empty(db.Hydrants);
        }
    }
}
