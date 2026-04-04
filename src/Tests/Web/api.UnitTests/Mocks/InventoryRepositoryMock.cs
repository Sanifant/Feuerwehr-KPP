using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace de.openelp.feuerwehr.Api.UnitTests.Mocks
{
    internal class InventoryRepositoryMock : IInventoryRepository
    {
        public IEnumerable<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

        public void Add(InventoryItem item)
        {
            InventoryItems = InventoryItems.Append(item).ToList();
        }

        public void Delete(Guid id)
        {
            InventoryItems = InventoryItems.Where(i => i.Id != id).ToList();
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
                InventoryItems = InventoryItems.Where(i => i.Id != item.Id).ToList();
                InventoryItems = InventoryItems.Append(item).ToList();
            }
        }
    }
}
