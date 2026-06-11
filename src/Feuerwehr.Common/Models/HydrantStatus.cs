namespace Feuerwehr.Common.Models
{
    public enum HydrantStatus
    {
        Operational = 1,                 // Betriebsbereit
        Defective = 2,                   // Defekt
        Blocked = 3,                     // Gesperrt (e.g., due to construction)
        UnderMaintenance = 4             // In Wartung
    }
}
