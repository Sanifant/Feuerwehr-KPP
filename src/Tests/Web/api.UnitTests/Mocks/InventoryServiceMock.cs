using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
