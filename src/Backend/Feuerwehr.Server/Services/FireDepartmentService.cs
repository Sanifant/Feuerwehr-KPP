using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public sealed class FireDepartmentService : IFireDepartmentService
    {
        private readonly FeuerwehrDbContext _db;

        public FireDepartmentService(FeuerwehrDbContext db)
        {
            this._db = db;
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Adds the operation.
        /// </summary>
        /// <param name="fireDepartmentDto">The fire Department Dto.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
