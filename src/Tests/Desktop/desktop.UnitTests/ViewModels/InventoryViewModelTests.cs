using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace de.openelp.feuerwehr.desktop.UnitTests.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public partial class InventoryViewModelTests
    {
        [Fact]
        public void Constructor_WhenCalled_CreatesInstanceSuccessfully()
        {
            // Arrange
            var mockApiService = new Mocks.MockApiService();
            // Act
            var viewModel = new InventoryViewModel(mockApiService);
            // Assert
            Assert.NotNull(viewModel);
            Assert.IsType<InventoryViewModel>(viewModel);
        }

        [Fact]
        public void Constructor_WhenCalled_PopulatesItemsCollection()
        {
            // Arrange
            var mockApiService = new Mocks.MockApiService();
            mockApiService.Items = new List<InventoryItem>
            {
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 1", CategoryId = Guid.NewGuid(), Quantity = 10 },
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 2", CategoryId = Guid.NewGuid(), Quantity = 5 }
            };

            // Act
            var viewModel = new InventoryViewModel(mockApiService);

            // Assert
            Assert.NotEmpty(viewModel.Items);
        }

        [Fact]
        public void LoadItems_WhenCalled_PopulatesItemsCollection()
        {
            // Arrange
            var mockApiService = new Mocks.MockApiService();
            mockApiService.Items = new List<InventoryItem>
            {
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 1", CategoryId = Guid.NewGuid(), Quantity = 10 },
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 2", CategoryId = Guid.NewGuid(), Quantity = 5 }
            };
            var viewModel = new InventoryViewModel(mockApiService);

            Assert.Equal(2, viewModel.Items.Count);

            // Act
            mockApiService.Items.Add(new InventoryItem { Id = Guid.NewGuid(), Name = "Item 3", CategoryId = Guid.NewGuid(), Quantity = 15 });
            viewModel.LoadItemsCommand.Execute(null);

            // Assert
            Assert.Equal(3, viewModel.Items.Count);
        }
    }
}
