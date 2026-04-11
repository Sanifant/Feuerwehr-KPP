using de.openelp.feuerwehr.domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.Interfaces
{
    public interface IApiService
    {
        Task Create(InventoryItem item);
        Task<List<InventoryItem>> GetAll();
        InventoryCategory[] GetInventoryCategories();
    }
}