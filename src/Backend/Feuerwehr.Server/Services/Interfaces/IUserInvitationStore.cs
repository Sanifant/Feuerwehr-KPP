using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Models;
using StackExchange.Redis;

namespace De.Openelp.Feuerwehr.Server.Services;

public interface IUserInvitationStore
{
    Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime);
    Task<UserInvitation?> FindAsync(Guid id);
    Task RemoveAsync(Guid id);
}
