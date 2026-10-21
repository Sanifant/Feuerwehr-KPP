using System.Text;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public class HydrantImportTests
{
    private const string Header = "street;houseNumber;postalCode;city;latitude;longitude;nominalDiameter;type;waterSource;status;notes";
    private const string Row = "Dorfstraße;12;02361;Testort;53.929134;10.638436;80;1;1;1;";

    private static FeuerwehrDbContext CreateDatabase() => new(new DbContextOptionsBuilder<FeuerwehrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<ServiceResult> Import(FeuerwehrDbContext db, string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return await new HydrantService(db).ImportAsync(stream);
    }

    [Fact]
    public async Task PublishedExample_ImportsEveryRowAndAllowsEmptyNotes()
    {
        using var db = CreateDatabase();
        var csv = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "hydranten-import.csv"));
        var result = await Import(db, csv);

        Assert.Equal(200, result.Code);
        var response = Assert.IsType<HydrantImportResponse>(result.Value);
        Assert.Equal(2, response.ImportedCount);
        Assert.Empty(response.Errors);
        var entities = db.Hydrants.OrderBy(h => h.Id).ToList();
        Assert.Equal(2, entities.Count);
        Assert.Equal("Dorfstraße", entities[0].Address.Street);
        Assert.Equal("Beim Gerätehaus", entities[0].Notes);
        Assert.Null(entities[1].Notes);
    }

    [Fact]
    public async Task OmittedNotesColumnAndOutOfRangeCoordinates_AreAccepted()
    {
        using var db = CreateDatabase();
        var result = await Import(db, Header.Replace(";notes", "") + "\n" +
            "Dorfstraße;12;02361;Testort;123;456;80;1;1;1");

        Assert.Equal(200, result.Code);
        var entity = Assert.Single(db.Hydrants);
        Assert.Equal(123, entity.Latitude);
        Assert.Equal(456, entity.Longitude);
        Assert.Equal("02361", entity.Address.PostalCode);
        Assert.Null(entity.Notes);
    }

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
        using var db = CreateDatabase();
        var fields = Row.Split(';');
        fields[Array.IndexOf(Header.Split(';'), column)] = " ";
        var result = await Import(db, Header + "\n" + Row + "\n" + string.Join(';', fields));

        Assert.Equal(400, result.Code);
        var response = Assert.IsType<HydrantImportResponse>(result.Value);
        Assert.Equal(0, response.ImportedCount);
        Assert.Contains(response.Errors, error => error.LineNumber == 3 && error.Description.Contains(column));
        Assert.Empty(db.Hydrants);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("")]
    [InlineData("street;houseNumber")]
    [InlineData("street;street")]
    [InlineData(Header)]
    public async Task MissingRowsOrRequiredHeaders_AreRejected(string csv)
    {
        using var db = CreateDatabase();
        var response = await Import(db, csv);
        Assert.Equal(400, response.Code);
        Assert.NotEmpty(Assert.IsType<HydrantImportResponse>(response.Value).Errors);
        Assert.Empty(db.Hydrants);
    }

    [Fact]
    public async Task QuotedSemicolonsQuotesAndMultilineNotes_ArePreserved()
    {
        using var db = CreateDatabase();
        var csv = Header + "\r\n" + Row + "\"Beim Tor; Hinweis: \"\"frei\"\"\nZweite Zeile\"";
        Assert.Equal(200, (await Import(db, csv)).Code);
        Assert.Equal("Beim Tor; Hinweis: \"frei\"\nZweite Zeile", Assert.Single(db.Hydrants).Notes);
    }

    [Theory]
    [InlineData("Dorfstraße;12;02361;Testort;abc;10;80;1;1;1;")]
    [InlineData("Dorfstraße;12;02361;Testort;53;10;80;0;1;1;")]
    [InlineData("Dorfstraße;12;02361;Testort;53;10;80;1;1;1;\"nicht geschlossen")]
    [InlineData("Dorfstraße;12;02361;Testort;53;10;80;1;1;1;Notiz;zusätzlich")]
    public async Task InvalidCsvOrStorageTypes_AreRejected(string row)
    {
        using var db = CreateDatabase();
        Assert.Equal(400, (await Import(db, Header + "\n" + row)).Code);
        Assert.Empty(db.Hydrants);
    }

    [Fact]
    public async Task BomZeroCoordinatesAndOmittedTrailingNotes_AreAccepted()
    {
        using var db = CreateDatabase();
        var csv = "\uFEFF" + Header + "\r\n" + "Dorfstraße;12;02361;Testort;0;0;80;1;1;1";
        Assert.Equal(200, (await Import(db, csv)).Code);
        Assert.Equal(0, Assert.Single(db.Hydrants).Latitude);
    }

    [Fact]
    public async Task Cancellation_DoesNotPersistAnything()
    {
        using var db = CreateDatabase();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Header + "\n" + Row));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new HydrantService(db).ImportAsync(stream, new CancellationToken(true)));
        Assert.Empty(db.Hydrants);
    }

    [Fact]
    public async Task InvalidUtf8_IsReportedAsAnImportError()
    {
        using var db = CreateDatabase();
        using var stream = new MemoryStream([0xff, 0xff, 0xff]);
        var result = await new HydrantService(db).ImportAsync(stream);
        Assert.Equal(400, result.Code);
        Assert.Contains(Assert.IsType<HydrantImportResponse>(result.Value).Errors, error => error.Description.Contains("UTF-8"));
        Assert.Empty(db.Hydrants);
    }

    [Fact]
    public async Task ExcessiveRowCount_DoesNotImportPartialData()
    {
        using var db = CreateDatabase();
        var result = await Import(db, Header + "\n" + string.Join('\n', Enumerable.Repeat(Row, 10_001)));
        Assert.Equal(400, result.Code);
        Assert.Contains(Assert.IsType<HydrantImportResponse>(result.Value).Errors, error => error.Description.Contains("10.000"));
        Assert.Empty(db.Hydrants);
    }
}
