using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public class HydrantServiceTests
{
    [Fact]
    /// <summary>
    /// Creates the and Update Map Transport Contract Without Replacing Entity.
    /// </summary>
    public void CreateAndUpdate_MapTransportContractWithoutReplacingEntity()
    {
        using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new HydrantService(db);
        var request = new Hydrant
        {
            Address = new Address { Street = "DorfstraÃŸe", HouseNumber = "5", PostalCode = "23617", City = "Stockelsdorf" },
            Latitude = 53.9, Longitude = 10.7, NominalDiameter = 100,
            Status = HydrantStatus.Operational, Type = HydrantType.Underground,
        };

        var created = service.Post(request);
        Assert.Equal(201, created.Code);
        var response = Assert.IsType<Hydrant>(created.Value);
        var entity = Assert.Single(db.Hydrants);
        Assert.Equal(entity.Id, response.Id);
        Assert.NotSame(request.Address, entity.Address);
        Assert.NotSame(response.Address, entity.Address);

        var update = new Hydrant
        {
            Id = response.Id, Address = new Address { Street = "Neue StraÃŸe", City = "Stockelsdorf" },
            Latitude = 54, Longitude = 11, NominalDiameter = 150,
            Status = HydrantStatus.Operational, Type = HydrantType.Underground, Notes = "Geprüft",
        };
        Assert.Equal(204, service.Put(response.Id, update).Code);
        Assert.Same(entity, Assert.Single(db.Hydrants));
        var updated = Assert.IsType<Hydrant>(service.Get(response.Id).Value);
        Assert.Equal("Neue StraÃŸe", updated.Address.Street);
        Assert.Equal(150, updated.NominalDiameter);
        Assert.Equal("Geprüft", updated.Notes);
        Assert.Equal(204, service.Delete(response.Id).Code);
        Assert.Equal(404, service.Get(response.Id).Code);
    }
}

