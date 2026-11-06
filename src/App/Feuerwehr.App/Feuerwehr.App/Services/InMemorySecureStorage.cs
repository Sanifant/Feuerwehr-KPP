using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Simple in-memory secure storage implementation for desktop/development. NOTE: For production mobile apps, use platform-specific secure storage: - Android: KeyStore - iOS: Keychain - Windows: Data Protection API (DPAPI).
    /// </summary>
    public class InMemorySecureStorage : ISecureStorage
    {
        private readonly ConcurrentDictionary<string, string> _storage = new ();

        /// <summary>
        /// Stores a value under the supplied key using the platform's credential storage.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <param name="value">The string value to process.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public Task SetAsync(string key, string value)
        {
            this._storage[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves a value from the platform's credential storage by key.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <returns>A task whose result is the value stored under the key, or null when the key is absent.</returns>
        public Task<string?> GetAsync(string key)
        {
            _ = this._storage.TryGetValue(key, out string? value);
            return Task.FromResult(value);
        }

        /// <summary>
        /// Removes the value associated with the supplied secure-storage key.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public Task RemoveAsync(string key)
        {
            _ = this._storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
