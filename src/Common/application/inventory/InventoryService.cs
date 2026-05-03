using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.application.inventory
{
    public class InventoryService : IInventoryService
    {

        private readonly IInventoryRepository _repo;

        public InventoryService(IInventoryRepository repo) => _repo = repo;

        public void CreateItem(InventoryItem item)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            item.Id = Guid.NewGuid();

            _repo.Add(item);
        }

        public void DeleteItem(Guid id)
        {
            _repo.Delete(id);
        }

        public Task<List<InventoryItem>> GetAll()
        {
            return Task.FromResult(_repo.GetAll().ToList());
        }

        public Task<List<InventoryItem>> GetByCategory(Guid categoryId)
        {
            return Task.FromResult(_repo.GetByCategory(categoryId).ToList());
        }

        public Task<InventoryItem> GetById(Guid id)
        {
            return Task.FromResult(_repo.GetById(id));
        }

        public Task<List<InventoryCategory>> GetCategories()
        {
            return Task.FromResult(_repo.GetCategories().ToList()); 
        }

        public void LinkItems(Guid parentId, Guid childId, ItemRelationshipLabel label)
        {
            var relationship = new InventoryItemRelationship
            {
                Id = Guid.NewGuid(),
                ParentItemId = parentId,
                ChildItemId = childId,
                RelationshipLabel = label
            };

            var parentItem = _repo.GetById(parentId);
            relationship.ParentItem = parentItem;
            relationship.ChildItemId = childId;

            _repo.Update(parentItem);
        }

        public void UpdateItem(InventoryItem item)
        {
            _repo.Update(item);
        }
    }
}
