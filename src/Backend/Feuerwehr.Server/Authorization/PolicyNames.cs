namespace De.Openelp.Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Constants for authorization policy names.
    /// </summary>
    public static class PolicyNames
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireCommander = "RequireCommander";
        public const string CanManageHydrants = "CanManageHydrants";
        public const string CanManageTraining = "CanManageTraining";
        public const string CanViewData = "CanViewData";
        public const string CanManageUsers = "RequireAdmin";
        public const string CanViewSituationMap = "CanViewSituationMap";
        public const string CanEditSituationMap = "CanEditSituationMap";
        public const string CanViewIncidentDiary = "CanViewIncidentDiary";
        public const string CanEditIncidentDiary = "CanEditIncidentDiary";
        public const string CanAccessIncidentModule = "CanAccessIncidentModule";
        public const string CanViewMapSymbols = "CanViewMapSymbols";
        public const string CanViewInventory = "CanViewInventory";
        public const string CanManageInventory = "CanManageInventory";
    }
}
