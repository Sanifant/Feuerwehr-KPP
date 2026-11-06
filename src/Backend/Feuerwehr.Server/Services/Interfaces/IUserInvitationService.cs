using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IUserInvitationService
    {
        Task SendAsync(ApplicationUser user);
        Task<ServiceResult> CompleteAsync(ResetRequest request);
    }
}
