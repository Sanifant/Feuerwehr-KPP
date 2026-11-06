using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Models;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IUserInvitationService
    {
        Task SendAsync(ApplicationUser user);

        Task<ServiceResult> CompleteAsync(ResetRequest request);
    }
}
