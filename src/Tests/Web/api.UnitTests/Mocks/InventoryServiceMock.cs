using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.Api.UnitTests.Mocks
{
    internal class InventoryServiceMock : IInventoryService
    {
        public List<InventoryItem> Items { get; set; } = new List<InventoryItem>();

        public void CreateItem(InventoryItem item)
        {
            Items.Add(item);
        }

        public Task<List<InventoryItem>> GetAll()
        {
            return Task.FromResult(Items);
        }

        public Task<InventoryItem> GetById(Guid id)
        {
            return Task.FromResult(Items.FirstOrDefault(i => i.Id == id)!);
        }

        public Task<List<InventoryItem>> GetByCategory(Guid categoryId)
        {
            return Task.FromResult(new List<InventoryItem>());
        }

        public void UpdateItem(InventoryItem item)
        {
            var index = Items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                Items[index] = item;
                return;
            }

            Items.Add(item);
        }

        public void DeleteItem(Guid id)
        {
            Items.RemoveAll(i => i.Id == id);
        }

        public void LinkItems(Guid parentId, Guid childId, ItemRelationshipLabel label)
        {
            // Not needed for current controller tests.
        }
    }
}
