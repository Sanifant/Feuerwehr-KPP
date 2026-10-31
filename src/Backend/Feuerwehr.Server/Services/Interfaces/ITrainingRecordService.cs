using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.EntityFrameworkCore;

using De.Openelp.Feuerwehr.Server.Services;

namespace De.Openelp.Feuerwehr.Server.Services;

public interface ITrainingRecordService
{
    Task<ServiceResult> Get(string? level, string? title);
    Task<ServiceResult> Get(int id);
    Task<ServiceResult> Post(TrainingCourseDto value);
    Task<ServiceResult> Put(int id, TrainingCourseDto value);
    Task<ServiceResult> Delete(int id);
}
