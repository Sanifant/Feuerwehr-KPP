using De.Openelp.Feuerwehr.Common.Models.Auth;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IUsersService
    {
        Task<ServiceResult> GetAllUsers();

        Task<ServiceResult> GetUser(string id);

        Task<ServiceResult> CreateUser(RegisterUserRequest request);

        Task<ServiceResult> ResendInvitation(string id);

        Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request);

        Task<ServiceResult> DeleteUser(string id, string? currentUserId);

        Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId);
    }
}
