using de.openelp.feuerwehr.Common.Models.Dto;

namespace de.openelp.feuerwehr.Server.Services
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
