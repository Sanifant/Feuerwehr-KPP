using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace de.openelp.feuerwehr.application.inventory
{
    public interface IInventoryRepository
    {
        IEnumerable<InventoryItem> GetAll();

        InventoryItem GetById(Guid id);

        void Add(InventoryItem item);

        void Update(InventoryItem item);

        void Delete(Guid id);

        IEnumerable<InventoryItem> GetByCategory(Guid category);

        IEnumerable<InventoryItemRelationship> GetRelationships();

        InventoryItemRelationship GetRelationshipById(Guid id);
        IEnumerable<InventoryCategory> GetCategories();
    }
}
