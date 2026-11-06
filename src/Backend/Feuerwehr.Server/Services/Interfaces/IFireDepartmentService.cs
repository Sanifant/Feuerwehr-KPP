using De.Openelp.Feuerwehr.Common.Models.Dto;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IFireDepartmentService
    {
        Task<ServiceResult> Get();

        Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto);

        Task<ServiceResult> Get(int id);
    }
}
