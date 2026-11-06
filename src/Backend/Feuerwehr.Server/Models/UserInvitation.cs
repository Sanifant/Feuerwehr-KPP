namespace De.Openelp.Feuerwehr.Server.Models
{
    // Server-only credential material; never returned by the API.
    /// <summary>
    /// Associates an invitation with its user, email, Identity reset token, and expiry.
    /// </summary>
    /// <param name="UserId">The identifier of the user account.</param>
    /// <param name="Email">The user's email address.</param>
    /// <param name="ResetToken">The Identity password-reset token bound to the invited account.</param>
    /// <param name="ExpiresAtUtc">The UTC expiry time of the invitation or lease.</param>
    public sealed record UserInvitation(string UserId, string Email, string ResetToken, DateTimeOffset ExpiresAtUtc);
}
