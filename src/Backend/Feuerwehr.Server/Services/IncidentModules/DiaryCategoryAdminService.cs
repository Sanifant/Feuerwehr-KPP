using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Implements diary category operations for the API application layer.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    public sealed class DiaryCategoryAdminService(FeuerwehrDbContext dbContext) : IDiaryCategoryAdminService
    {
        /// <summary>
        /// Retrieves diary category records visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the matching diary category records, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetAll(CancellationToken cancellationToken)
        {
            List<DiaryCategoryDto> categories = await dbContext.DiaryCategories
                .OrderBy(x => x.Name)
                .Select(x => new DiaryCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
                .ToListAsync(cancellationToken);

            return ServiceResult.Ok(categories);
        }

        /// <summary>
        /// Creates or updates diary category records after validating input and concurrency state.
        /// </summary>
        /// <param name="request">The upsert diary category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the outcome of the diary category operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
        {

            DateTime now = DateTime.UtcNow;
            DiaryCategory? existing = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == request.Code, cancellationToken);
            if (existing is null)
            {
                existing = new DiaryCategory
                {
                    Id = Guid.NewGuid(),
                    Code = request.Code.Trim(),
                    Name = request.Name.Trim(),
                    IsActive = request.IsActive,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                _ = dbContext.DiaryCategories.Add(existing);
            }
            else
            {
                existing.Name = request.Name.Trim();
                existing.IsActive = request.IsActive;
                existing.UpdatedAtUtc = now;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new DiaryCategoryDto(existing.Id, existing.Code, existing.Name, existing.IsActive));
        }
    }
}
