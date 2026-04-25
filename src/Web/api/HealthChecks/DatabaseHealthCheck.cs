using de.openelp.feuerwehr.infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace de.openelp.feuerwehr.Api.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _dbContext;

        public DatabaseHealthCheck(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : new HealthCheckResult(context.Registration.FailureStatus, "Database is not reachable.");
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, "Database check failed.", ex);
            }
        }
    }
}
