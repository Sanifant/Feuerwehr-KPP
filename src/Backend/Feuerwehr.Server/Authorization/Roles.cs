namespace De.Openelp.Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Role name constants for the fire department system.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Commander = "Commander";
        public const string Firefighter = "Firefighter";
        public const string Viewer = "Viewer";
        public const string SituationMapViewer = "SituationMapViewer";
        public const string SituationMapEditor = "SituationMapEditor";
        public const string IncidentDiaryViewer = "IncidentDiaryViewer";
        public const string IncidentDiaryEditor = "IncidentDiaryEditor";
        public const string EquipmentOfficer = "EquipmentOfficer";
        public const string InventoryViewer = "InventoryViewer";

        public static readonly string[] AllRoles = { Admin, Commander, Firefighter, Viewer, SituationMapViewer, SituationMapEditor, IncidentDiaryViewer, IncidentDiaryEditor, EquipmentOfficer, InventoryViewer };
        public static readonly string[] ManagementRoles = { Admin, Commander };
        public static readonly string[] DataAccessRoles = { Admin, Commander, Firefighter, Viewer };
    }
}
