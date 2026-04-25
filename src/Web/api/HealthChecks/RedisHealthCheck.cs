using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace de.openelp.feuerwehr.Api.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var database = _connectionMultiplexer.GetDatabase();
                await database.PingAsync();
                return HealthCheckResult.Healthy();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, "Redis check failed.", ex);
            }
        }
    }
}
