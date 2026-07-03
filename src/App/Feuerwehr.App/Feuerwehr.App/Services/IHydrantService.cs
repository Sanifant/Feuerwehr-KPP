using Feuerwehr.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Feuerwehr.App.Services
{
    public interface IHydrantService
    {
        Task<Hydrant> CreateAsync(Hydrant hydrant);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Hydrant>> GetAllAsync();
        Task<Hydrant> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, Hydrant hydrant);
    }
}