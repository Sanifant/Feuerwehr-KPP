using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using de.openelp.feuerwehr.Server.Models;
using StackExchange.Redis;

namespace de.openelp.feuerwehr.Server.Services;

public interface IUserInvitationStore
{
    Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime);
    Task<UserInvitation?> FindAsync(Guid id);
    Task RemoveAsync(Guid id);
}

public sealed class UserInvitationStore(IConnectionMultiplexer redis) : IUserInvitationStore
{
    private static string Key(Guid id) => "user-invitation:" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("D"))));

    public async Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime)
    {
        var stored = await redis.GetDatabase().StringSetAsync(Key(id), JsonSerializer.Serialize(invitation), lifetime);
        if (!stored) throw new InvalidOperationException("Einladung konnte nicht gespeichert werden.");
    }

    public async Task<UserInvitation?> FindAsync(Guid id)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(id));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<UserInvitation>(value.ToString());
    }

    public async Task RemoveAsync(Guid id) => await redis.GetDatabase().KeyDeleteAsync(Key(id));
}
