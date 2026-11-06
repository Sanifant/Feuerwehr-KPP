using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Server.Models;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IUserInvitationService
    {
        Task SendAsync(ApplicationUser user);
        Task<ServiceResult> CompleteAsync(ResetRequest request);
    }
}
