using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the command idempotency operations required by application consumers.
    /// </summary>
    public interface ICommandIdempotencyService
    {
        /// <summary>
        /// Finds an earlier command execution and computes the payload hash used to validate retries.
        /// </summary>
        /// <param name="commandId">The client-generated command identifier used to detect retries.</param>
        /// <param name="scope">The operation scope used to identify a command retry.</param>
        /// <param name="targetId">The optional target entity identifier included in command identity.</param>
        /// <param name="payload">The command or event data to serialize.</param>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the existing command record, if found, and the hash of the supplied payload.</returns>
        Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the persisted command record containing caller identity, payload hash, and replayable response.
        /// </summary>
        /// <param name="commandId">The client-generated command identifier used to detect retries.</param>
        /// <param name="scope">The operation scope used to identify a command retry.</param>
        /// <param name="targetId">The optional target entity identifier included in command identity.</param>
        /// <param name="payloadHash">The hash of the command payload used to detect inconsistent retries.</param>
        /// <param name="response">The response envelope retained for idempotent command replay.</param>
        /// <param name="userId">The identifier of the user account.</param>
        /// <returns>The command record ready to save with the command's data changes.</returns>
        ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId);
    }
}
