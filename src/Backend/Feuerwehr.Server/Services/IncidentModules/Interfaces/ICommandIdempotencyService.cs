using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

public interface ICommandIdempotencyService
{
    Task<(ProcessedCommand? Existing, string PayloadHash)> FindExistingAsync(string commandId, string scope, string? targetId, object payload, string userId, CancellationToken cancellationToken);
    ProcessedCommand BuildStoredCommand(string commandId, string scope, string? targetId, string payloadHash, CommandEnvelopeResponse response, string userId);
}
