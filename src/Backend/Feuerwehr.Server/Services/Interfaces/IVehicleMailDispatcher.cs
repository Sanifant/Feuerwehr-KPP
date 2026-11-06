namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the vehicle mail operations required by application consumers.
    /// </summary>
    public interface IVehicleMailDispatcher
    {
        /// <summary>
        /// Delivers pending vehicle reservation emails and records successful delivery or a scheduled retry.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task Dispatch(CancellationToken ct);
    }
}
