using de.openelp.feuerwehr.Server.Models.IncidentModules;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface ICommandIdempotencyService
    {
        Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken);
        ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId);
    }
}
