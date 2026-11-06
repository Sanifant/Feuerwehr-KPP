using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Executes command Idempotency Service.
    /// </summary>
    /// <param name="dbContext">The db Context.</param>
    public sealed class CommandIdempotencyService(FeuerwehrDbContext dbContext) : ICommandIdempotencyService
    {
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
        /// <param name="commandId">The command Id.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="targetId">The target Id.</param>
        /// <param name="payloadHash">The payload Hash.</param>
        /// <param name="response">The response.</param>
        /// <param name="userId">The user Id.</param>
        /// <returns></returns>
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
