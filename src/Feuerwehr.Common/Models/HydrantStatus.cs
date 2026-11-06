namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Defines the supported hydrant status values.
    /// </summary>
    public enum HydrantStatus
    {
        /// <summary>
        /// The hydrant is available for operational use.
        /// </summary>
        Operational = 1,                 // Betriebsbereit
        /// <summary>
        /// The item has a reported defect.
        /// </summary>
        Defective = 2,                   // Defekt
        /// <summary>
        /// Access to the hydrant is blocked.
        /// </summary>
        Blocked = 3,                     // Gesperrt (e.g., due to construction)
        /// <summary>
        /// The hydrant is undergoing maintenance.
        /// </summary>
        UnderMaintenance = 4             // In Wartung
    }
}
