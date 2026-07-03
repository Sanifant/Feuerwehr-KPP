using System.Threading.Tasks;

namespace Feuerwehr.App.Services;

/// <summary>
/// Platform-specific secure storage for sensitive data like tokens.
/// </summary>
public interface ISecureStorage
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
