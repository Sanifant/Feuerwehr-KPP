using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;
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
    Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default);
}

public sealed class HydrantService(FeuerwehrDbContext context) : IHydrantService
{
    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        var hydrants = await context.Hydrants.AsNoTracking().OrderBy(h => h.Id).ToListAsync(cancellationToken);
        var csv = new StringBuilder("street;houseNumber;postalCode;city;latitude;longitude;nominalDiameter;type;waterSource;status;notes\r\n");
        foreach (var hydrant in hydrants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            csv.AppendJoin(';', new[]
            {
                CsvText(hydrant.Address.Street), CsvText(hydrant.Address.HouseNumber),
                CsvText(hydrant.Address.PostalCode), CsvText(hydrant.Address.City),
                hydrant.Latitude.ToString("R", CultureInfo.InvariantCulture),
                hydrant.Longitude.ToString("R", CultureInfo.InvariantCulture),
                hydrant.NominalDiameter.ToString(CultureInfo.InvariantCulture),
                ((int)hydrant.Type).ToString(CultureInfo.InvariantCulture),
                ((int)hydrant.WaterSource).ToString(CultureInfo.InvariantCulture),
                ((int)hydrant.Status).ToString(CultureInfo.InvariantCulture), CsvText(hydrant.Notes),
            }).Append("\r\n");
        }

        // A UTF-8 BOM lets spreadsheet applications recognize German characters.
        return Encoding.UTF8.GetBytes("\uFEFF" + csv);
    }

    private static string CsvText(string? value)
    {
        value ??= "";
        // Quoting alone does not prevent spreadsheet applications from evaluating formulas.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])
            || value.Length > 0 && "\t\r\n".Contains(value[0]))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public async Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default)
    {
        var (hydrants, errors) = HydrantCsvImport.Read(csv, cancellationToken);
        if (errors.Count > 0) return ServiceResult.BadRequest(new HydrantImportResponse(0, errors));

        var entities = hydrants.Select(dto =>
        {
            var entity = new HydrantEntity();
            Apply(entity, dto);
            return entity;
        }).ToList();
        context.Hydrants.AddRange(entities);
        // One SaveChanges transaction persists the complete file, never individual rows.
        await context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(new HydrantImportResponse(entities.Count, []));
    }

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
