namespace De.Openelp.Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Extension methods for configuring authorization policies.
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        /// <summary>
        /// Configure all authorization policies for the Feuerwehr system.
        /// </summary>
        /// <param name="options">The options.</param>
        public static void AddFeuerwehrPolicies(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
        {
            options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(PolicyNames.CanViewInventory, policy =>
                policy.RequireRole(Roles.Admin, Roles.EquipmentOfficer, Roles.InventoryViewer));
            options.AddPolicy(PolicyNames.CanManageInventory, policy =>
                policy.RequireRole(Roles.Admin, Roles.EquipmentOfficer));

            options.AddPolicy(PolicyNames.RequireCommander, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            options.AddPolicy(PolicyNames.CanManageHydrants, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            options.AddPolicy(PolicyNames.CanManageTraining, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander));

            options.AddPolicy(PolicyNames.CanViewData, policy =>
                policy.RequireRole(Roles.Admin, Roles.Commander, Roles.Firefighter, Roles.Viewer));

            options.AddPolicy(PolicyNames.CanViewSituationMap, policy =>
                policy.RequireRole(Roles.SituationMapViewer, Roles.SituationMapEditor));

            options.AddPolicy(PolicyNames.CanViewMapSymbols, policy =>
                policy.RequireRole(Roles.Admin, Roles.SituationMapViewer, Roles.SituationMapEditor));

            options.AddPolicy(PolicyNames.CanEditSituationMap, policy =>
                policy.RequireRole(Roles.SituationMapEditor));

            options.AddPolicy(PolicyNames.CanViewIncidentDiary, policy =>
                policy.RequireRole(Roles.IncidentDiaryViewer, Roles.IncidentDiaryEditor));

            options.AddPolicy(PolicyNames.CanEditIncidentDiary, policy =>
                policy.RequireRole(Roles.IncidentDiaryEditor));

            options.AddPolicy(PolicyNames.CanAccessIncidentModule, policy =>
                policy.RequireRole(Roles.SituationMapViewer, Roles.SituationMapEditor, Roles.IncidentDiaryViewer, Roles.IncidentDiaryEditor));
        }
    }
}
