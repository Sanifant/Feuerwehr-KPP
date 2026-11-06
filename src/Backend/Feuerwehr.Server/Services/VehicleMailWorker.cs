namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Executes vehicle Mail Worker.
    /// </summary>
    /// <param name="scopes">The scopes.</param>
    /// <param name="logger">The logger.</param>
    public sealed class VehicleMailWorker(IServiceScopeFactory scopes, ILogger<VehicleMailWorker> logger) : BackgroundService
    {
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
