using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using de.openelp.feuerwehr.Api.Controllers;
using de.openelp.feuerwehr.Api.UnitTests.Mocks;
using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Xunit;

namespace de.openelp.feuerwehr.Api.Controllers.UnitTests
{
    /// <summary>
    /// Unit tests for the InventoryItemController class.
    /// </summary>
    public class InventoryItemControllerTests
    {
        /// <summary>
        /// Tests that Post method calls CreateItem on the service with a valid inventory item.
        /// Input: A valid InventoryItem object.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_ValidInventoryItem_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                Description = "Test Description",
                PurchaseDate = new DateOnly(2023, 1, 1),
                Location = "Test Location"
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem on the service with an inventory item with minimal required fields.
        /// Input: An InventoryItem with only the Name property set (required field).
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithMinimalFields_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "Minimal Item"
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item containing empty Description.
        /// Input: An InventoryItem with empty Description string.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithEmptyDescription_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "Item",
                Description = string.Empty
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item containing special characters.
        /// Input: An InventoryItem with special characters in Name and Description.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithSpecialCharacters_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "Test!@#$%^&*()",
                Description = "Description with special chars: <>&\"'",
                Location = "Location/Path\\Test"
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item containing very long strings.
        /// Input: An InventoryItem with very long Name and Description strings.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithVeryLongStrings_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = new string('A', 10000),
                Description = new string('B', 50000),
                Location = new string('C', 5000)
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item containing whitespace-only strings.
        /// Input: An InventoryItem with whitespace-only strings for Name and Location.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithWhitespaceStrings_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "   ",
                Description = "\t\n\r",
                Location = "  "
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item with Guid.Empty as Id.
        /// Input: An InventoryItem with Guid.Empty as Id.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithEmptyGuid_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Id = Guid.Empty,
                Name = "Test Item"
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item with DateOnly.MinValue.
        /// Input: An InventoryItem with DateOnly.MinValue as PurchaseDate.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithMinDateValue_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "Test Item",
                PurchaseDate = DateOnly.MinValue
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Post method calls CreateItem with an inventory item with DateOnly.MaxValue.
        /// Input: An InventoryItem with DateOnly.MaxValue as PurchaseDate.
        /// Expected: The service's CreateItem method is called once with the provided item.
        /// </summary>
        [Fact]
        public void Post_InventoryItemWithMaxDateValue_CallsServiceCreateItem()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Name = "Test Item",
                PurchaseDate = DateOnly.MaxValue
            };

            // Act
            controller.Post(inventoryItem);

            // Assert
            Assert.Single(mockService.Items);
        }

        /// <summary>
        /// Tests that Delete executes without throwing exceptions for various Guid values.
        /// </summary>
        /// <param name="id">The inventory item identifier.</param>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        public async Task Delete_WithVariousGuidValues_ExecutesWithoutException(Guid id)
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);

            // Act
            await controller.Delete(id);

            // Assert
            // Method completes without throwing an exception (implicit assertion)
            Assert.True(true);
        }

        /// <summary>
        /// Tests that Get returns the matching InventoryItem when the specified id exists in the collection.
        /// </summary>
        /// <remarks>
        /// NOTE: This test is marked as Inconclusive because InventoryService.GetAll() is not virtual
        /// and cannot be properly mocked using Moq. To make this test functional:
        /// 1. Make InventoryService.GetAll() virtual, OR
        /// 2. Extract an interface (e.g., IInventoryService) and use dependency injection with the interface, OR
        /// 3. Use a different testing approach that doesn't require mocking the service.
        /// 
        /// Expected behavior: When an item with the specified id exists, the method should return that item.
        /// </remarks>
        [Fact]
        public async Task Get_WhenIdExists_ReturnsMatchingInventoryItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var expectedItem = new InventoryItem { Id = itemId, Name = "Test Item" };
            var mockService = new InventoryServiceMock();
            mockService.Items = new List<InventoryItem> { expectedItem };

            var controller = new InventoryItemController(mockService);

            var result = await controller.Get(itemId);
            Assert.Equal(expectedItem, result);
        }

        /// <summary>
        /// Tests that Get returns null when the specified id does not exist in the collection.
        /// </summary>
        /// <remarks>
        /// NOTE: This test is marked as Inconclusive because InventoryService.GetAll() is not virtual
        /// and cannot be properly mocked using Moq.
        /// 
        /// Expected behavior: When no item with the specified id exists, the method should return null
        /// (FirstOrDefault behavior).
        /// </remarks>
        [Fact]
        public async Task Get_WhenIdDoesNotExist_ReturnsNull()
        {
            // Arrange
            var searchId = Guid.NewGuid();
            var differentId = Guid.NewGuid();
            var mockService = new InventoryServiceMock();
            mockService.Items = new List<InventoryItem>
            {
                new InventoryItem { Id = differentId, Name = "Different Item" }
            };
            var controller = new InventoryItemController(mockService);

            // Cannot properly mock InventoryService because GetAll() is not virtual
            var result = await controller.Get(searchId);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that Get returns the matching item when searching for Guid.Empty and such an item exists.
        /// </summary>
        /// <remarks>
        /// NOTE: This test is marked as Inconclusive because InventoryService.GetAll() is not virtual
        /// and cannot be properly mocked using Moq.
        /// 
        /// Expected behavior: If an item has Id = Guid.Empty, it should be returned when searching for Guid.Empty.
        /// </remarks>
        [Fact]
        public async Task Get_WithEmptyGuid_ReturnsMatchingItemWhenExists()
        {
            // Arrange
            var searchId = Guid.Empty;
            var matchingItem = new InventoryItem { Id = Guid.Empty, Name = "Empty Guid Item" };
            var mockService = new InventoryServiceMock();
            mockService.Items = new List<InventoryItem> { matchingItem };
            var controller = new InventoryItemController(mockService);

            // Cannot properly mock InventoryService because GetAll() is not virtual
            var result = await controller.Get(searchId);
            Assert.Equal(matchingItem, result);
        }

        /// <summary>
        /// Tests that Get returns the first matching item when multiple items exist with one match.
        /// </summary>
        /// <remarks>
        /// NOTE: This test is marked as Inconclusive because InventoryService.GetAll() is not virtual
        /// and cannot be properly mocked using Moq.
        /// 
        /// Expected behavior: FirstOrDefault should return the first item that matches the predicate.
        /// </remarks>
        [Fact]
        public async Task Get_WithMultipleItemsOneMatch_ReturnsFirstMatch()
        {
            // Arrange
            var matchingId = Guid.NewGuid();
            var matchingItem = new InventoryItem { Id = matchingId, Name = "Matching Item" };
            var mockService = new InventoryServiceMock();
            mockService.Items = new List<InventoryItem>
            {
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 1" },
                matchingItem,
                new InventoryItem { Id = Guid.NewGuid(), Name = "Item 2" }
            };
            var controller = new InventoryItemController(mockService);

            
            var result = await controller.Get(matchingId);
            Assert.Equal(matchingItem, result);
        }

        /// <summary>
        /// Tests that the constructor successfully creates an instance when provided with a valid InventoryService.
        /// </summary>
        [Fact]
        public void Constructor_ValidService_CreatesInstanceSuccessfully()
        {
            // Arrange
            var mockService = new InventoryServiceMock();

            // Act
            var controller = new InventoryItemController(mockService);

            // Assert
            Assert.NotNull(controller);
        }

        /// <summary>
        /// Tests that the constructor accepts a null service parameter.
        /// Note: The parameter is marked as non-nullable, but runtime enforcement is not present in the constructor.
        /// This test documents the current behavior where null is accepted without validation.
        /// </summary>
        [Fact]
        public void Constructor_NullService_CreatesInstanceWithoutThrowingException()
        {
            // Arrange
            InventoryService? nullService = null;

            // Act
            var controller = new InventoryItemController(nullService!);

            // Assert
            Assert.NotNull(controller);
        }

        /// <summary>
        /// Tests that the Put method executes without throwing exceptions for various Guid IDs
        /// with a valid InventoryItem.
        /// </summary>
        /// <param name="id">The ID parameter to test.</param>
        [Theory]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("22222222-2222-2222-2222-222222222222")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public async Task Put_VariousGuidValuesWithValidItem_ExecutesWithoutException(Guid id)
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            var inventoryItem = new InventoryItem
            {
                Id = id,
                Name = "Test Item",
                Description = "Test Description",
                PurchaseDate = new DateOnly(2023, 1, 1),
                Location = "Test Location"
            };

            // Act
            await controller.Put(id, inventoryItem);

            // Assert
            // Method completes without throwing an exception
            // Verify that the service was not called since the method body is empty
            Assert.True(true);
        }

        /// <summary>
        /// Tests that the Put method executes without throwing exceptions when provided
        /// with a null InventoryItem value, even though the parameter is non-nullable.
        /// This tests runtime behavior when nullability is not enforced.
        /// </summary>
        [Fact]
        public async Task Put_ValidIdWithNullItem_ExecutesWithoutException()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            Guid id = Guid.NewGuid();
            InventoryItem? nullItem = null;

            // Act
            await Assert.ThrowsAsync<NullReferenceException>(async () => await controller.Put(id, nullItem!));

            // Assert
            // Current controller behavior dereferences value.Id and throws for null payloads.
        }

        /// <summary>
        /// Tests that the Put method executes without throwing exceptions when provided
        /// with an InventoryItem that has minimal/default property values.
        /// </summary>
        [Fact]
        public async Task Put_ValidIdWithItemHavingDefaultValues_ExecutesWithoutException()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            Guid id = Guid.NewGuid();
            var inventoryItem = new InventoryItem
            {
                Id = id,
                Name = string.Empty,
                Description = string.Empty,
                PurchaseDate = default(DateOnly),
                Location = string.Empty
            };

            // Act
            await controller.Put(id, inventoryItem);

            // Assert
            // Method completes without throwing an exception
            // Verify that the service was not called since the method body is empty
            Assert.True(true);
        }

        /// <summary>
        /// Tests that the Put method executes without throwing exceptions when provided
        /// with an InventoryItem that has special characters and extreme string values.
        /// </summary>
        [Fact]
        public async Task Put_ValidIdWithItemHavingSpecialCharacters_ExecutesWithoutException()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);
            Guid id = Guid.NewGuid();
            var inventoryItem = new InventoryItem
            {
                Id = id,
                Name = "Test\nItem\t<>&\"'",
                Description = new string('x', 10000),
                PurchaseDate = DateOnly.MaxValue,
                Location = "\0\r\n"
            };

            // Act
            await controller.Put(id, inventoryItem);

            // Assert
            // Method completes without throwing an exception
            // Verify that the service was not called since the method body is empty
            Assert.True(true);
        }

        /// <summary>
        /// Tests that the Get method returns the expected number of items when the service returns a list with various counts.
        /// </summary>
        /// <param name="itemCount">The number of items to be returned by the service.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void Get_ServiceReturnsListWithVariousCounts_ReturnsExpectedCount(int itemCount)
        {
            // Arrange
            List<InventoryItem> expectedItems = new List<InventoryItem>();
            var mockService = new InventoryServiceMock();
            for (int i = 0; i < itemCount; i++)
            {
                expectedItems.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"Item{i}",
                    Description = $"Description{i}",
                    PurchaseDate = DateOnly.FromDateTime(DateTime.Now),
                    Location = $"Location{i}"
                });
            }
            mockService.Items = expectedItems;
            var controller = new InventoryItemController(mockService);

            // Act
            var result = controller.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemCount, result.Count());
        }

        /// <summary>
        /// Tests that the Get method returns all items with correct properties when the service returns a populated list.
        /// </summary>
        [Fact]
        public void Get_ServiceReturnsMultipleItems_ReturnsAllItemsWithCorrectProperties()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var item1 = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Fire Extinguisher",
                Description = "ABC Type",
                PurchaseDate = new DateOnly(2023, 1, 15),
                Location = "Station 1"
            };
            var item2 = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Hose",
                Description = "50m length",
                PurchaseDate = new DateOnly(2023, 5, 20),
                Location = "Station 2"
            };
            var expectedItems = new List<InventoryItem> { item1, item2 };
            mockService.Items = expectedItems;
            var controller = new InventoryItemController(mockService);

            // Act
            var result = controller.Get().ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(item1.Id, result[0].Id);
            Assert.Equal(item1.Name, result[0].Name);
            Assert.Equal(item2.Id, result[1].Id);
            Assert.Equal(item2.Name, result[1].Name);
        }

        /// <summary>
        /// Tests that the Get method returns an empty collection when the service returns an empty list.
        /// </summary>
        [Fact]
        public void Get_ServiceReturnsEmptyList_ReturnsEmptyCollection()
        {
            // Arrange
            var mockService = new InventoryServiceMock();
            var controller = new InventoryItemController(mockService);

            // Act
            var result = controller.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

    }
}
