using Feuerwehr.Common.Models.Dto;
using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class FireDepartmentController : ControllerBase
{
    private readonly ILogger<FireDepartmentController> _logger;
    private readonly FeuerwehrDbContext _db;

    public FireDepartmentController(FeuerwehrDbContext db, ILogger<FireDepartmentController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FireDepartmentDto>>> Get()
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

        return Ok(fireDepartments);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireCommander)]
    public async Task<ActionResult<FireDepartmentDto>> Add([FromBody] FireDepartmentDto fireDepartmentDto)
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

        return CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FireDepartmentDto>> Get(int id)
    {
        var fireDepartment = await _db.FireDepartments
            .Include(fd => fd.FireDepartmentTrainingCourses)
            .ThenInclude(fdTc => fdTc.TrainingCourse)
            .FirstOrDefaultAsync(fd => fd.Id == id);

        if (fireDepartment is null)
        {
            return NotFound();
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

        return Ok(dto);
    }
}
