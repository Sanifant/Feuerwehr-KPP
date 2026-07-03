using Feuerwehr.Common.Models.Dto;
using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class TrainingRecordController : ControllerBase
    {
        private readonly ILogger<TrainingRecordController> _logger;
        private readonly FeuerwehrDbContext _db;

        public TrainingRecordController(FeuerwehrDbContext dbContext, ILogger<TrainingRecordController> logger)
        {
            _db = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingCourseDto>>> Get([FromQuery] string? level, [FromQuery] string? title)
        {
            var trainingsQuery = _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .ThenInclude(fdTc => fdTc.FireDepartment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(level))
            {
                if (!TryParseTrainingLevel(level, out var parsedLevel))
                {
                    return BadRequest("Ungültiger Level-Filter. Erlaubt sind: gemeinde, kreis, land.");
                }

                trainingsQuery = trainingsQuery.Where(tc => tc.Level == parsedLevel);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                var loweredTitle = title.ToLower();
                trainingsQuery = trainingsQuery.Where(tc => tc.Title.ToLower().Contains(loweredTitle));
            }

            var trainings = await trainingsQuery
                .SelectMany(
                    tc => tc.FireDepartmentTrainingCourses.DefaultIfEmpty(),
                    (tc, relation) => new TrainingCourseDto
                    {
                        Id = tc.Id,
                        Title = tc.Title,
                        Description = tc.Description ?? string.Empty,
                        Level = tc.Level,
                        Status = tc.Status,
                        AssignedFireDepartmentId = relation != null ? relation.FireDepartmentId : 0,
                        AssignedFireDepartmentName = relation != null ? relation.FireDepartment.Name : string.Empty,
                        AssignedSeats = relation != null ? relation.SeatsAssigned : 0
                    })
                .ToListAsync();

            return Ok(trainings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TrainingCourseDto>> Get(int id)
        {
            var training = await _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .ThenInclude(fdTc => fdTc.FireDepartment)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (training is null)
            {
                return NotFound();
            }

            var relation = training.FireDepartmentTrainingCourses.FirstOrDefault();

            var dto = new TrainingCourseDto
            {
                Id = training.Id,
                Title = training.Title,
                Description = training.Description ?? string.Empty,
                Level = training.Level,
                Status = training.Status,
                AssignedFireDepartmentId = relation?.FireDepartmentId ?? 0,
                AssignedFireDepartmentName = relation?.FireDepartment.Name ?? string.Empty,
                AssignedSeats = relation?.SeatsAssigned ?? 0
            };

            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult<TrainingCourseDto>> Post([FromBody] TrainingCourseDto value)
        {
            var trimmedTitle = value.Title.Trim();

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                return BadRequest("Ein Titel ist erforderlich.");
            }

            var hasMatchingTitle = await _db.TrainingCourses
                .AnyAsync(tc => tc.Title.ToLower() == trimmedTitle.ToLower());

            if (!hasMatchingTitle && string.IsNullOrWhiteSpace(value.Description))
            {
                return BadRequest("Für neue Titel ist eine Beschreibung erforderlich.");
            }

            var fireDepartment = await _db.FireDepartments
                .FirstOrDefaultAsync(fd => fd.Id == value.AssignedFireDepartmentId);

            if (fireDepartment is null)
            {
                return BadRequest("Die ausgewählte Feuerwehr existiert nicht.");
            }

            var trainingEntity = new TrainingCourse
            {
                Title = trimmedTitle,
                Description = value.Description.Trim(),
                Level = value.Level,
                Status = value.Status,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _db.TrainingCourses.Add(trainingEntity);
            await _db.SaveChangesAsync();

            var relation = new FireDepartmentTrainingCourse
            {
                FireDepartmentId = fireDepartment.Id,
                TrainingCourseId = trainingEntity.Id,
                SeatsAssigned = value.AssignedSeats
            };

            _db.FireDepartmentTrainingCourses.Add(relation);
            await _db.SaveChangesAsync();

            var createdDto = new TrainingCourseDto
            {
                Id = trainingEntity.Id,
                Title = trainingEntity.Title,
                Description = trainingEntity.Description ?? string.Empty,
                Level = trainingEntity.Level,
                Status = trainingEntity.Status,
                AssignedFireDepartmentId = fireDepartment.Id,
                AssignedFireDepartmentName = fireDepartment.Name,
                AssignedSeats = relation.SeatsAssigned
            };

            return CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult> Put(int id, [FromBody] TrainingCourseDto value)
        {
            var trainingEntity = await _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (trainingEntity is null)
            {
                return NotFound();
            }

            var fireDepartment = await _db.FireDepartments
                .FirstOrDefaultAsync(fd => fd.Id == value.AssignedFireDepartmentId);

            if (fireDepartment is null)
            {
                return BadRequest("Die ausgewählte Feuerwehr existiert nicht.");
            }

            trainingEntity.Title = value.Title.Trim();
            trainingEntity.Description = value.Description.Trim();
            trainingEntity.Level = value.Level;
            trainingEntity.Status = value.Status;

            var relation = trainingEntity.FireDepartmentTrainingCourses.FirstOrDefault();
            if (relation is null)
            {
                relation = new FireDepartmentTrainingCourse
                {
                    TrainingCourseId = trainingEntity.Id,
                    FireDepartmentId = fireDepartment.Id,
                    SeatsAssigned = value.AssignedSeats
                };
                _db.FireDepartmentTrainingCourses.Add(relation);
            }
            else
            {
                relation.FireDepartmentId = fireDepartment.Id;
                relation.SeatsAssigned = value.AssignedSeats;
            }

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult> Delete(int id)
        {
            var trainingEntity = await _db.TrainingCourses
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (trainingEntity is null)
            {
                return NotFound();
            }

            _db.TrainingCourses.Remove(trainingEntity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static bool TryParseTrainingLevel(string value, out TrainingLevel level)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "gemeinde":
                case "municipality":
                case "1":
                    level = TrainingLevel.Municipality;
                    return true;
                case "kreis":
                case "district":
                case "2":
                    level = TrainingLevel.District;
                    return true;
                case "land":
                case "state":
                case "3":
                    level = TrainingLevel.State;
                    return true;
                default:
                    level = default;
                    return false;
            }
        }
    }
}
