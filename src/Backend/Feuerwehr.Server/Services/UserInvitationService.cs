using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace de.openelp.feuerwehr.Server.Services;

public interface IUserInvitationService
{
    Task SendAsync(ApplicationUser user);
    Task<ServiceResult> CompleteAsync(ResetRequest request);
}

public sealed class UserInvitationService(
    UserManager<ApplicationUser> users,
    IUserInvitationStore store,
    IMailService mail,
    ITokenService tokens,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<UserInvitationService> logger) : IUserInvitationService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(60);

    public async Task SendAsync(ApplicationUser user)
    {
        var configuredUrl = configuration["Frontend:BaseUrl"];
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
            throw new InvalidOperationException("Frontend:BaseUrl muss als absolute HTTP(S)-Adresse konfiguriert sein.");

        var previousStamp = user.SecurityStamp;
        var previouslyRequired = user.PasswordResetRequired;
        user.PasswordResetRequired = true;
        // Persist the lock and invalidate previous invitations and access tokens before sending.
        var locked = await users.UpdateSecurityStampAsync(user);
        if (!locked.Succeeded)
            throw new InvalidOperationException("Das Konto konnte nicht für die Passwortvergabe gesperrt werden.");

        var id = Guid.NewGuid();
        var link = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), $"password/{id:D}");
        try
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(user);
            var invitation = new UserInvitation(user.Id, user.Email!, resetToken, clock.GetUtcNow().Add(Lifetime));
            await store.StoreAsync(id, invitation, Lifetime);
            await tokens.RevokeRefreshTokenAsync(user.Id);
            await mail.SendAsync(user.Email!, "Ihr Feuerwehr-Benutzerkonto – Passwort festlegen",
                $"Hallo {user.FirstName},\n\nSie wurden zur Passwortvergabe für Ihr Benutzerkonto eingeladen.\n" +
                $"Öffnen Sie diesen Link und geben Sie Ihre E-Mail-Adresse ein, um Ihr Passwort festzulegen:\n\n{link.AbsoluteUri}\n\n" +
                $"Der Link ist {Lifetime.TotalMinutes:0} Minuten gültig und kann einmal verwendet werden.\n" +
                "Ihr Konto ist bis zur erfolgreichen Passwortvergabe gesperrt. Frühere Einladungslinks sind ungültig.");
        }
        catch
        {
            // Restore the previous invitation and account state on delivery failure. Identity's
            // concurrency stamp prevents overwriting a reset/resend made by another request.
            user.PasswordResetRequired = previouslyRequired;
            user.SecurityStamp = previousStamp;
            var restored = await users.UpdateAsync(user);
            if (!restored.Succeeded)
                logger.LogError("Kontostatus konnte nach fehlgeschlagenem Einladungsversand nicht wiederhergestellt werden.");
            try
            {
                await store.RemoveAsync(id);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Fehlgeschlagene Einladung konnte nicht aus dem Cache gelöscht werden; sie läuft über ihre TTL ab.");
            }
            throw;
        }
    }

    public async Task<ServiceResult> CompleteAsync(ResetRequest request)
    {
        static ServiceResult InvalidInvitation() => ServiceResult.BadRequest(
            new MessageResponse("Der Einladungslink oder die E-Mail-Adresse ist ungültig oder abgelaufen."));

        if (!Guid.TryParseExact(request.Token, "D", out var id) || id == Guid.Empty)
            return InvalidInvitation();
        var invitation = await store.FindAsync(id);
        if (invitation is null || invitation.ExpiresAtUtc <= clock.GetUtcNow())
            return InvalidInvitation();

        var user = await users.FindByIdAsync(invitation.UserId);
        var email = users.NormalizeEmail(request.Email.Trim());
        if (user is null || !user.IsActive ||
            !string.Equals(email, users.NormalizeEmail(invitation.Email), StringComparison.Ordinal) ||
            !string.Equals(email, users.NormalizeEmail(user.Email), StringComparison.Ordinal))
            return InvalidInvitation();

        // Identity validates password strength and the security-stamp-bound token. A successful
        // reset changes the stamp, so even concurrent/replayed requests cannot reuse the token.
        // ResetPasswordAsync saves the password and invitation lock in the same Identity update.
        var previouslyRequired = user.PasswordResetRequired;
        var previousLockout = user.LockoutEnd;
        var previousFailures = user.AccessFailedCount;
        user.PasswordResetRequired = false;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        var result = await users.ResetPasswordAsync(user, invitation.ResetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            user.PasswordResetRequired = previouslyRequired;
            user.LockoutEnd = previousLockout;
            user.AccessFailedCount = previousFailures;
            if (result.Errors.Any(error => error.Code is "InvalidToken" or "ConcurrencyFailure"))
                return InvalidInvitation();
            return ServiceResult.BadRequest(new IdentityErrorResponse("Das Passwort konnte nicht gesetzt werden.",
                result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        await tokens.RevokeRefreshTokenAsync(user.Id);
        try
        {
            await store.RemoveAsync(id);
        }
        catch (Exception exception)
        {
            // The password is already saved and the Identity token invalidated. Cache
            // cleanup must not turn this into a failed reset; the original TTL still applies.
            logger.LogWarning(exception, "Einladung konnte nach erfolgreicher Passwortvergabe nicht aus dem Cache gelöscht werden; sie läuft über ihre TTL ab.");
        }
        return ServiceResult.Ok(new MessageResponse("Ihr Passwort wurde gespeichert. Sie können sich jetzt anmelden."));
    }
}
