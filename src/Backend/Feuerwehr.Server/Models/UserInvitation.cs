namespace De.Openelp.Feuerwehr.Server.Models
{
    // Server-only credential material; never returned by the API.
    /// <summary>
    /// Executes user Invitation.
    /// </summary>
    /// <param name="UserId">The user Id.</param>
    /// <param name="Email">The email.</param>
    /// <param name="ResetToken">The reset Token.</param>
    /// <param name="ExpiresAtUtc">The expires At Utc.</param>
    public sealed record UserInvitation(string UserId, string Email, string ResetToken, DateTimeOffset ExpiresAtUtc);

}
