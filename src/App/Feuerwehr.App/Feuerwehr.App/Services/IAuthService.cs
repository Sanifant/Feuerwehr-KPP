using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Auth;

namespace de.openelp.feuerwehr.App.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task<string?> GetAccessTokenAsync();
    bool IsAuthenticated { get; }
    string? UserEmail { get; }
    string? UserFullName { get; }
    IReadOnlyList<string> UserRoles { get; }
    event EventHandler? AuthStateChanged;
}
