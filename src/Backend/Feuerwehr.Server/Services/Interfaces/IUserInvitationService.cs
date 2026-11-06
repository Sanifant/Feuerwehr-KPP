using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Models;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the user invitation operations required by application consumers.
    /// </summary>
    public interface IUserInvitationService
    {
        /// <summary>
        /// Creates a sixty-minute password invitation, locks the account for assignment, and emails the configured frontend link.
        /// </summary>
        /// <param name="user">The user account involved in the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task SendAsync(ApplicationUser user);

        /// <summary>
        /// Validates the invitation, assigns the account password, and revokes earlier refresh credentials.
        /// </summary>
        /// <param name="request">The reset input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CompleteAsync(ResetRequest request);
    }
}
