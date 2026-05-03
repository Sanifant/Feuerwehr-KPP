using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace de.openelp.feuerwehr.Api.UnitTests.Mocks
{
    internal class InventoryRepositoryMock : IInventoryRepository
    {
        public IEnumerable<InventoryItem> InventoryItems { get; set; } = [];

        public void Add(InventoryItem item)
        {
            InventoryItems = [.. InventoryItems, item];
        }

        public void Delete(Guid id)
        {
            InventoryItems = [.. InventoryItems.Where(i => i.Id != id)];
        }

        public IEnumerable<InventoryItem> GetByCategory(Guid category)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InventoryItemRelationship> GetRelationships()
        {
            throw new NotImplementedException();
        }

        public InventoryItemRelationship GetRelationshipById(Guid id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InventoryItem> GetAll()
        {
            return InventoryItems;
        }

        public InventoryItem GetById(Guid id)
        {
            return InventoryItems.FirstOrDefault(item => item.Id == id);
        }

        public void Update(InventoryItem item)
        {
            var existingItem = InventoryItems.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                InventoryItems = [.. InventoryItems.Where(i => i.Id != item.Id)];
                InventoryItems = [.. InventoryItems, item];
            }
        }

        public IEnumerable<InventoryCategory> GetCategories()
        {
            throw new NotImplementedException();
        }
    }
}
