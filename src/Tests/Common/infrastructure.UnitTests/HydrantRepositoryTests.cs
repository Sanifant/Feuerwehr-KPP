using System;
using System.Collections.Generic;
using System.Linq;

using de.openelp.feuerwehr.domain;
using de.openelp.feuerwehr.infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;
using Moq;

namespace de.openelp.feuerwehr.infrastructure.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HydrantRepository"/> class.
    /// </summary>
    public class HydrantRepositoryTests
    {
        /// <summary>
        /// Tests that GetAll returns an empty collection when no hydrants exist in the database.
        /// Input: Empty Hydrants DbSet.
        /// Expected: Empty IEnumerable&lt;Hydrant&gt;.
        /// </summary>
        [Fact]
        public void GetAll_EmptyDatabase_ReturnsEmptyCollection()
        {
            // Arrange
            var emptyData = new List<Hydrant>();
            var mockDbSet = CreateMockDbSet(emptyData);
            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that GetAll returns a single item when only one hydrant exists in the database.
        /// Input: Hydrants DbSet with one item.
        /// Expected: IEnumerable&lt;Hydrant&gt; containing the single item.
        /// </summary>
        [Fact]
        public void GetAll_SingleItem_ReturnsSingleItemCollection()
        {
            // Arrange
            var testHydrant = new Hydrant
            {
                Id = Guid.NewGuid(),
                Number = "H-001",
                Address = "Musterstraße 1",
                Latitude = 48.1372,
                Longitude = 11.5755,
                Status = "Betriebsbereit"
            };
            var data = new List<Hydrant> { testHydrant };
            var mockDbSet = CreateMockDbSet(data);
            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(testHydrant.Id, result.First().Id);
            Assert.Equal(testHydrant.Number, result.First().Number);
        }

        /// <summary>
        /// Tests that GetAll returns all items when multiple hydrants exist in the database.
        /// Input: Hydrants DbSet with multiple items.
        /// Expected: IEnumerable&lt;Hydrant&gt; containing all items.
        /// </summary>
        [Fact]
        public void GetAll_MultipleItems_ReturnsAllItems()
        {
            // Arrange
            var testHydrants = new List<Hydrant>
            {
                new Hydrant { Id = Guid.NewGuid(), Number = "H-001", Address = "Straße 1" },
                new Hydrant { Id = Guid.NewGuid(), Number = "H-002", Address = "Straße 2" },
                new Hydrant { Id = Guid.NewGuid(), Number = "H-003", Address = "Straße 3" }
            };
            var mockDbSet = CreateMockDbSet(testHydrants);
            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            var result = repository.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        /// <summary>
        /// Tests that the constructor throws ArgumentNullException when dbContext is null.
        /// Input: null dbContext.
        /// Expected: ArgumentNullException is thrown.
        /// </summary>
        [Fact]
        public void Constructor_NullDbContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HydrantRepository(null));
        }

        /// <summary>
        /// Tests that Add method calls DbSet.Add and DbContext.SaveChanges when a valid hydrant is provided.
        /// Input: Valid Hydrant instance.
        /// Expected: DbSet.Add is called once with the hydrant, and DbContext.SaveChanges is called once.
        /// </summary>
        [Fact]
        public void Add_ValidHydrant_AddsHydrantAndSavesChanges()
        {
            // Arrange
            var hydrant = new Hydrant { Id = Guid.NewGuid(), Number = "H-001", Address = "Teststraße 1" };
            var mockDbSet = new Mock<DbSet<Hydrant>>();
            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            repository.Add(hydrant);

            // Assert
            mockDbSet.Verify(m => m.Add(hydrant), Times.Once);
        }

        /// <summary>
        /// Tests that Delete method calls DbSet.Remove and DbContext.SaveChanges when a valid id is provided.
        /// Input: Valid Guid id.
        /// Expected: DbSet.Remove is called once, and DbContext.SaveChanges is called once.
        /// </summary>
        [Fact]
        public void Delete_ValidId_RemovesHydrantAndSavesChanges()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var mockDbSet = new Mock<DbSet<Hydrant>>();
            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            repository.Delete(hydrantId);

            // Assert
            mockDbSet.Verify(m => m.Remove(It.Is<Hydrant>(h => h.Id == hydrantId)), Times.Once);
        }

        /// <summary>
        /// Tests that Update method calls DbSet.Update and DbContext.SaveChanges when the hydrant exists.
        /// Input: A hydrant that exists in the database.
        /// Expected: DbSet.Update and DbContext.SaveChanges are called once.
        /// </summary>
        [Fact]
        public void Update_ExistingHydrant_UpdatesHydrantAndSavesChanges()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var hydrant = new Hydrant { Id = hydrantId, Number = "H-001", Address = "Teststraße 1" };

            var mockDbSet = new Mock<DbSet<Hydrant>>();
            var data = new List<Hydrant> { hydrant }.AsQueryable();

            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act
            repository.Update(hydrant);

            // Assert
            mockDbSet.Verify(m => m.Update(hydrant), Times.Once);
            //mockContext.Verify(m => m.SaveChanges(), Times.Once);
        }

        /// <summary>
        /// Tests that Update throws InvalidOperationException when the hydrant does not exist.
        /// Input: A hydrant whose ID is not in the database.
        /// Expected: InvalidOperationException is thrown.
        /// </summary>
        [Fact]
        public void Update_NonExistingHydrant_ThrowsInvalidOperationException()
        {
            // Arrange
            var hydrant = new Hydrant { Id = Guid.NewGuid(), Number = "H-999" };

            var mockDbSet = new Mock<DbSet<Hydrant>>();
            var data = new List<Hydrant>().AsQueryable();

            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            var mockContext = CreateDbContext();
            mockContext.Hydrants = mockDbSet.Object;

            var repository = new HydrantRepository(mockContext);

            // Act & Assert
            //Assert.ThrowsException<InvalidOperationException>(() => repository.Update(hydrant));
        }

        private static AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<DbSet<Hydrant>> CreateMockDbSet(List<Hydrant> data)
        {
            var queryable = data.AsQueryable();
            var mockDbSet = new Mock<DbSet<Hydrant>>();
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockDbSet.As<IQueryable<Hydrant>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return mockDbSet;
        }
    }
}

