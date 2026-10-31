using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Models;
using StackExchange.Redis;

namespace De.Openelp.Feuerwehr.Server.Services;

/// <summary>
/// Executes user Invitation Store.
/// </summary>
/// <param name="redis">The redis.</param>
public sealed class UserInvitationStore(IConnectionMultiplexer redis) : IUserInvitationStore
{
    private static string Key(Guid id) => "user-invitation:" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("D"))));

    /// <summary>
    /// Executes store.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="invitation">The invitation.</param>
    /// <param name="lifetime">The lifetime.</param>
    public async Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime)
    {
        var stored = await redis.GetDatabase().StringSetAsync(Key(id), JsonSerializer.Serialize(invitation), lifetime);
        if (!stored) throw new InvalidOperationException("Einladung konnte nicht gespeichert werden.");
    }

    /// <summary>
    /// Executes find.
    /// </summary>
    /// <param name="id">The id.</param>
    public async Task<UserInvitation?> FindAsync(Guid id)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(id));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<UserInvitation>(value.ToString());
    }

    /// <summary>
    /// Removes the operation.
    /// </summary>
    public async Task RemoveAsync(Guid id) => await redis.GetDatabase().KeyDeleteAsync(Key(id));
}
