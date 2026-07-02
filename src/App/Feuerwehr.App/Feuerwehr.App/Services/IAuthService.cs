using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Feuerwehr.Common.Models.Auth;

namespace Feuerwehr.App.Services;

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
