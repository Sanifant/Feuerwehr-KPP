namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    /// <summary>
    /// Carries the result of lease released operations.
    /// </summary>
    /// <param name="Released">The editing lease was released.</param>
    /// <param name="ServerUtc">The server's current UTC time used to compare lease expiry.</param>
    public record LeaseReleasedResponse(bool Released, DateTime ServerUtc);
}
