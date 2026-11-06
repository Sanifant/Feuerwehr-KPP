namespace De.Openelp.Feuerwehr.Server.Authorization
{
    /// <summary>
    /// Constants for authorization policy names.
    /// </summary>
    public static class PolicyNames
    {
        /// <summary>
        /// The authorization policy name used to admin.
        /// </summary>
        public const string RequireAdmin = "RequireAdmin";
        /// <summary>
        /// The authorization policy name used to commander.
        /// </summary>
        public const string RequireCommander = "RequireCommander";
        /// <summary>
        /// The authorization policy name used to manage hydrants.
        /// </summary>
        public const string CanManageHydrants = "CanManageHydrants";
        /// <summary>
        /// The authorization policy name used to manage training.
        /// </summary>
        public const string CanManageTraining = "CanManageTraining";
        /// <summary>
        /// The authorization policy name used to view data.
        /// </summary>
        public const string CanViewData = "CanViewData";
        /// <summary>
        /// The authorization policy name used to manage users.
        /// </summary>
        public const string CanManageUsers = "RequireAdmin";
        /// <summary>
        /// The authorization policy name used to view situation map.
        /// </summary>
        public const string CanViewSituationMap = "CanViewSituationMap";
        /// <summary>
        /// The authorization policy name used to edit situation map.
        /// </summary>
        public const string CanEditSituationMap = "CanEditSituationMap";
        /// <summary>
        /// The authorization policy name used to view incident diary.
        /// </summary>
        public const string CanViewIncidentDiary = "CanViewIncidentDiary";
        /// <summary>
        /// The authorization policy name used to edit incident diary.
        /// </summary>
        public const string CanEditIncidentDiary = "CanEditIncidentDiary";
        /// <summary>
        /// The authorization policy name used to access incident module.
        /// </summary>
        public const string CanAccessIncidentModule = "CanAccessIncidentModule";
        /// <summary>
        /// The authorization policy name used to view map symbols.
        /// </summary>
        public const string CanViewMapSymbols = "CanViewMapSymbols";
        /// <summary>
        /// The authorization policy name used to view inventory.
        /// </summary>
        public const string CanViewInventory = "CanViewInventory";
        /// <summary>
        /// The authorization policy name used to manage inventory.
        /// </summary>
        public const string CanManageInventory = "CanManageInventory";
    }
}
