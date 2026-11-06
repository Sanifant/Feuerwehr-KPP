using System.Security.Claims;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Executes http User Context Accessor.
    /// </summary>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    public sealed class HttpUserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContextAccessor
    {
        public string UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public string DisplayName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "Unbekannt";

        public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(Authorization.Roles.Admin) == true;
    }
}
