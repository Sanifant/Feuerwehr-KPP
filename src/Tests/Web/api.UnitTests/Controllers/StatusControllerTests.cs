using de.openelp.feuerwehr.Api;
using de.openelp.feuerwehr.Api.Controllers;
using de.openelp.feuerwehr.Api.UnitTests.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace de.openelp.feuerwehr.Api.Controllers.UnitTests
{
    /// <summary>
    /// Unit tests for the StatusController class.
    /// </summary>
    [TestClass]
    public class StatusControllerTests
    {
        /// <summary>
        /// Tests that Get returns 200 OK with Healthy status when the database is reachable.
        /// Input: Database health checker returns true.
        /// Expected: 200 OK response with Status and Database set to "Healthy".
        /// </summary>
        [TestMethod]
        public async Task Get_DatabaseHealthy_ReturnsOkWithHealthyStatus()
        {
            // Arrange
            var mockHealthChecker = new DatabaseHealthCheckerMock(canConnect: true);
            var controller = new StatusController(mockHealthChecker);

            // Act
            var result = await controller.Get();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

            var response = okResult.Value as StatusResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual("Healthy", response.Status);
            Assert.AreEqual("Healthy", response.Database);
        }

        /// <summary>
        /// Tests that Get returns 503 Service Unavailable with Unhealthy status when the database is not reachable.
        /// Input: Database health checker returns false.
        /// Expected: 503 response with Status and Database set to "Unhealthy".
        /// </summary>
        [TestMethod]
        public async Task Get_DatabaseUnhealthy_Returns503WithUnhealthyStatus()
        {
            // Arrange
            var mockHealthChecker = new DatabaseHealthCheckerMock(canConnect: false);
            var controller = new StatusController(mockHealthChecker);

            // Act
            var result = await controller.Get();

            // Assert
            var objectResult = result.Result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);

            var response = objectResult.Value as StatusResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual("Unhealthy", response.Status);
            Assert.AreEqual("Unhealthy", response.Database);
        }
    }
}
