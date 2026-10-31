using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public class HydrantExportTests
{
    private static FeuerwehrDbContext CreateDatabase() => new(new DbContextOptionsBuilder<FeuerwehrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    /// <summary>
    /// Executes export Matches Import Example And Preserves Csv Escaping Regardless Of Culture.
    /// </summary>
    public async Task Export_MatchesImportExampleAndPreservesCsvEscapingRegardlessOfCulture()
    {
        using var db = CreateDatabase();
        var service = new HydrantService(db);
        service.Post(new Hydrant
        {
            Id = 2,
            Address = new Address
            {
                Street = "DorfstraÃŸe; Süd", HouseNumber = "12a", PostalCode = "02361", City = "Lübeck",
                AdditionalInfo = "Tor \"links\"",
            },
            Latitude = 53.929134, Longitude = -10.638436, NominalDiameter = 80,
            Type = HydrantType.Underground, WaterSource = WaterSource.WaterGrid, Status = HydrantStatus.Operational,
            Notes = "Erste Zeile\r\nZweite Zeile; \"frei\"",
        });
        service.Post(new Hydrant { Id = 1, Notes = "=1+1" });

        var previousCulture = CultureInfo.CurrentCulture;
        byte[] bytes;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            bytes = await service.ExportAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        using var stream = new MemoryStream(bytes);
        using var parser = new TextFieldParser(stream, Encoding.UTF8) { HasFieldsEnclosedInQuotes = true };
        parser.SetDelimiters(";");
        var example = await File.ReadAllLinesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "hydranten-import.csv"));
        Assert.Equal(example[0].Split(';'), parser.ReadFields());
        var first = parser.ReadFields()!;
        Assert.Equal(11, first.Length);
        Assert.Equal("", first[0]);
        Assert.Equal("'=1+1", first[10]);
        Assert.Equal(new[] { "DorfstraÃŸe; Süd", "12a", "02361", "Lübeck", "53.929134", "-10.638436",
            "80", "1", "1", "1", "Erste Zeile\r\nZweite Zeile; \"frei\"" }, parser.ReadFields());
        Assert.True(parser.EndOfData);
        Assert.Equal(2, await db.Hydrants.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Beim Tor; Hinweis: \"frei\"\r\nZweite Zeile")]
    /// <summary>
    /// Executes export Can Be Imported Without Changing The Csv.
    /// </summary>
    /// <param name="notes">The notes.</param>
    public async Task Export_CanBeImportedWithoutChangingTheCsv(string? notes)
    {
        using var source = CreateDatabase();
        var service = new HydrantService(source);
        var original = new Hydrant
        {
            Id = 42,
            Address = new Address
            {
                Street = "DorfstraÃŸe; Süd", HouseNumber = "12a", PostalCode = "02361", City = "Lübeck",
                AdditionalInfo = "Nicht Bestandteil des Importformats",
            },
            Latitude = 53.929134, Longitude = -10.638436, NominalDiameter = 100,
            Type = HydrantType.AbovegroundWithJack, WaterSource = WaterSource.Cistern, Status = HydrantStatus.UnderMaintenance,
            Notes = notes,
        };
        service.Post(original);
        using var csv = new MemoryStream(await service.ExportAsync());
        using var target = CreateDatabase();

        var result = await new HydrantService(target).ImportAsync(csv);

        Assert.Equal(200, result.Code);
        var response = Assert.IsType<HydrantImportResponse>(result.Value);
        Assert.Equal(1, response.ImportedCount);
        Assert.Empty(response.Errors);
        var imported = Assert.Single(target.Hydrants);
        Assert.Equal(original.Address.Street, imported.Address.Street);
        Assert.Equal(original.Address.HouseNumber, imported.Address.HouseNumber);
        Assert.Equal(original.Address.PostalCode, imported.Address.PostalCode);
        Assert.Equal(original.Address.City, imported.Address.City);
        Assert.Equal(original.Latitude, imported.Latitude);
        Assert.Equal(original.Longitude, imported.Longitude);
        Assert.Equal(original.NominalDiameter, imported.NominalDiameter);
        Assert.Equal(original.Type, imported.Type);
        Assert.Equal(original.WaterSource, imported.WaterSource);
        Assert.Equal(original.Status, imported.Status);
        Assert.Equal(original.Notes, imported.Notes);
        Assert.NotEqual(original.Id, imported.Id);
        Assert.Null(imported.Address.AdditionalInfo);
        Assert.Equal(original.Id, Assert.Single(source.Hydrants).Id);
    }

    [Fact]
    /// <summary>
    /// Executes empty Database Exports Headers And Cancellation Is Respected.
    /// </summary>
    public async Task EmptyDatabase_ExportsHeadersAndCancellationIsRespected()
    {
        using var db = CreateDatabase();
        var service = new HydrantService(db);
        var csv = Encoding.UTF8.GetString(await service.ExportAsync());
        Assert.StartsWith("\uFEFFstreet;houseNumber;", csv);
        Assert.Equal(2, csv.Split("\r\n").Length);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExportAsync(new CancellationToken(true)));
    }
}

