using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public class HydrantExportTests
{
    private static FeuerwehrDbContext CreateDatabase() => new(new DbContextOptionsBuilder<FeuerwehrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Export_PreservesAllFieldsAndCsvEscapingRegardlessOfCulture()
    {
        using var db = CreateDatabase();
        var service = new HydrantService(db);
        service.Post(new Hydrant
        {
            Id = 2,
            Address = new Address
            {
                Street = "Dorfstraße; Süd", HouseNumber = "12a", PostalCode = "02361", City = "Lübeck",
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
        Assert.Equal(new[] { "id", "street", "houseNumber", "postalCode", "city", "additionalInfo", "latitude", "longitude",
            "nominalDiameter", "type", "waterSource", "status", "notes" }, parser.ReadFields());
        var first = parser.ReadFields()!;
        Assert.Equal("1", first[0]);
        Assert.Equal("", first[5]);
        Assert.Equal("'=1+1", first[12]);
        Assert.Equal(new[] { "2", "Dorfstraße; Süd", "12a", "02361", "Lübeck", "Tor \"links\"", "53.929134", "-10.638436",
            "80", "1", "1", "1", "Erste Zeile\r\nZweite Zeile; \"frei\"" }, parser.ReadFields());
        Assert.True(parser.EndOfData);
        Assert.Equal(2, await db.Hydrants.CountAsync());
    }

    [Fact]
    public async Task EmptyDatabase_ExportsHeadersAndCancellationIsRespected()
    {
        using var db = CreateDatabase();
        var service = new HydrantService(db);
        var csv = Encoding.UTF8.GetString(await service.ExportAsync());
        Assert.StartsWith("\uFEFFid;street;", csv);
        Assert.Equal(2, csv.Split("\r\n").Length);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExportAsync(new CancellationToken(true)));
    }
}
