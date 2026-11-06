using de.openelp.feuerwehr.Server.Models;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IUserInvitationStore
    {
        Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime);
        Task<UserInvitation?> FindAsync(Guid id);
        Task RemoveAsync(Guid id);
    }
}
