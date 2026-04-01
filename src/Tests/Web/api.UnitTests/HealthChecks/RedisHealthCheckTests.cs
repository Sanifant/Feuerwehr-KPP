using de.openelp.feuerwehr.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using StackExchange.Redis;

namespace de.openelp.feuerwehr.Api.HealthChecks.UnitTests
{
    /// <summary>
    /// Unit tests for the RedisHealthCheck class.
    /// </summary>
    [TestClass]
    public class RedisHealthCheckTests
    {
        private static HealthCheckContext CreateContext(HealthStatus failureStatus = HealthStatus.Unhealthy)
        {
            var registration = new HealthCheckRegistration("redis", Mock.Of<IHealthCheck>(), failureStatus, null);
            return new HealthCheckContext { Registration = registration };
        }

        /// <summary>
        /// Tests that CheckHealthAsync returns Healthy when Redis responds to PING.
        /// Input: IConnectionMultiplexer whose database PingAsync succeeds.
        /// Expected: HealthCheckResult.Status == Healthy.
        /// </summary>
        [TestMethod]
        public async Task CheckHealthAsync_PingSucceeds_ReturnsHealthy()
        {
            // Arrange
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
                  .ReturnsAsync(TimeSpan.FromMilliseconds(1));

            var multiplexerMock = new Mock<IConnectionMultiplexer>();
            multiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                           .Returns(dbMock.Object);

            var check = new RedisHealthCheck(multiplexerMock.Object);

            // Act
            var result = await check.CheckHealthAsync(CreateContext());

            // Assert
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
        }

        /// <summary>
        /// Tests that CheckHealthAsync returns the registration's FailureStatus when PingAsync throws.
        /// Input: IConnectionMultiplexer whose database PingAsync throws a RedisException.
        /// Expected: HealthCheckResult.Status == Unhealthy (the registered failure status).
        /// </summary>
        [TestMethod]
        public async Task CheckHealthAsync_PingThrows_ReturnsFailureStatus()
        {
            // Arrange
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
                  .ThrowsAsync(new RedisException("Connection refused"));

            var multiplexerMock = new Mock<IConnectionMultiplexer>();
            multiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                           .Returns(dbMock.Object);

            var check = new RedisHealthCheck(multiplexerMock.Object);

            // Act
            var result = await check.CheckHealthAsync(CreateContext(HealthStatus.Degraded));

            // Assert
            Assert.AreEqual(HealthStatus.Degraded, result.Status);
            Assert.IsNotNull(result.Exception);
        }
    }
}
