using System;
using System.Collections.Generic;
using System.Linq;

using de.openelp.feuerwehr.Api.Controllers;
using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace de.openelp.feuerwehr.Api.Controllers.UnitTests
{
    /// <summary>
    /// Unit tests for the HydrantController class.
    /// </summary>
    public class HydrantControllerTests
    {
        /// <summary>
        /// Tests that Post method calls CreateHydrant on the service with a valid hydrant.
        /// Input: A valid Hydrant object.
        /// Expected: The service's CreateHydrant method is called once with the provided hydrant,
        /// and a 201 Created response is returned.
        /// </summary>
        [Fact]
        public void Post_ValidHydrant_CallsServiceCreateHydrant()
        {
            // Arrange
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            var controller = new HydrantController(mockService.Object);
            var hydrant = new Hydrant
            {
                Id = Guid.NewGuid(),
                Number = "H-001",
                Address = "Musterstraße 1",
                Latitude = 48.1372,
                Longitude = 11.5755,
                Status = "Betriebsbereit"
            };

            // Act
            var result = controller.Post(hydrant);

            // Assert
            mockService.Verify(s => s.CreateHydrant(hydrant), Times.Once);
            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        /// <summary>
        /// Tests that Get returns all hydrants from the service.
        /// Input: Service returns a list of two hydrants.
        /// Expected: Controller returns all hydrants.
        /// </summary>
        [Fact]
        public void Get_ServiceReturnsHydrants_ReturnsAllHydrants()
        {
            // Arrange
            var hydrants = new List<Hydrant>
            {
                new Hydrant { Id = Guid.NewGuid(), Number = "H-001", Address = "Straße 1" },
                new Hydrant { Id = Guid.NewGuid(), Number = "H-002", Address = "Straße 2" }
            };
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetAll()).ReturnsAsync(hydrants);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(hydrants, result);
        }

        /// <summary>
        /// Tests that Get by id returns the correct hydrant when it exists.
        /// Input: Valid hydrant ID.
        /// Expected: OkObjectResult containing the hydrant.
        /// </summary>
        [Fact]
        public void GetById_ExistingHydrant_ReturnsOkWithHydrant()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var hydrant = new Hydrant { Id = hydrantId, Number = "H-001", Address = "Teststraße 1" };
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns(hydrant);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Get(hydrantId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.Equal(hydrant, okResult.Value);
        }

        /// <summary>
        /// Tests that Get by id returns NotFound when the hydrant does not exist.
        /// Input: ID of a non-existent hydrant.
        /// Expected: NotFoundResult.
        /// </summary>
        [Fact]
        public void GetById_NonExistingHydrant_ReturnsNotFound()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns((Hydrant?)null);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Get(hydrantId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        /// <summary>
        /// Tests that Put returns NoContent and calls UpdateHydrant when the hydrant exists.
        /// Input: Valid hydrant ID and hydrant data.
        /// Expected: NoContentResult and UpdateHydrant called once.
        /// </summary>
        [Fact]
        public void Put_ExistingHydrant_ReturnsNoContent()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var existing = new Hydrant { Id = hydrantId, Number = "H-001", Address = "Teststraße 1" };
            var hydrant = new Hydrant { Number = "H-001-Updated", Address = "Updatedstraße 1" };
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns(existing);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Put(hydrantId, hydrant);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.UpdateHydrant(It.Is<Hydrant>(h => h.Id == hydrantId)), Times.Once);
        }

        /// <summary>
        /// Tests that Put returns NotFound when the hydrant does not exist.
        /// Input: ID of a non-existent hydrant.
        /// Expected: NotFoundResult.
        /// </summary>
        [Fact]
        public void Put_NonExistingHydrant_ReturnsNotFound()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var hydrant = new Hydrant { Number = "H-999" };
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns((Hydrant?)null);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Put(hydrantId, hydrant);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        /// <summary>
        /// Tests that Delete returns NoContent and calls DeleteHydrant when the hydrant exists.
        /// Input: Valid hydrant ID.
        /// Expected: NoContentResult and DeleteHydrant called once.
        /// </summary>
        [Fact]
        public void Delete_ExistingHydrant_ReturnsNoContent()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var existing = new Hydrant { Id = hydrantId, Number = "H-001" };
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns(existing);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Delete(hydrantId);
            // Assert
            Assert.IsType<NoContentResult>(result);
            mockService.Verify(s => s.DeleteHydrant(hydrantId), Times.Once);
        }

        /// <summary>
        /// Tests that Delete returns NotFound when the hydrant does not exist.
        /// Input: ID of a non-existent hydrant.
        /// Expected: NotFoundResult.
        /// </summary>
        [Fact]
        public void Delete_NonExistingHydrant_ReturnsNotFound()
        {
            // Arrange
            var hydrantId = Guid.NewGuid();
            var mockService = new Mock<HydrantService>(Mock.Of<IHydrantRepository>());
            mockService.Setup(s => s.GetById(hydrantId)).Returns((Hydrant?)null);
            var controller = new HydrantController(mockService.Object);

            // Act
            var result = controller.Delete(hydrantId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
