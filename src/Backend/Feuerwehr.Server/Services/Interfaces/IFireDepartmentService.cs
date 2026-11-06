using de.openelp.feuerwehr.Common.Models.Dto;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IFireDepartmentService
    {
        Task<ServiceResult> Get();
        Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto);
        Task<ServiceResult> Get(int id);
    }
}
