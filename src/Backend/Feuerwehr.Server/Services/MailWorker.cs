namespace de.openelp.feuerwehr.Server.Services
{
    /// <summary>Runs the shared email dispatcher in a fresh scope for each batch.</summary>
    public sealed class MailWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<MailWorker> logger) : BackgroundService
    {
        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IMailDispatcher>().DispatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    // Do not log message bodies, recipient addresses or SMTP error details.
                    logger.LogError("Mail-Queue konnte nicht verarbeitet werden ({Error}).", exception.GetType().Name);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), clock, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
