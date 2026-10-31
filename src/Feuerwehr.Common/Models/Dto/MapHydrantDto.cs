using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record MapHydrantDto(int Id, double Longitude, double Latitude, HydrantStatus Status, int NominalDiameter);
}
