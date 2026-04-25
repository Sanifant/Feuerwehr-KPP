using de.openelp.feuerwehr.Api;
using de.openelp.feuerwehr.Api.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace de.openelp.feuerwehr.Api.HealthChecks.UnitTests
{
    /// <summary>
    /// Unit tests for the StatusResponseWriter class.
    /// </summary>
    [TestClass]
    public class StatusResponseWriterTests
    {
        /// <summary>
        /// Tests that WriteAsync writes a JSON body with Status, Database and Redis set to "Healthy"
        /// when the health report is fully healthy.
        /// Input: HealthReport with Healthy database and redis entries.
        /// Expected: JSON body with Status="Healthy", Database="Healthy" and Redis="Healthy".
        /// </summary>
        [TestMethod]
        public async Task WriteAsync_HealthyReport_WritesHealthyJson()
        {
            // Arrange
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { StatusResponseWriter.DatabaseHealthCheckName, new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.Zero, null, null) },
                { StatusResponseWriter.RedisHealthCheckName, new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.Zero, null, null) }
            };
            var report = new HealthReport(entries, HealthStatus.Healthy, TimeSpan.Zero);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await StatusResponseWriter.WriteAsync(context, report);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<StatusResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.IsNotNull(response);
            Assert.AreEqual("Healthy", response.Status);
            Assert.AreEqual("Healthy", response.Database);
            Assert.AreEqual("Healthy", response.Redis);
        }

        /// <summary>
        /// Tests that WriteAsync writes a JSON body with Status and Database set to "Unhealthy"
        /// when the health report contains an unhealthy database entry.
        /// Input: HealthReport with Unhealthy database entry and Healthy redis entry.
        /// Expected: JSON body with Status="Unhealthy", Database="Unhealthy" and Redis="Healthy".
        /// </summary>
        [TestMethod]
        public async Task WriteAsync_UnhealthyDatabaseReport_WritesUnhealthyDatabaseJson()
        {
            // Arrange
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { StatusResponseWriter.DatabaseHealthCheckName, new HealthReportEntry(HealthStatus.Unhealthy, "DB unreachable", TimeSpan.Zero, null, null) },
                { StatusResponseWriter.RedisHealthCheckName, new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.Zero, null, null) }
            };
            var report = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.Zero);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await StatusResponseWriter.WriteAsync(context, report);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<StatusResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.IsNotNull(response);
            Assert.AreEqual("Unhealthy", response.Status);
            Assert.AreEqual("Unhealthy", response.Database);
            Assert.AreEqual("Healthy", response.Redis);
        }

        /// <summary>
        /// Tests that WriteAsync writes a JSON body with Redis set to "Degraded"
        /// when Redis is unavailable but the database is healthy.
        /// Input: HealthReport with Healthy database entry and Degraded redis entry.
        /// Expected: JSON body with Status="Degraded", Database="Healthy" and Redis="Degraded".
        /// </summary>
        [TestMethod]
        public async Task WriteAsync_DegradedRedisReport_WritesDegradedRedisJson()
        {
            // Arrange
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { StatusResponseWriter.DatabaseHealthCheckName, new HealthReportEntry(HealthStatus.Healthy, null, TimeSpan.Zero, null, null) },
                { StatusResponseWriter.RedisHealthCheckName, new HealthReportEntry(HealthStatus.Degraded, "Redis unreachable", TimeSpan.Zero, null, null) }
            };
            var report = new HealthReport(entries, HealthStatus.Degraded, TimeSpan.Zero);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await StatusResponseWriter.WriteAsync(context, report);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<StatusResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.IsNotNull(response);
            Assert.AreEqual("Degraded", response.Status);
            Assert.AreEqual("Healthy", response.Database);
            Assert.AreEqual("Degraded", response.Redis);
        }

        /// <summary>
        /// Tests that WriteAsync sets the Content-Type header to "application/json".
        /// Input: Any HealthReport.
        /// Expected: Response Content-Type is "application/json".
        /// </summary>
        [TestMethod]
        public async Task WriteAsync_AnyReport_SetsJsonContentType()
        {
            // Arrange
            var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), HealthStatus.Healthy, TimeSpan.Zero);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await StatusResponseWriter.WriteAsync(context, report);

            // Assert
            Assert.AreEqual("application/json", context.Response.ContentType);
        }
    }
}
