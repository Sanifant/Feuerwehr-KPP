using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public sealed class TrainingRecordService : ITrainingRecordService
    {
        private readonly ILogger<TrainingRecordService> _logger;
        private readonly FeuerwehrDbContext _db;

        public TrainingRecordService(FeuerwehrDbContext dbContext, ILogger<TrainingRecordService> logger)
        {
            _db = dbContext;
            _logger = logger;
        }
        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="title">The title.</param>
        public async Task<ServiceResult> Get(string? level, string? title)
        {
            IQueryable<TrainingCourse> trainingsQuery = _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .ThenInclude(fdTc => fdTc.FireDepartment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(level))
            {
                if (!TryParseTrainingLevel(level, out TrainingLevel parsedLevel))
                {
                    return ServiceResult.BadRequest("Ungültiger Level-Filter. Erlaubt sind: gemeinde, kreis, land.");
                }

                trainingsQuery = trainingsQuery.Where(tc => tc.Level == parsedLevel);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                string loweredTitle = title.ToLower();
                trainingsQuery = trainingsQuery.Where(tc => tc.Title.ToLower().Contains(loweredTitle));
            }

            List<TrainingCourseDto> trainings = await trainingsQuery
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

            return ServiceResult.Ok(trainings);

        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<ServiceResult> Get(int id)
        {
            TrainingCourse? training = await _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .ThenInclude(fdTc => fdTc.FireDepartment)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (training is null)
            {
                return ServiceResult.NotFound();
            }

            FireDepartmentTrainingCourse? relation = training.FireDepartmentTrainingCourses.FirstOrDefault();

            TrainingCourseDto dto = new TrainingCourseDto
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

            return ServiceResult.Ok(dto);

        }

        /// <summary>
        /// Executes post.
        /// </summary>
        /// <param name="value">The value.</param>
        public async Task<ServiceResult> Post(TrainingCourseDto value)
        {
            string trimmedTitle = value.Title.Trim();

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                return ServiceResult.BadRequest("Ein Titel ist erforderlich.");
            }

            bool hasMatchingTitle = await _db.TrainingCourses
                .AnyAsync(tc => tc.Title.ToLower() == trimmedTitle.ToLower());

            if (!hasMatchingTitle && string.IsNullOrWhiteSpace(value.Description))
            {
                return ServiceResult.BadRequest("Für neue Titel ist eine Beschreibung erforderlich.");
            }

            FireDepartment? fireDepartment = await _db.FireDepartments
                .FirstOrDefaultAsync(fd => fd.Id == value.AssignedFireDepartmentId);

            if (fireDepartment is null)
            {
                return ServiceResult.BadRequest("Die ausgewählte Feuerwehr existiert nicht.");
            }

            TrainingCourse? trainingEntity = _db.TrainingCourses.FirstOrDefault(t => t.Title.Equals(trimmedTitle));

            if (trainingEntity == null)
            {
                trainingEntity = new TrainingCourse
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
            }

            FireDepartmentTrainingCourse relation = new FireDepartmentTrainingCourse
            {
                FireDepartmentId = fireDepartment.Id,
                TrainingCourseId = trainingEntity.Id,
                SeatsAssigned = value.AssignedSeats
            };

            _db.FireDepartmentTrainingCourses.Add(relation);
            await _db.SaveChangesAsync();

            TrainingCourseDto createdDto = new TrainingCourseDto
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

            return ServiceResult.CreatedAtAction(nameof(Get), new { id = createdDto.Id }, createdDto);

        }

        /// <summary>
        /// Executes put.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        public async Task<ServiceResult> Put(int id, TrainingCourseDto value)
        {
            TrainingCourse? trainingEntity = await _db.TrainingCourses
                .Include(tc => tc.FireDepartmentTrainingCourses)
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (trainingEntity is null)
            {
                return ServiceResult.NotFound();
            }

            FireDepartment? fireDepartment = await _db.FireDepartments
                .FirstOrDefaultAsync(fd => fd.Id == value.AssignedFireDepartmentId);

            if (fireDepartment is null)
            {
                return ServiceResult.BadRequest("Die ausgewählte Feuerwehr existiert nicht.");
            }

            trainingEntity.Title = value.Title.Trim();
            trainingEntity.Description = value.Description.Trim();
            trainingEntity.Level = value.Level;
            trainingEntity.Status = value.Status;

            FireDepartmentTrainingCourse? relation = trainingEntity.FireDepartmentTrainingCourses.FirstOrDefault();
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

            return ServiceResult.NoContent();

        }

        /// <summary>
        /// Deletes the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<ServiceResult> Delete(int id)
        {
            TrainingCourse? trainingEntity = await _db.TrainingCourses
                .FirstOrDefaultAsync(tc => tc.Id == id);

            if (trainingEntity is null)
            {
                return ServiceResult.NotFound();
            }

            _db.TrainingCourses.Remove(trainingEntity);
            await _db.SaveChangesAsync();
            return ServiceResult.NoContent();

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
