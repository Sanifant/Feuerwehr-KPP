namespace Feuerwehr.Common.Models
{
    public enum HydrantType
    {
        Underground = 1,                 // Unterflurhydrant
        AbovegroundWithoutJack = 2,       // Überflurhydrant ohne Fallmantel
        AbovegroundWithJack = 3,          // Überflurhydrant mit Fallmantel
        WallHydrant = 4                  // Wandhydrant
    }
}
