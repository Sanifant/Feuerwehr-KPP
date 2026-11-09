using System.Threading.Tasks;
using Android.App;
using Android.Content;
using de.openelp.feuerwehr.App.Services;

namespace de.openelp.feuerwehr.App.Android.Services
{
    public class AndroidSecureStorage : ISecureStorage
    {
        private const string StorageName = "feuerwehr_secure_storage";

        private static ISharedPreferences Storage => Application.Context.GetSharedPreferences(StorageName, FileCreationMode.Private)!;

        /// <summary>
        /// Gets the Vakue from the Android Secure Storage.
        /// </summary>
        /// <param name="key">Name of the Value to be retrieved</param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public Task<string?> GetAsync(string key)
        {
            return Task.FromResult(Storage.GetString(key, null));
        }

        /// <summary>
        /// Removes the given <paramref name="key"/> from the Android Secure Storage.
        /// </summary>
        /// <param name="key">Name of the parameter to be deleted</param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public Task RemoveAsync(string key)
        {
            using ISharedPreferencesEditor editor = Storage.Edit();
            _ = editor.Remove(key);
            _ = editor.Commit();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the <paramref name="value"/> by using the <paramref name="key"/> in the Android Secure Storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>A <see cref="Task"/> representuing the return value</returns>
        public Task SetAsync(string key, string value)
        {
            using ISharedPreferencesEditor editor = Storage.Edit();
            _ = editor.PutString(key, value);
            _ = editor.Commit();
            return Task.CompletedTask;
        }
    }
}
