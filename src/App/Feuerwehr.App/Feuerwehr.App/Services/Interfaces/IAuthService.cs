using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.App.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string email, string password);

        Task<bool> LoginAsync(string email, string password, bool rememberMe);

        Task LogoutAsync();
        Task<bool> RefreshTokenAsync();
        Task<string?> GetAccessTokenAsync();
        bool IsAuthenticated { get; }
        string? UserEmail { get; }
        string? UserFullName { get; }
        IReadOnlyList<string> UserRoles { get; }
        event EventHandler? AuthStateChanged;
    }
}
