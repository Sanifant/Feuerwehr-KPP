namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of identity error operations.
    /// </summary>
    /// <param name="Message">The human-readable outcome or error message.</param>
    /// <param name="Errors">The validation errors found while importing the file.</param>
    public record IdentityErrorResponse(string Message, IReadOnlyCollection<IdentityErrorDto> Errors);
}
