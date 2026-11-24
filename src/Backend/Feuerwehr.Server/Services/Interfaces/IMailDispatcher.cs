namespace de.openelp.feuerwehr.Server.Services
{
    /// <summary>Dispatches a batch of due emails from the shared queue.</summary>
    public interface IMailDispatcher
    {
        /// <summary>Claims and sends due messages, recording outcomes and retry times.</summary>
        /// <param name="cancellationToken">The worker shutdown token.</param>
        Task DispatchAsync(CancellationToken cancellationToken);
    }
}
