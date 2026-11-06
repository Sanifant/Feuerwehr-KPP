using De.Openelp.Feuerwehr.Common.Models.Dto;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface ITrainingRecordService
    {
        Task<ServiceResult> Get(string? level, string? title);

        Task<ServiceResult> Get(int id);

        Task<ServiceResult> Post(TrainingCourseDto value);

        Task<ServiceResult> Put(int id, TrainingCourseDto value);

        Task<ServiceResult> Delete(int id);
    }
}
