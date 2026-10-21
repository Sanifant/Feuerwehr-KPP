using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

using de.openelp.feuerwehr.Server.Services;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules;

public sealed class DiaryCategoryAdminService(FeuerwehrDbContext dbContext) : IDiaryCategoryAdminService
{
    public async Task<ServiceResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await dbContext.DiaryCategories
            .OrderBy(x => x.Name)
            .Select(x => new DiaryCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return ServiceResult.Ok(categories);

    }

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

public interface IDiaryCategoryAdminService
{
    Task<ServiceResult> GetAll(CancellationToken cancellationToken);
    Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken);
}
