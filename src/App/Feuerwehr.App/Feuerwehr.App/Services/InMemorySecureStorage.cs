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
        private readonly ConcurrentDictionary<string, string> _storage = new();

        /// <summary>
        /// Sets the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        public Task<string?> GetAsync(string key)
        {
            _storage.TryGetValue(key, out string? value);
            return Task.FromResult(value);
        }

        /// <summary>
        /// Removes the operation.
        /// </summary>
        /// <param name="key">The key.</param>
        public Task RemoveAsync(string key)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }

}
