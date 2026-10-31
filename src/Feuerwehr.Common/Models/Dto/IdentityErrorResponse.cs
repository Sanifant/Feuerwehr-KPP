using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record IdentityErrorResponse(string Message, IReadOnlyCollection<IdentityErrorDto> Errors);
}
