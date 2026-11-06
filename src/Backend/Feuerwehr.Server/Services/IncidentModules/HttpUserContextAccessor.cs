using System.Security.Claims;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Reads the authenticated caller's identity, display name, and administrator role from the HTTP context.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for the authenticated HTTP request and caller claims.</param>
    public sealed class HttpUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
    {
        /// <summary>
        /// Gets the identifier of the user account.
        /// </summary>
        public string UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// Gets the name displayed for the HTTP user context accessor.
        /// </summary>
        public string DisplayName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Unbekannt";

        /// <summary>
        /// Gets a value indicating whether the authenticated user has the administrator role.
        /// </summary>
        public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(Authorization.Roles.Admin) == true;
    }
}
