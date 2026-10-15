using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models;
using Microsoft.EntityFrameworkCore;

using de.openelp.feuerwehr.Server.Services;

namespace de.openelp.feuerwehr.Server.Services;

public sealed class FireDepartmentService : IFireDepartmentService
{
    private readonly ILogger<FireDepartmentService> _logger;
    private readonly FeuerwehrDbContext _db;

    public FireDepartmentService(FeuerwehrDbContext db, ILogger<FireDepartmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult> Get()
    {
        var fireDepartments = await _db.FireDepartments
            .Include(fd => fd.FireDepartmentTrainingCourses)
            .ThenInclude(fdTc => fdTc.TrainingCourse)
            .Select(fd => new FireDepartmentDto
            {
                Id = fd.Id,
                Name = fd.Name,
                ContactPersonName = fd.ContactPersonName,
                ContactPersonEmail = fd.ContactPersonEmail,
                TrainingCourses = fd.FireDepartmentTrainingCourses
                    .Select(trainingRelation => new FireDepartmentTrainingCourseDto
                    {
                        TrainingCourseId = trainingRelation.TrainingCourseId,
                        TrainingCourseTitle = trainingRelation.TrainingCourse.Title,
                        AssignedSeats = trainingRelation.SeatsAssigned
                    })
                    .ToList()
            })
            .ToListAsync();

        return ServiceResult.Ok(fireDepartments);
    }

    public async Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto)
    {
        var entity = new FireDepartment
        {
            Name = fireDepartmentDto.Name,
            ContactPersonName = fireDepartmentDto.ContactPersonName,
            ContactPersonEmail = fireDepartmentDto.ContactPersonEmail
        };

        _db.FireDepartments.Add(entity);
        await _db.SaveChangesAsync();

        var createdDto = new FireDepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ContactPersonName = entity.ContactPersonName,
            ContactPersonEmail = entity.ContactPersonEmail,
            TrainingCourses = new List<FireDepartmentTrainingCourseDto>()
        };

        return ServiceResult.CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);
    }

    public async Task<ServiceResult> Get(int id)
    {
        var fireDepartment = await _db.FireDepartments
            .Include(fd => fd.FireDepartmentTrainingCourses)
            .ThenInclude(fdTc => fdTc.TrainingCourse)
            .FirstOrDefaultAsync(fd => fd.Id == id);

        if (fireDepartment is null)
        {
            return ServiceResult.NotFound();
        }

        var dto = new FireDepartmentDto
        {
            Id = fireDepartment.Id,
            Name = fireDepartment.Name,
            ContactPersonName = fireDepartment.ContactPersonName,
            ContactPersonEmail = fireDepartment.ContactPersonEmail,
            TrainingCourses = fireDepartment.FireDepartmentTrainingCourses
                .Select(trainingRelation => new FireDepartmentTrainingCourseDto
                {
                    TrainingCourseId = trainingRelation.TrainingCourseId,
                    TrainingCourseTitle = trainingRelation.TrainingCourse.Title,
                    AssignedSeats = trainingRelation.SeatsAssigned
                })
                .ToList()
        };

        return ServiceResult.Ok(dto);

    }

}

public interface IFireDepartmentService
{
    Task<ServiceResult> Get();
    Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto);
    Task<ServiceResult> Get(int id);
}
