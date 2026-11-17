using System.Collections.Generic;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models;

namespace de.openelp.feuerwehr.App.Services
{
    public interface IHydrantService
    {
        Task<HydrantDto> CreateAsync(HydrantDto hydrant);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<HydrantDto>> GetAllAsync();
        Task<HydrantDto> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, HydrantDto hydrant);
    }
}
