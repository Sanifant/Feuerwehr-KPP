using System.Threading.Tasks;
using de.openelp.feuerwehr.App.Services;
using Xamarin.Essentials;

namespace de.openelp.feuerwehr.App.Android.Services
{
    public class AndroidSecureStorage : ISecureStorage
    {
        /// <summary>
        /// Gets the Vakue from the Android Secure Storage.
        /// </summary>
        /// <param name="key">Name of the Value to be retrieved</param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public async Task<string?> GetAsync(string key)
        {
            return await SecureStorage.GetAsync(key);
        }

        /// <summary>
        /// Removes the given <paramref name="key"/> from the Android Secure Storage.
        /// </summary>
        /// <param name="key">Name of the parameter to be deleted</param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public async Task RemoveAsync(string key)
        {
            _ = SecureStorage.Remove(key);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sets the <paramref name="value"/> by using the <paramref name="key"/> in the Android Secure Storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }
    }
}
