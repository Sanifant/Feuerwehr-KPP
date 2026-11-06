namespace De.Openelp.Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Role name constants for the fire department system.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// The Identity role name for admin permissions.
        /// </summary>
        public const string Admin = "Admin";
        /// <summary>
        /// The Identity role name for commander permissions.
        /// </summary>
        public const string Commander = "Commander";
        /// <summary>
        /// The Identity role name for firefighter permissions.
        /// </summary>
        public const string Firefighter = "Firefighter";
        /// <summary>
        /// The Identity role name for viewer permissions.
        /// </summary>
        public const string Viewer = "Viewer";
        /// <summary>
        /// The Identity role name for situation map viewer permissions.
        /// </summary>
        public const string SituationMapViewer = "SituationMapViewer";
        /// <summary>
        /// The Identity role name for situation map editor permissions.
        /// </summary>
        public const string SituationMapEditor = "SituationMapEditor";
        /// <summary>
        /// The Identity role name for incident diary viewer permissions.
        /// </summary>
        public const string IncidentDiaryViewer = "IncidentDiaryViewer";
        /// <summary>
        /// The Identity role name for incident diary editor permissions.
        /// </summary>
        public const string IncidentDiaryEditor = "IncidentDiaryEditor";
        /// <summary>
        /// The Identity role name for equipment officer permissions.
        /// </summary>
        public const string EquipmentOfficer = "EquipmentOfficer";
        /// <summary>
        /// The Identity role name for inventory viewer permissions.
        /// </summary>
        public const string InventoryViewer = "InventoryViewer";

        /// <summary>
        /// The Identity role name for all roles permissions.
        /// </summary>
        public static readonly string[] AllRoles = { Admin, Commander, Firefighter, Viewer, SituationMapViewer, SituationMapEditor, IncidentDiaryViewer, IncidentDiaryEditor, EquipmentOfficer, InventoryViewer };
        /// <summary>
        /// The Identity role name for management roles permissions.
        /// </summary>
        public static readonly string[] ManagementRoles = { Admin, Commander };
        /// <summary>
        /// The Identity role name for data access roles permissions.
        /// </summary>
        public static readonly string[] DataAccessRoles = { Admin, Commander, Firefighter, Viewer };
    }
}
