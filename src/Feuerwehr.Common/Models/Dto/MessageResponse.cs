namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of message operations.
    /// </summary>
    /// <param name="Message">The human-readable outcome or error message.</param>
    public record MessageResponse(string Message);
}
