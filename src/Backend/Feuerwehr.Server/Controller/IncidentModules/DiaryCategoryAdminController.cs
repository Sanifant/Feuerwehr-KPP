using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Controller.IncidentModules;

[Route("api/diary-categories")]
[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class DiaryCategoryAdminController(FeuerwehrDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await dbContext.DiaryCategories
            .OrderBy(x => x.Name)
            .Select(x => new DiaryCategoryDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertDiaryCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

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
        return Ok(new DiaryCategoryDto(existing.Id, existing.Code, existing.Name, existing.IsActive));
    }
}
