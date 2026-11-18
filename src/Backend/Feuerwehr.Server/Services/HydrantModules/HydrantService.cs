using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.Hydrant;
using Microsoft.EntityFrameworkCore;
using AddressEntity = de.openelp.feuerwehr.Server.Models.Hydrant.Address;
using HydrantEntity = de.openelp.feuerwehr.Server.Models.Hydrant.Hydrant;

namespace de.openelp.feuerwehr.Server.Services.HydrantModules
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
            value ??= string.Empty;
            // Quoting alone does not prevent spreadsheet applications from evaluating formulas.
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])
                || value.Length > 0 && "\t\r\n".Contains(value[0]))
            {
                value = "'" + value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public async Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default)
        {
            var (hydrants, errors) = HydrantCsvImport.Read(csv, cancellationToken);
            if (errors.Count > 0)
            {
                return ServiceResult.BadRequest(new HydrantImportResponse(0, errors));
            }

            List<HydrantEntity> entities = hydrants.Select(dto =>
            {
                HydrantEntity entity = new HydrantEntity();
                Apply(entity, dto);
                return entity;
            }).ToList();
            context.Hydrants.AddRange(entities);
            // One SaveChanges transaction persists the complete file, never individual rows.
            await context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new HydrantImportResponse(entities.Count,[]));
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        public ServiceResult Get()
        {
            return ServiceResult.Ok(context.Hydrants.ToList().Select(ToDto).ToList());
        }

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
        public ServiceResult Post(HydrantDto value)
        {
            if (value == null)
            {
                return ServiceResult.BadRequest(null);
            }

            HydrantEntity entity = new HydrantEntity { Id = value.Id };
            Apply(entity, value);
            _ = context.Hydrants.Add(entity);
            _ = context.SaveChanges();
            HydrantDto dto = ToDto(entity);
            return ServiceResult.CreatedAtAction("Get", new { id = dto.Id }, dto);
        }

        public ServiceResult Put(int id, HydrantDto value)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            if (entity is null)
            {
                return ServiceResult.NotFound();
            }

            Apply(entity, value);
            _ = context.SaveChanges();
            return ServiceResult.NoContent();
        }

        public ServiceResult Delete(int id)
        {
            var entity = context.Hydrants.Find(id);
            if (entity is null)
            {
                return ServiceResult.NotFound();
            }

            context.Hydrants.Remove(entity);
            context.SaveChanges();
            return ServiceResult.NoContent();
        }

        private static void Apply(HydrantEntity entity, HydrantDto dto)
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

        private static HydrantDto ToDto(HydrantEntity entity)
        {
            return new ()
            {
                Id = entity.Id,
                Address = new AddressDto
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
}
