using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Detects command replays and builds stored responses using the command payload hash and caller identity.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    public sealed class CommandIdempotencyService(FeuerwehrDbContext dbContext) : ICommandIdempotencyService
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
        public async Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken)
        {
            string payloadJson = JsonSerializer.Serialize(payload, JsonOptionsFactory.Default);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));

            ProcessedCommand? existing = await dbContext.ProcessedCommands
                .FirstOrDefaultAsync(x => x.CommandId == commandId, cancellationToken);

            if (existing is not null)
            {
                if (!string.Equals(existing.UserId, userId, StringComparison.Ordinal) ||
                    !string.Equals(existing.Scope, scope, StringComparison.Ordinal) ||
                    !string.Equals(existing.TargetId ?? string.Empty, targetId ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("CommandId wurde bereits mit anderer Nutzlast oder anderem Benutzer verwendet.");
                }
            }

            return (existing, hash);
        }

        /// <summary>
        /// Builds the stored Command.
        /// </summary>
        /// <param name="commandId">The client-generated command identifier used to detect retries.</param>
        /// <param name="scope">The operation scope used to identify a command retry.</param>
        /// <param name="targetId">The optional target entity identifier included in command identity.</param>
        /// <param name="payloadHash">The hash of the command payload used to detect inconsistent retries.</param>
        /// <param name="response">The response envelope retained for idempotent command replay.</param>
        /// <param name="userId">The identifier of the user account.</param>
        /// <returns>The command record ready to save with the command's data changes.</returns>
        public ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId)
        {
            return new ProcessedCommand
            {
                Id = Guid.NewGuid(),
                CommandId = commandId,
                UserId = userId,
                Scope = scope,
                TargetId = targetId,
                PayloadHash = payloadHash,
                ResponseStatusCode = response.StatusCode,
                ResponseJson = response.JsonPayload,
                CreatedAtUtc = DateTime.UtcNow,
            };
        }
    }
}
