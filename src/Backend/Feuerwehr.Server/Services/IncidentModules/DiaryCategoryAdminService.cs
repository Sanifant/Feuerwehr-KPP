using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

using De.Openelp.Feuerwehr.Server.Services;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

/// <summary>
/// Executes diary Category Admin Service.
/// </summary>
/// <param name="dbContext">The db Context.</param>
public sealed class DiaryCategoryAdminService(FeuerwehrDbContext dbContext) : IDiaryCategoryAdminService
{
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await dbContext.DiaryCategories
            .OrderBy(x => x.Name)
            .Select(x => new DiaryCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return ServiceResult.Ok(categories);

    }

    /// <summary>
    /// Executes upsert.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
    {

        var now = DateTime.UtcNow;
        var existing = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == request.Code, cancellationToken);
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
            dbContext.DiaryCategories.Add(existing);
        }
        else
        {
            existing.Name = request.Name.Trim();
            existing.IsActive = request.IsActive;
            existing.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(new DiaryCategoryDto(existing.Id, existing.Code, existing.Name, existing.IsActive));

    }

}
