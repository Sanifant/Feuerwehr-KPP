using System.Globalization;
using System.Text;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Common.Models.HydrantInspections;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.Hydrant;
using de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;
using AddressEntity = de.openelp.feuerwehr.Server.Models.Hydrant.Address;
using HydrantEntity = de.openelp.feuerwehr.Server.Models.Hydrant.Hydrant;

namespace de.openelp.feuerwehr.Server.Services.HydrantModules
{
    /// <summary>
    /// Executes hydrant Service.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="user">The authenticated user context.</param>
    /// <param name="timeProvider">The server clock.</param>
    public sealed class HydrantService(FeuerwehrDbContext context, IUserContextAccessor user, TimeProvider timeProvider) : IHydrantService
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
                _ = csv.AppendJoin(';', new[]
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
            if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0]))
                ||
                (value.Length > 0 && "\t\r\n".Contains(value[0])))
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
                ToHydrantEntity(entity, dto);
                return entity;
            }).ToList();
            context.Hydrants.AddRange(entities);
            // One SaveChanges transaction persists the complete file, never individual rows.
            _ = await context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new HydrantImportResponse(entities.Count,[]));
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        public ServiceResult Get()
        {
            return ServiceResult.Ok(context.Hydrants.ToList().Select(ToHydrantDto).ToList());
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public ServiceResult Get(int id)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            return entity is null ? ServiceResult.NotFound() : ServiceResult.Ok(ToHydrantDto(entity));
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
            ToHydrantEntity(entity, value);
            _ = context.Hydrants.Add(entity);
            _ = context.SaveChanges();
            HydrantDto dto = ToHydrantDto(entity);
            return ServiceResult.CreatedAtAction("Get", new { id = dto.Id }, dto);
        }

        public ServiceResult Put(int id, HydrantDto value)
        {
            HydrantEntity? entity = context.Hydrants.Find(id);
            if (entity is null)
            {
                return ServiceResult.NotFound();
            }

            ToHydrantEntity(entity, value);
            _ = context.SaveChanges();
            return ServiceResult.NoContent();
        }

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

        public ServiceResult Inspect(int hydrantId, CreateHydrantInspectionRequest request)
        {
            if (request is null || !Enum.IsDefined(request.NewStatus)
                || request.Comment?.Length > HydrantInspection.MaximumCommentLength)
            {
                return ServiceResult.Problem(400, "Bitte einen gültigen Status und einen Kommentar mit maximal 500 Zeichen angeben.");
            }

            Models.ApplicationUser? inspector = context.Users.Find(user.UserId);
            if (inspector is null)
            {
                return ServiceResult.Unauthorized();
            }

            HydrantEntity? entity = context.Hydrants.Find(hydrantId);
            if (entity is null)
            {
                return ServiceResult.NotFound();
            }

            DateTime inspectedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            HydrantInspection inspection = new HydrantInspection
            {
                Id = Guid.NewGuid(),
                HydrantId = entity.Id,
                InspectedOn = DateOnly.FromDateTime(inspectedAtUtc),
                InspectedAtUtc = inspectedAtUtc,
                InspectorId = inspector.Id,
                InspectorName = inspector.FullName,
                Comment = request.Comment,
                PreviousStatus = entity.Status,
                NewStatus = request.NewStatus,
            };

            entity.Status = request.NewStatus;
            _ = context.HydrantInspections.Add(inspection);
            // A single SaveChanges transaction persists the inspection and hydrant status together.
            _ = context.SaveChanges();

            return ServiceResult.StatusCode(201, ToHydrantInspectionDto(inspection));
        }

        public ServiceResult GetInspections(int hydrantId)
        {
            if (!context.Hydrants.Any(h => h.Id == hydrantId))
            {
                return ServiceResult.NotFound();
            }

            IList<HydrantInspection> inspections = context.HydrantInspections.AsNoTracking()
                .Where(i => i.HydrantId == hydrantId)
                .OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).ToList();
            return ServiceResult.Ok(inspections.Select(ToHydrantInspectionDto).ToList());
        }

        private static HydrantInspectionDto ToHydrantInspectionDto(HydrantInspection inspection)
        {
            HydrantInspectionDto returnValue = new HydrantInspectionDto(
                inspection.Id,
                inspection.HydrantId,
                inspection.InspectedOn,
                inspection.InspectedAtUtc,
                inspection.InspectorId,
                inspection.InspectorName,
                inspection.Comment,
                inspection.PreviousStatus,
                inspection.NewStatus,
                inspection.PhotoStorageKey,
                inspection.PhotoContentType);

            return returnValue;
        }

        private static void ToHydrantEntity(HydrantEntity entity, HydrantDto dto)
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

        private static HydrantDto ToHydrantDto(HydrantEntity entity)
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
