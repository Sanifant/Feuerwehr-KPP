using System;
using System.Collections.Generic;
using System.Linq;

using de.openelp.feuerwehr.domain;
using de.openelp.feuerwehr.infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace de.openelp.feuerwehr.infrastructure.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="InventoryRepository"/> class.
    /// </summary>
    [TestClass]
    public class InventoryRepositoryTests
    {
        /// <summary>
        /// Tests that GetAll returns an empty collection when no inventory items exist in the database.
        /// Input: Empty InventoryItems DbSet.
        /// Expected: Empty IEnumerable<InventoryItem>.
        /// </summary>
        [TestMethod]
        public void GetAll_EmptyDatabase_ReturnsEmptyCollection()
        {
            // Arrange
            var emptyData = new List<InventoryItem>();
            var mockDbSet = CreateMockDbSet(emptyData);
            var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            var repository = new InventoryRepository(mockContext.Object);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        /// <summary>
        /// Tests that GetAll returns a single item when only one inventory item exists in the database.
        /// Input: InventoryItems DbSet with one item.
        /// Expected: IEnumerable<InventoryItem> containing the single item.
        /// </summary>
        [TestMethod]
        public void GetAll_SingleItem_ReturnsSingleItemCollection()
        {
            // Arrange
            var testItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                Description = "Test Description",
                PurchaseDate = new DateOnly(2024, 1, 1),
                Location = "Test Location"
            };
            var data = new List<InventoryItem> { testItem };
            var mockDbSet = CreateMockDbSet(data);
            var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            var repository = new InventoryRepository(mockContext.Object);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(testItem.Id, result.First().Id);
            Assert.AreEqual(testItem.Name, result.First().Name);
        }

        /// <summary>
        /// Tests that GetAll returns all items when multiple inventory items exist in the database.
        /// Input: InventoryItems DbSet with multiple items.
        /// Expected: IEnumerable<InventoryItem> containing all items in the same order.
        /// </summary>
        [TestMethod]
        public void GetAll_MultipleItems_ReturnsAllItems()
        {
            // Arrange
            var testItems = new List<InventoryItem>
            {
                new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Item 1",
                    Description = "Description 1",
                    PurchaseDate = new DateOnly(2024, 1, 1),
                    Location = "Location 1"
                },
                new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Item 2",
                    Description = "Description 2",
                    PurchaseDate = new DateOnly(2024, 2, 1),
                    Location = "Location 2"
                },
                new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Item 3",
                    Description = "Description 3",
                    PurchaseDate = new DateOnly(2024, 3, 1),
                    Location = "Location 3"
                }
            };
            var mockDbSet = CreateMockDbSet(testItems);
            var mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            var repository = new InventoryRepository(mockContext.Object);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            CollectionAssert.AreEqual(testItems.Select(i => i.Id).ToList(), result.Select(i => i.Id).ToList());
        }

        /// <summary>
        /// Creates a mock DbSet with the specified data for testing purposes.
        /// </summary>
        /// <param name="data">The list of inventory items to include in the mock DbSet.</param>
        /// <returns>A mock DbSet configured to behave like an in-memory collection.</returns>
        private static Mock<DbSet<InventoryItem>> CreateMockDbSet(List<InventoryItem> data)
        {
            var queryable = data.AsQueryable();
            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return mockDbSet;
        }

        /// <summary>
        /// Tests that the constructor successfully creates an instance when provided with a valid dbContext.
        /// </summary>
        [TestMethod]
        public void Constructor_ValidDbContext_CreatesInstance()
        {
            // Arrange
            var mockDbContext = new Mock<AppDbContext>();

            // Act
            var repository = new InventoryRepository(mockDbContext.Object);

            // Assert
            Assert.IsNotNull(repository);
        }

        /// <summary>
        /// Tests that Add method calls DbSet.Add and DbContext.SaveChanges when a valid item is provided.
        /// Input: Valid InventoryItem instance.
        /// Expected: DbSet.Add is called once with the item, and DbContext.SaveChanges is called once.
        /// </summary>
        [TestMethod]
        public void Add_ValidItem_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            InventoryItem testItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item",
                Description = "Test Description",
                PurchaseDate = DateOnly.FromDateTime(DateTime.Now),
                Location = "Test Location"
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Add method with minimal required properties calls DbSet.Add and SaveChanges.
        /// Input: InventoryItem with only required properties set.
        /// Expected: DbSet.Add is called once and SaveChanges is called once.
        /// </summary>
        [TestMethod]
        public void Add_ItemWithMinimalProperties_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            InventoryItem testItem = new InventoryItem
            {
                Name = "Minimal Item"
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Add method with special characters in string properties works correctly.
        /// Input: InventoryItem with special characters in Name, Description, and Location.
        /// Expected: DbSet.Add is called once with the item containing special characters.
        /// </summary>
        [TestMethod]
        public void Add_ItemWithSpecialCharacters_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            InventoryItem testItem = new InventoryItem
            {
                Name = "Test<>\"Item&'",
                Description = "Line1\nLine2\rLine3\tTabbed",
                Location = "Location with émojis 🔥 and symbols ™©®"
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Add method with empty Guid ID works correctly.
        /// Input: InventoryItem with Guid.Empty as Id.
        /// Expected: DbSet.Add is called once with the item.
        /// </summary>
        [TestMethod]
        public void Add_ItemWithEmptyGuidId_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            InventoryItem testItem = new InventoryItem
            {
                Id = Guid.Empty,
                Name = "Item with Empty ID"
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Add method with boundary date values works correctly.
        /// Input: InventoryItem with DateOnly.MinValue as PurchaseDate.
        /// Expected: DbSet.Add is called once with the item.
        /// </summary>
        [TestMethod]
        public void Add_ItemWithMinDateValue_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            InventoryItem testItem = new InventoryItem
            {
                Name = "Item with Min Date",
                PurchaseDate = DateOnly.MinValue
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Add method with very long string values works correctly.
        /// Input: InventoryItem with very long strings in Name, Description, and Location.
        /// Expected: DbSet.Add is called once with the item.
        /// </summary>
        [TestMethod]
        public void Add_ItemWithVeryLongStrings_CallsDbSetAddAndSaveChanges()
        {
            // Arrange
            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            Mock<AppDbContext> mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);
            string longString = new string('x', 10000);
            InventoryItem testItem = new InventoryItem
            {
                Name = longString,
                Description = longString,
                Location = longString
            };

            // Act
            repository.Add(testItem);

            // Assert
            mockDbSet.Verify(d => d.Add(testItem), Times.Once);
            mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Delete method calls Remove on DbSet with correct InventoryItem and calls SaveChanges.
        /// Input: A valid non-empty Guid.
        /// Expected: Remove is called with InventoryItem having the specified Id, and SaveChanges is called once.
        /// </summary>
        [TestMethod]
        [DataRow("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d")]
        [DataRow("00000000-0000-0000-0000-000000000000")]
        [DataRow("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        public void Delete_WithGuid_CallsRemoveAndSaveChanges(string guidString)
        {
            // Arrange
            var id = Guid.Parse(guidString);
            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            var mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);

            // Act
            repository.Delete(id);

            // Assert
            mockDbSet.Verify(
                ds => ds.Remove(It.Is<InventoryItem>(item => item.Id == id)),
                Times.Once,
                $"Remove should be called once with InventoryItem having Id {id}");
            mockContext.Verify(c => c.SaveChanges(), Times.Once, "SaveChanges should be called once");
        }

        /// <summary>
        /// Tests that Delete method with Guid.Empty calls Remove and SaveChanges without validation.
        /// Input: Guid.Empty.
        /// Expected: Remove is called with InventoryItem having Id = Guid.Empty, and SaveChanges is called.
        /// </summary>
        [TestMethod]
        public void Delete_WithEmptyGuid_CallsRemoveAndSaveChanges()
        {
            // Arrange
            var id = Guid.Empty;
            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            var mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);

            // Act
            repository.Delete(id);

            // Assert
            mockDbSet.Verify(
                ds => ds.Remove(It.Is<InventoryItem>(item => item.Id == Guid.Empty)),
                Times.Once,
                "Remove should be called with InventoryItem having Id = Guid.Empty");
            mockContext.Verify(c => c.SaveChanges(), Times.Once, "SaveChanges should be called once");
        }

        /// <summary>
        /// Tests that Delete method creates a new InventoryItem instance for removal.
        /// Input: A valid Guid.
        /// Expected: Remove is called with a newly created InventoryItem (not null) with matching Id.
        /// </summary>
        [TestMethod]
        public void Delete_WithValidGuid_CreatesNewInventoryItemForRemoval()
        {
            // Arrange
            var id = Guid.NewGuid();
            InventoryItem? capturedItem = null;
            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            mockDbSet.Setup(ds => ds.Remove(It.IsAny<InventoryItem>()))
                .Callback<InventoryItem>(item => capturedItem = item);

            var mockContext = new Mock<AppDbContext>();
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);

            // Act
            repository.Delete(id);

            // Assert
            Assert.IsNotNull(capturedItem, "Remove should be called with a non-null InventoryItem");
            Assert.AreEqual(id, capturedItem.Id, $"The InventoryItem passed to Remove should have Id {id}");
        }

        /// <summary>
        /// Tests that GetById returns the correct inventory item when it exists in the database.
        /// </summary>
        [TestMethod]
        public void GetById_ExistingId_ReturnsInventoryItem()
        {
            // Arrange
            Guid testId = Guid.NewGuid();
            InventoryItem expectedItem = new InventoryItem
            {
                Id = testId,
                Name = "Test Item",
                Description = "Test Description",
                Location = "Test Location"
            };

            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            mockDbSet.Setup(m => m.Find(testId)).Returns(expectedItem);

            Mock<AppDbContext> mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);

            // Act
            InventoryItem result = repository.GetById(testId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedItem.Id, result.Id);
            Assert.AreEqual(expectedItem.Name, result.Name);
            mockDbSet.Verify(m => m.Find(testId), Times.Once);
        }

        /// <summary>
        /// Tests that GetById returns the correct item when Guid.Empty exists in the database.
        /// </summary>
        [TestMethod]
        public void GetById_ExistingEmptyGuid_ReturnsInventoryItem()
        {
            // Arrange
            Guid emptyId = Guid.Empty;
            InventoryItem expectedItem = new InventoryItem
            {
                Id = emptyId,
                Name = "Empty GUID Item",
                Description = "Item with empty GUID",
                Location = "Storage"
            };

            Mock<DbSet<InventoryItem>> mockDbSet = new Mock<DbSet<InventoryItem>>();
            mockDbSet.Setup(m => m.Find(emptyId)).Returns(expectedItem);

            Mock<AppDbContext> mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);

            InventoryRepository repository = new InventoryRepository(mockContext.Object);

            // Act
            InventoryItem result = repository.GetById(emptyId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedItem.Id, result.Id);
            Assert.AreEqual(expectedItem.Name, result.Name);
            mockDbSet.Verify(m => m.Find(emptyId), Times.Once);
        }

        /// <summary>
        /// Tests that Update successfully updates an existing item and calls SaveChanges.
        /// Input: An item that exists in the database.
        /// Expected: Update and SaveChanges are called without throwing an exception.
        /// </summary>
        [TestMethod]
        public void Update_ItemExists_UpdatesItemAndSavesChanges()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new InventoryItem { Id = itemId, Name = "Test Item", Location = "Test Location" };

            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            var data = new List<InventoryItem> { item }.AsQueryable();

            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = new Mock<AppDbContext>(MockBehavior.Strict, new object[] { new DbContextOptionsBuilder<AppDbContext>().Options });
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);

            // Act
            repository.Update(item);

            // Assert
            mockDbSet.Verify(m => m.Update(item), Times.Once);
            mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Update successfully handles an item with Guid.Empty as ID.
        /// Input: An item with Guid.Empty ID that exists in the database.
        /// Expected: Update and SaveChanges are called without throwing an exception.
        /// </summary>
        [TestMethod]
        public void Update_ItemWithEmptyGuid_UpdatesItemAndSavesChanges()
        {
            // Arrange
            var item = new InventoryItem { Id = Guid.Empty, Name = "Test Item", Location = "Test Location" };

            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            var data = new List<InventoryItem> { item }.AsQueryable();

            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = new Mock<AppDbContext>(MockBehavior.Strict, new object[] { new DbContextOptionsBuilder<AppDbContext>().Options });
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);

            // Act
            repository.Update(item);

            // Assert
            mockDbSet.Verify(m => m.Update(item), Times.Once);
            mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Update correctly identifies an existing item when multiple items exist.
        /// Input: Multiple items in database, updating one that exists.
        /// Expected: Update and SaveChanges are called for the correct item.
        /// </summary>
        [TestMethod]
        public void Update_MultipleItemsExist_UpdatesCorrectItem()
        {
            // Arrange
            var itemId1 = Guid.NewGuid();
            var itemId2 = Guid.NewGuid();
            var itemId3 = Guid.NewGuid();
            var item1 = new InventoryItem { Id = itemId1, Name = "Item 1", Location = "Location 1" };
            var item2 = new InventoryItem { Id = itemId2, Name = "Item 2", Location = "Location 2" };
            var item3 = new InventoryItem { Id = itemId3, Name = "Item 3", Location = "Location 3" };

            var mockDbSet = new Mock<DbSet<InventoryItem>>();
            var data = new List<InventoryItem> { item1, item2, item3 }.AsQueryable();

            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<InventoryItem>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = new Mock<AppDbContext>(MockBehavior.Strict, new object[] { new DbContextOptionsBuilder<AppDbContext>().Options });
            mockContext.Setup(c => c.InventoryItems).Returns(mockDbSet.Object);
            mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var repository = new InventoryRepository(mockContext.Object);
            var updatedItem = new InventoryItem { Id = itemId2, Name = "Updated Item 2", Location = "Updated Location 2" };

            // Act
            repository.Update(updatedItem);

            // Assert
            mockDbSet.Verify(m => m.Update(updatedItem), Times.Once);
            mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }
    }
}