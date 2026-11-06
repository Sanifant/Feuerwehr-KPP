namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Runs vehicle mail processing in a background service with scoped dependencies.
    /// </summary>
    /// <param name="scopes">The service scope factory dependency used by this component.</param>
    /// <param name="logger">The logger used to record operational failures and diagnostics.</param>
    public sealed class VehicleMailWorker(IServiceScopeFactory scopes, ILogger<VehicleMailWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Processes pending vehicle mail work until the host requests cancellation.
        /// </summary>
        /// <param name="stoppingToken">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IVehicleMailDispatcher>().Dispatch(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "MTW-Mailversand fehlgeschlagen.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
