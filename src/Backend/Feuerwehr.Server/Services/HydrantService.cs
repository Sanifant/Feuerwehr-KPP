using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using HydrantEntity = de.openelp.feuerwehr.Server.Models.Hydrant;
using AddressEntity = de.openelp.feuerwehr.Server.Models.Address;

namespace de.openelp.feuerwehr.Server.Services;

public interface IHydrantService
{
    ServiceResult Get();
    ServiceResult Get(int id);
    ServiceResult Post(Hydrant value);
    ServiceResult Put(int id, Hydrant value);
    ServiceResult Delete(int id);
}

public sealed class HydrantService(FeuerwehrDbContext context) : IHydrantService
{
    public ServiceResult Get() => ServiceResult.Ok(context.Hydrants.ToList().Select(ToDto).ToList());

    public ServiceResult Get(int id)
    {
        var entity = context.Hydrants.Find(id);
        return entity is null ? ServiceResult.NotFound() : ServiceResult.Ok(ToDto(entity));
    }

    public ServiceResult Post(Hydrant value)
    {
        var entity = new HydrantEntity { Id = value.Id };
        Apply(entity, value);
        context.Hydrants.Add(entity);
        context.SaveChanges();
        var dto = ToDto(entity);
        return ServiceResult.CreatedAtAction("Get", new { id = dto.Id }, dto);
    }

    public ServiceResult Put(int id, Hydrant value)
    {
        var entity = context.Hydrants.Find(id);
        if (entity is null) return ServiceResult.NotFound();
        Apply(entity, value);
        context.SaveChanges();
        return ServiceResult.NoContent();
    }

    public ServiceResult Delete(int id)
    {
        var entity = context.Hydrants.Find(id);
        if (entity is null) return ServiceResult.NotFound();
        context.Hydrants.Remove(entity);
        context.SaveChanges();
        return ServiceResult.NoContent();
    }

    private static void Apply(HydrantEntity entity, Hydrant dto)
    {
        entity.Address = new AddressEntity
        {
            Street = dto.Address.Street,
            HouseNumber = dto.Address.HouseNumber,
            PostalCode = dto.Address.PostalCode,
            City = dto.Address.City,
            AdditionalInfo = dto.Address.AdditionalInfo,
        };
        entity.Latitude = dto.Latitude;
        entity.Longitude = dto.Longitude;
        entity.NominalDiameter = dto.NominalDiameter;
        entity.Type = dto.Type;
        entity.WaterSource = dto.WaterSource;
        entity.Status = dto.Status;
        entity.Notes = dto.Notes;
    }

    private static Hydrant ToDto(HydrantEntity entity) => new()
    {
        Id = entity.Id,
        Address = new Address
        {
            Street = entity.Address.Street,
            HouseNumber = entity.Address.HouseNumber,
            PostalCode = entity.Address.PostalCode,
            City = entity.Address.City,
            AdditionalInfo = entity.Address.AdditionalInfo,
        },
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        NominalDiameter = entity.NominalDiameter,
        Type = entity.Type,
        WaterSource = entity.WaterSource,
        Status = entity.Status,
        Notes = entity.Notes,
    };
}
