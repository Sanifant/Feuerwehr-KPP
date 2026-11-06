using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Implements fire department operations for the API application layer.
    /// </summary>
    public sealed class FireDepartmentService : IFireDepartmentService
    {
        private readonly FeuerwehrDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="FireDepartmentService" /> class with its required dependencies.
        /// </summary>
        /// <param name="db">The database context for querying and persisting application records.</param>
        public FireDepartmentService(FeuerwehrDbContext db)
        {
            this._db = db;
        }

        /// <summary>
        /// Retrieves fire department records visible to the caller.
        /// </summary>
        /// <returns>A task whose result is an application result containing the matching fire department records, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Get()
        {
            List<FireDepartmentDto> fireDepartments = await this._db.FireDepartments
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

        /// <summary>
        /// Creates fire department records from the validated input.
        /// </summary>
        /// <param name="fireDepartmentDto">The fire department details to persist.</param>
        /// <returns>A task whose result is an application result containing the saved fire department record or success status, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto)
        {
            FireDepartment entity = new ()
            {
                Name = fireDepartmentDto.Name,
                ContactPersonName = fireDepartmentDto.ContactPersonName,
                ContactPersonEmail = fireDepartmentDto.ContactPersonEmail
            };

            _ = this._db.FireDepartments.Add(entity);
            _ = await this._db.SaveChangesAsync();

            FireDepartmentDto createdDto = new ()
            {
                Id = entity.Id,
                Name = entity.Name,
                ContactPersonName = entity.ContactPersonName,
                ContactPersonEmail = entity.ContactPersonEmail,
                TrainingCourses = new List<FireDepartmentTrainingCourseDto>()
            };

            return ServiceResult.CreatedAtAction(nameof(this.Get), new { id = createdDto.Id }, createdDto);
        }

        /// <summary>
        /// Retrieves fire department records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the fire department to retrieve.</param>
        /// <returns>A task whose result is an application result containing the matching fire department record, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Get(int id)
        {
            FireDepartment? fireDepartment = await this._db.FireDepartments
                .Include(fd => fd.FireDepartmentTrainingCourses)
                .ThenInclude(fdTc => fdTc.TrainingCourse)
                .FirstOrDefaultAsync(fd => fd.Id == id);

            if (fireDepartment is null)
            {
                return ServiceResult.NotFound();
            }

            FireDepartmentDto dto = new ()
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
}
