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
    TimeProvider clock) : IUserInvitationService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task SendAsync(ApplicationUser user)
    {
        var configuredUrl = configuration["Frontend:BaseUrl"];
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment))
            throw new InvalidOperationException("Frontend:BaseUrl muss als absolute HTTP(S)-Adresse konfiguriert sein.");

        var id = Guid.NewGuid();
        var resetToken = await users.GeneratePasswordResetTokenAsync(user);
        var invitation = new UserInvitation(user.Id, user.Email!, resetToken, clock.GetUtcNow().Add(Lifetime));
        var link = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), $"password/{id:D}");
        await store.StoreAsync(id, invitation, Lifetime);
        try
        {
            await mail.SendAsync(user.Email!, "Ihr Feuerwehr-Benutzerkonto – Passwort festlegen",
                $"Hallo {user.FirstName},\n\nIhr Benutzerkonto wurde eingerichtet.\n" +
                $"Öffnen Sie diesen Link und geben Sie Ihre E-Mail-Adresse ein, um Ihr Passwort festzulegen:\n\n{link.AbsoluteUri}\n\n" +
                "Der Link ist 24 Stunden gültig und kann einmal verwendet werden.");
        }
        catch
        {
            await store.RemoveAsync(id);
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
        var result = await users.ResetPasswordAsync(user, invitation.ResetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "InvalidToken" or "ConcurrencyFailure"))
                return InvalidInvitation();
            return ServiceResult.BadRequest(new IdentityErrorResponse("Das Passwort konnte nicht gesetzt werden.",
                result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        await store.RemoveAsync(id);
        await tokens.RevokeRefreshTokenAsync(user.Id);
        return ServiceResult.Ok(new MessageResponse("Ihr Passwort wurde gespeichert. Sie können sich jetzt anmelden."));
    }
}
