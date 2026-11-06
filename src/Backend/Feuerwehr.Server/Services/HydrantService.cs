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
    /// Implements hydrant operations for the API application layer.
    /// </summary>
    /// <param name="context">The database context or model builder used by this operation.</param>
    public sealed class HydrantService(FeuerwehrDbContext context) : IHydrantService
    {
        /// <summary>
        /// Exports hydrant records in the supported download format.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the generated export file bytes.</returns>
        public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
        {
            List<HydrantEntity> hydrants = await context.Hydrants.AsNoTracking().OrderBy(h => h.Id).ToListAsync(cancellationToken);
            StringBuilder csv = new ("street;houseNumber;postalCode;city;latitude;longitude;nominalDiameter;type;waterSource;status;notes\r\n");
            foreach (HydrantEntity? hydrant in hydrants)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] values = new[]
                {
                    CsvText(hydrant.Address.Street), CsvText(hydrant.Address.HouseNumber),
                    CsvText(hydrant.Address.PostalCode), CsvText(hydrant.Address.City),
                    hydrant.Latitude.ToString("R", CultureInfo.InvariantCulture),
                    hydrant.Longitude.ToString("R", CultureInfo.InvariantCulture),
                    hydrant.NominalDiameter.ToString(CultureInfo.InvariantCulture),
                    ((int)hydrant.Type).ToString(CultureInfo.InvariantCulture),
                    ((int)hydrant.WaterSource).ToString(CultureInfo.InvariantCulture),
                    ((int)hydrant.Status).ToString(CultureInfo.InvariantCulture), CsvText(hydrant.Notes),
                };
                _ = csv.AppendJoin(';', values).Append("\r\n");
            }

            // A UTF-8 BOM lets spreadsheet applications recognize German characters.
            return Encoding.UTF8.GetBytes("\uFEFF" + csv);
        }

        /// <summary>
        /// Validates and imports hydrant records, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="csv">The readable CSV stream to validate and import.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the created and updated record counts and any import errors, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default)
        {
            (List<Hydrant>? hydrants, List<HydrantImportError>? errors) = HydrantCsvImport.Read(csv, cancellationToken);
            if (errors.Count > 0)
            {
                return ServiceResult.BadRequest(new HydrantImportResponse(0, errors));
            }

            List<HydrantEntity> entities = hydrants.Select(dto =>
            {
                HydrantEntity entity = new ();
                Apply(entity, dto);
                return entity;
            }).ToList();
            context.Hydrants.AddRange(entities);

            _ = await context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new HydrantImportResponse(entities.Count, []));
        }

        /// <summary>
        /// Retrieves hydrant records visible to the caller.
        /// </summary>
        /// <returns>An application result containing the matching hydrant records, or validation, authorization, or conflict details on failure.</returns>
        public ServiceResult Get() => ServiceResult.Ok(context.Hydrants.ToList().Select(ToDto).ToList());

        /// <summary>
        /// Retrieves hydrant records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to retrieve.</param>
        /// <returns>An application result containing the matching hydrant record, or validation, authorization, or conflict details on failure.</returns>
        public ServiceResult Get(int id)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            return entity is null ? ServiceResult.NotFound() : ServiceResult.Ok(ToDto(entity));
        }

        /// <summary>
        /// Creates hydrant records from the validated input.
        /// </summary>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An application result containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        public ServiceResult Post(Hydrant value)
        {
            HydrantEntity entity = new ()
            { Id = value.Id };
            Apply(entity, value);
            _ = context.Hydrants.Add(entity);
            _ = context.SaveChanges();
            Hydrant dto = ToDto(entity);
            return ServiceResult.CreatedAtAction("Get", new { id = dto.Id }, dto);
        }

        /// <summary>
        /// Updates hydrant records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An application result containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        public ServiceResult Put(int id, Hydrant value)
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

        /// <summary>
        /// Deletes the hydrant record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <returns>An application result containing confirmation that the record was deleted, or validation, authorization, or conflict details on failure.</returns>
        public ServiceResult Delete(int id)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            if (entity is null)
            {
                return ServiceResult.NotFound();
            }

            _ = context.Hydrants.Remove(entity);
            _ = context.SaveChanges();
            return ServiceResult.NoContent();
        }

        private static string CsvText(string? value)
        {
            value ??= string.Empty;
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])
                || value.Length > 0 && "\t\r\n".Contains(value[0]))
            {
                value = "'" + value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
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

        private static Hydrant ToDto(HydrantEntity entity) => new ()
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
