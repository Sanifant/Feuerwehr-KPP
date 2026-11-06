using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Simple in-memory secure storage implementation for desktop/development.
    /// NOTE: For production mobile apps, use platform-specific secure storage:
    /// - Android: KeyStore
    /// - iOS: Keychain
    /// - Windows: Data Protection API (DPAPI)
    /// </summary>
    public class InMemorySecureStorage : ISecureStorage
    {
        private readonly ConcurrentDictionary<string, string> _storage = new ();

        /// <summary>
        /// Sets the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public Task SetAsync(string key, string value)
        {
            this._storage[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public Task<string?> GetAsync(string key)
        {
            _ = this._storage.TryGetValue(key, out string? value);
            return Task.FromResult(value);
        }

        /// <summary>
        /// Removes the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public Task RemoveAsync(string key)
        {
            _ = this._storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
