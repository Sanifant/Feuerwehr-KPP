using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Xunit;
using Moq;

namespace de.openelp.feuerwehr.application.inventory.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="InventoryService"/> class.
    /// </summary>
    public class InventoryServiceTests
    {
        /// <summary>
        /// Tests that the constructor successfully initializes the service with a valid repository instance.
        /// Input: Valid mocked IInventoryRepository instance.
        /// Expected: InventoryService object is created without throwing any exceptions.
        /// </summary>
        [Fact]
        public void Constructor_ValidRepository_CreatesInstanceSuccessfully()
        {
            // Arrange
            Mock<IInventoryRepository> mockRepository = new Mock<IInventoryRepository>();

            // Act
            InventoryService service = new InventoryService(mockRepository.Object);

            // Assert
            Assert.NotNull(service);
        }

        /// <summary>
        /// Tests that the constructor behavior when passed a null repository.
        /// Input: null repository reference.
        /// Expected: Constructor accepts null (no explicit validation present in code).
        /// </summary>
        [Fact]
        public void Constructor_NullRepository_AcceptsNull()
        {
            // Arrange
            IInventoryRepository? nullRepository = null;

            // Act
            InventoryService service = new InventoryService(nullRepository!);

            // Assert
            Assert.NotNull(service);
        }

        /// <summary>
        /// Tests that CreateItem calls the repository's Add method with the provided item.
        /// Input: A valid InventoryItem instance.
        /// Expected: The repository's Add method is called exactly once with the provided item.
        /// </summary>
        [Fact]
        public void CreateItem_ValidItem_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                Description = "Test Description",
                PurchaseDate = new DateOnly(2024, 1, 15),
                Location = "Test Location"
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem passes the exact same item instance to the repository.
        /// Input: A valid InventoryItem instance.
        /// Expected: The exact same item instance is passed to the repository's Add method.
        /// </summary>
        [Fact]
        public void CreateItem_ValidItem_PassesExactItemToRepository()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            InventoryItem? capturedItem = null;
            mockRepo.Setup(r => r.Add(It.IsAny<InventoryItem>()))
                .Callback<InventoryItem>(item => capturedItem = item);
            var expectedItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Specific Item",
                Description = "Specific Description",
                PurchaseDate = new DateOnly(2023, 12, 1),
                Location = "Specific Location"
            };

            // Act
            service.CreateItem(expectedItem);

            // Assert
            Assert.NotNull(capturedItem);
            Assert.Same(expectedItem, capturedItem);
        }

        /// <summary>
        /// Tests that CreateItem works correctly with minimal item properties.
        /// Input: An InventoryItem with only required properties set.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_MinimalItem_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Name = "Minimal Item"
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with empty string properties.
        /// Input: An InventoryItem with empty strings for string properties.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithEmptyStrings_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Name = string.Empty,
                Description = string.Empty,
                Location = string.Empty
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with very long string properties.
        /// Input: An InventoryItem with very long strings for Name, Description, and Location.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithLongStrings_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var longString = new string('A', 10000);
            var item = new InventoryItem
            {
                Name = longString,
                Description = longString,
                Location = longString
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with special characters in string properties.
        /// Input: An InventoryItem with special characters, control characters, and Unicode in strings.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithSpecialCharacters_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Name = "Item with !@#$%^&*()_+-={}[]|\\:;\"'<>,.?/~`",
                Description = "Description with\nnewlines\tand\ttabs",
                Location = "Location with émojis 🚒🔥 and ümlauts"
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with boundary date values.
        /// Input: An InventoryItem with DateOnly.MinValue for PurchaseDate.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithMinDate_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Name = "Item with min date",
                PurchaseDate = DateOnly.MinValue
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with boundary date values.
        /// Input: An InventoryItem with DateOnly.MaxValue for PurchaseDate.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithMaxDate_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Name = "Item with max date",
                PurchaseDate = DateOnly.MaxValue
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that CreateItem handles item with empty Guid.
        /// Input: An InventoryItem with Guid.Empty for Id.
        /// Expected: The repository's Add method is called once with the item.
        /// </summary>
        [Fact]
        public void CreateItem_ItemWithEmptyGuid_CallsRepositoryAddOnce()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            var service = new InventoryService(mockRepo.Object);
            var item = new InventoryItem
            {
                Id = Guid.Empty,
                Name = "Item with empty GUID"
            };

            // Act
            service.CreateItem(item);

            // Assert
            mockRepo.Verify(r => r.Add(item), Times.Once);
        }

        /// <summary>
        /// Tests that GetAll returns an empty list when the repository returns an empty collection.
        /// </summary>
        [Fact]
        public async Task GetAll_WhenRepositoryReturnsEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            mockRepo.Setup(r => r.GetAll()).Returns(Enumerable.Empty<InventoryItem>());
            var service = new InventoryService(mockRepo.Object);

            // Act
            var result = await service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetAll returns a list with a single item when the repository returns a single item.
        /// </summary>
        [Fact]
        public async Task GetAll_WhenRepositoryReturnsSingleItem_ReturnsListWithSingleItem()
        {
            // Arrange
            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                Description = "Test Description",
                Location = "Test Location"
            };
            var mockRepo = new Mock<IInventoryRepository>();
            mockRepo.Setup(r => r.GetAll()).Returns(new[] { item });
            var service = new InventoryService(mockRepo.Object);

            // Act
            var result = await service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(item.Id, result[0].Id);
            Assert.Equal(item.Name, result[0].Name);
        }

        /// <summary>
        /// Tests that GetAll returns a list with multiple items when the repository returns multiple items.
        /// </summary>
        [Fact]
        public async Task GetAll_WhenRepositoryReturnsMultipleItems_ReturnsListWithAllItems()
        {
            // Arrange
            var items = new[]
            {
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 1", Location = "Location 1" },
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 2", Location = "Location 2" },
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 3", Location = "Location 3" }
            };
            var mockRepo = new Mock<IInventoryRepository>();
            mockRepo.Setup(r => r.GetAll()).Returns(items);
            var service = new InventoryService(mockRepo.Object);

            // Act
            var result = await service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(items[i].Id, result[i].Id);
                Assert.Equal(items[i].Name, result[i].Name);
            }
        }

        /// <summary>
        /// Tests that GetAll returns a completed task and does not delay execution.
        /// </summary>
        [Fact]
        public void GetAll_ReturnsCompletedTask()
        {
            // Arrange
            var mockRepo = new Mock<IInventoryRepository>();
            mockRepo.Setup(r => r.GetAll()).Returns(Enumerable.Empty<InventoryItem>());
            var service = new InventoryService(mockRepo.Object);

            // Act
            var task = service.GetAll();

            // Assert
            Assert.NotNull(task);
            Assert.True(task.IsCompleted);
        }
    }
}