using de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.Hydrant;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Services.HydrantModules;
using Microsoft.EntityFrameworkCore;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Moq;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public class HydrantServiceTests
{
    [Fact]
    public void CreateAndUpdate_MapTransportContractWithoutReplacingEntity()
    {
        using FeuerwehrDbContext db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        HydrantService service = new HydrantService(db, Mock.Of<IUserContextAccessor>(), TimeProvider.System, Mock.Of<IHydrantMailService>());
        HydrantDto request = new HydrantDto
        {
            Address = new AddressDto { Street = "Dorfstraße", HouseNumber = "5", PostalCode = "23617", City = "Stockelsdorf" },
            Latitude = 53.9,
            Longitude = 10.7,
            NominalDiameter = 100,
            Status = HydrantStatus.Operational,
            Type = HydrantType.Underground,
        };

        ServiceResult created = service.Post(request);
        Assert.Equal(201, created.Code);
        HydrantDto response = Assert.IsType<HydrantDto>(created.Value);
        Hydrant entity = Assert.Single(db.Hydrants);
        Assert.Equal(entity.Id, response.Id);
        Assert.NotSame(request.Address, entity.Address);
        Assert.NotSame(response.Address, entity.Address);

        HydrantDto update = new HydrantDto
        {
            Id = response.Id,
            Address = new AddressDto { Street = "Neue Straße", City = "Stockelsdorf" },
            Latitude = 54,
            Longitude = 11,
            NominalDiameter = 150,
            Status = HydrantStatus.Operational,
            Type = HydrantType.Underground,
            Notes = "Geprüft",
        };
        Assert.Equal(204, service.Put(response.Id, update).Code);
        Assert.Same(entity, Assert.Single(db.Hydrants));
        HydrantDto updated = Assert.IsType<HydrantDto>(service.Get(response.Id).Value);
        Assert.Equal("Neue Straße", updated.Address.Street);
        Assert.Equal(150, updated.NominalDiameter);
        Assert.Equal("Geprüft", updated.Notes);
        Assert.Equal(204, service.Delete(response.Id).Code);
        Assert.Equal(404, service.Get(response.Id).Code);
    }
}
