namespace Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Constants for authorization policy names
    /// </summary>
    public static class PolicyNames
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireCommander = "RequireCommander";
        public const string CanManageHydrants = "CanManageHydrants";
        public const string CanManageTraining = "CanManageTraining";
        public const string CanViewData = "CanViewData";
        public const string CanManageUsers = "RequireAdmin"; // Alias for RequireAdmin
    }

    /// <summary>
    /// Role name constants for the fire department system
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Commander = "Commander";
        public const string Firefighter = "Firefighter";
        public const string Viewer = "Viewer";

        public static readonly string[] AllRoles = { Admin, Commander, Firefighter, Viewer };
        public static readonly string[] ManagementRoles = { Admin, Commander };
        public static readonly string[] DataAccessRoles = { Admin, Commander, Firefighter, Viewer };
    }

    /// <summary>
    /// Extension methods for configuring authorization policies
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        /// <summary>
        /// Configure all authorization policies for the Feuerwehr system
        /// </summary>
        public static void AddFeuerwehrPolicies(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
        {
            // Admin-only policy
            options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                policy.RequireRole(Roles.Admin));

            // Commander and Admin policy
            options.AddPolicy(PolicyNames.RequireCommander, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            // Policy for managing hydrants (Admin, Commander)
            options.AddPolicy(PolicyNames.CanManageHydrants, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            // Policy for managing training (Admin, Commander)
            options.AddPolicy(PolicyNames.CanManageTraining, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            // Policy for viewing data (all roles)
            options.AddPolicy(PolicyNames.CanViewData, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander, Roles.Firefighter, Roles.Viewer));
        }
    }
}
