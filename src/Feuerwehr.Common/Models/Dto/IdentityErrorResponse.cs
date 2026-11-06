namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record IdentityErrorResponse(string Message, IReadOnlyCollection<IdentityErrorDto> Errors);
}
