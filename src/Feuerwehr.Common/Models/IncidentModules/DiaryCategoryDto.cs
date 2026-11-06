namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries diary category data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the diary category.</param>
    /// <param name="Code">The HTTP status code of the application outcome.</param>
    /// <param name="Name">The display name of the diary category.</param>
    /// <param name="IsActive">The diary category is active.</param>
    public record DiaryCategoryDto(Guid Id, string Code, string Name, bool IsActive);
}
