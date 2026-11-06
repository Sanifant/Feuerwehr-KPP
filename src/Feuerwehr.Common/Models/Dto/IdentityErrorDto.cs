namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries identity error data across the API boundary.
    /// </summary>
    /// <param name="Code">The HTTP status code of the application outcome.</param>
    /// <param name="Description">The descriptive text for the identity error.</param>
    public record IdentityErrorDto(string Code, string Description);
}
