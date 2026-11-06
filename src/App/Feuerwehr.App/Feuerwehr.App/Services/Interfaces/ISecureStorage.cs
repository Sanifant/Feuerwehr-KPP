using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Platform-specific secure storage for sensitive data like tokens.
    /// </summary>
    public interface ISecureStorage
    {
        /// <summary>
        /// Stores a value under the supplied key using the platform's credential storage.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <param name="value">The string value to process.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task SetAsync(string key, string value);

        /// <summary>
        /// Retrieves a value from the platform's credential storage by key.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <returns>A task whose result is the value stored under the key, or null when the key is absent.</returns>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Removes the value associated with the supplied secure-storage key.
        /// </summary>
        /// <param name="key">The key identifying the securely stored value.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task RemoveAsync(string key);
    }
}
