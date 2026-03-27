using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.application.inventory
{
    public interface IInventoryService
    {
        void CreateItem(InventoryItem item);
        Task<List<InventoryItem>> GetAll();
    }
}