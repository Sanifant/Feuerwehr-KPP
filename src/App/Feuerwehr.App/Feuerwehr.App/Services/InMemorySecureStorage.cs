using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Feuerwehr.App.Services;

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

    public Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key)
    {
        _storage.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
