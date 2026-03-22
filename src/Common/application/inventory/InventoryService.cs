using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.application.inventory
{
    public class InventoryService
    {

        private readonly IInventoryRepository _repo;

        public InventoryService(IInventoryRepository repo)
        {
            _repo = repo;
        }

        public void CreateItem(InventoryItem item)
        {
            _repo.Add(item);
        }

        public Task<List<InventoryItem>> GetAll()
        {
            return Task.FromResult(_repo.GetAll().ToList());
        }
    }
}
