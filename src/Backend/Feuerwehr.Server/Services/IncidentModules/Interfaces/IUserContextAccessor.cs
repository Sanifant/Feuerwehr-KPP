namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the user context accessor operations required by application consumers.
    /// </summary>
    public interface IUserContextAccessor
    {
        /// <summary>
        /// Gets the identifier of the user account.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the name displayed for the user context accessor.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets a value indicating whether the authenticated user has the administrator role.
        /// </summary>
        bool IsAdmin { get; }
    }
}
