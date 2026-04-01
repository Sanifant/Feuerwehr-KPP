using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace de.openelp.feuerwehr.Api.HealthChecks
{
    public static class StatusResponseWriter
    {
        public const string DatabaseHealthCheckName = "database";
        public const string RedisHealthCheckName = "redis";

        public static Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var dbStatus = report.Entries.TryGetValue(DatabaseHealthCheckName, out var dbEntry)
                ? dbEntry.Status
                : report.Status;

            var redisStatus = report.Entries.TryGetValue(RedisHealthCheckName, out var redisEntry)
                ? redisEntry.Status
                : report.Status;

            var response = new StatusResponse
            {
                Status = report.Status.ToString(),
                Database = dbStatus.ToString(),
                Redis = redisStatus.ToString()
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted);
        }
    }
}
