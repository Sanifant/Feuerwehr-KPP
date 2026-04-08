using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.application.inventory
{
    public interface IInventoryService
    {
        void CreateItem(InventoryItem item);

        Task<List<InventoryItem>> GetAll();

        Task<InventoryItem> GetById(Guid id);

        Task<List<InventoryItem>> GetByCategory(Guid categoryId);

        void UpdateItem(InventoryItem item);

        void DeleteItem(Guid id);

        void LinkItems(Guid parentId, Guid childId, ItemRelationshipLabel label);

        Task<List<InventoryCategory>> GetCategories();
    }
}