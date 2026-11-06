using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Models;
using StackExchange.Redis;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Stores expiring password invitations in Redis under their opaque invitation identifiers.
    /// </summary>
    /// <param name="redis">The Redis connection used for expiring invitation storage.</param>
    public sealed class UserInvitationStore(IConnectionMultiplexer redis) : IUserInvitationStore
    {
        /// <summary>
        /// Stores the invitation under its opaque identifier for the requested lifetime.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <param name="invitation">The account, reset token, and expiry associated with the invitation identifier.</param>
        /// <param name="lifetime">The duration for which the invitation remains in the store.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime)
        {
            bool stored = await redis.GetDatabase().StringSetAsync(Key(id), JsonSerializer.Serialize(invitation), lifetime);
            if (!stored)
            {
                throw new InvalidOperationException("Einladung konnte nicht gespeichert werden.");
            }
        }

        /// <summary>
        /// Retrieves the invitation associated with the opaque identifier, if it has not expired.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <returns>A task whose result is the stored invitation, or null if the identifier is unknown or expired.</returns>
        public async Task<UserInvitation?> FindAsync(Guid id)
        {
            RedisValue value = await redis.GetDatabase().StringGetAsync(Key(id));
            return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<UserInvitation>(value.ToString());
        }

        /// <summary>
        /// Removes the invitation associated with the opaque identifier.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task RemoveAsync(Guid id) => await redis.GetDatabase().KeyDeleteAsync(Key(id));

        private static string Key(Guid id) => "user-invitation:" +
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("D"))));
    }
}
