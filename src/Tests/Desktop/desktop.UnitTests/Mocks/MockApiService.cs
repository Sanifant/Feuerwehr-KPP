using de.openelp.feuerwehr.desktop.Interfaces;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace de.openelp.feuerwehr.desktop.UnitTests.Mocks
{
    internal class MockApiService : IApiService
    {
        public List<InventoryItem> Items { get; set; } = new List<InventoryItem>();
        public List<InventoryCategory> Categories { get; set; } = new List<InventoryCategory>();

        public Task Create(InventoryItem item)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task<List<InventoryItem>> GetAll()
        {
            return Task.FromResult(Items);
        }

        public InventoryCategory[] GetInventoryCategories()
        {
            return Categories.ToArray();
        }
    }
}
