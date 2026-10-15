namespace de.openelp.feuerwehr.Server.Models;

// Server-only credential material; never returned by the API.
public sealed record UserInvitation(string UserId, string Email, string ResetToken, DateTimeOffset ExpiresAtUtc);
