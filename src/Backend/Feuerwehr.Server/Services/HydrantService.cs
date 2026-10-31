using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;
using AddressEntity = De.Openelp.Feuerwehr.Server.Models.Address;
using HydrantEntity = De.Openelp.Feuerwehr.Server.Models.Hydrant;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Executes hydrant Service.
    /// </summary>
    /// <param name="context">The context.</param>
    public sealed class HydrantService(FeuerwehrDbContext context) : IHydrantService
    {
        /// <summary>
        /// Executes export.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
        {
            List<HydrantEntity> hydrants = await context.Hydrants.AsNoTracking().OrderBy(h => h.Id).ToListAsync(cancellationToken);
            StringBuilder csv = new StringBuilder("street;houseNumber;postalCode;city;latitude;longitude;nominalDiameter;type;waterSource;status;notes\r\n");
            foreach (HydrantEntity? hydrant in hydrants)
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
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])
                || value.Length > 0 && "\t\r\n".Contains(value[0]))
                value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Executes import.
        /// </summary>
        /// <param name="csv">The csv.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default)
        {
            (List<Hydrant>? hydrants, List<HydrantImportError>? errors) = HydrantCsvImport.Read(csv, cancellationToken);
            if (errors.Count > 0) return ServiceResult.BadRequest(new HydrantImportResponse(0, errors));

            List<HydrantEntity> entities = hydrants.Select(dto =>
            {
                HydrantEntity entity = new HydrantEntity();
                Apply(entity, dto);
                return entity;
            }).ToList();
            context.Hydrants.AddRange(entities);
            // One SaveChanges transaction persists the complete file, never individual rows.
            await context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new HydrantImportResponse(entities.Count, []));
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        public ServiceResult Get() => ServiceResult.Ok(context.Hydrants.ToList().Select(ToDto).ToList());

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public ServiceResult Get(int id)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            return entity is null ? ServiceResult.NotFound() : ServiceResult.Ok(ToDto(entity));
        }

        /// <summary>
        /// Executes post.
        /// </summary>
        /// <param name="value">The value.</param>
        public ServiceResult Post(Hydrant value)
        {
            HydrantEntity entity = new HydrantEntity { Id = value.Id };
            Apply(entity, value);
            context.Hydrants.Add(entity);
            context.SaveChanges();
            Hydrant dto = ToDto(entity);
            return ServiceResult.CreatedAtAction("Get", new { id = dto.Id }, dto);
        }

        /// <summary>
        /// Executes put.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        public ServiceResult Put(int id, Hydrant value)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            if (entity is null) return ServiceResult.NotFound();
            Apply(entity, value);
            context.SaveChanges();
            return ServiceResult.NoContent();
        }

        /// <summary>
        /// Deletes the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public ServiceResult Delete(int id)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
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
}
