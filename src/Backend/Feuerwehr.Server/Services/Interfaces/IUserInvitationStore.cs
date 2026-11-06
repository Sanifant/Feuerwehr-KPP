using De.Openelp.Feuerwehr.Server.Models;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the user invitation operations required by application consumers.
    /// </summary>
    public interface IUserInvitationStore
    {
        /// <summary>
        /// Stores the invitation under its opaque identifier for the requested lifetime.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <param name="invitation">The account, reset token, and expiry associated with the invitation identifier.</param>
        /// <param name="lifetime">The duration for which the invitation remains in the store.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime);

        /// <summary>
        /// Retrieves the invitation associated with the opaque identifier, if it has not expired.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <returns>A task whose result is the stored invitation, or null if the identifier is unknown or expired.</returns>
        Task<UserInvitation?> FindAsync(Guid id);

        /// <summary>
        /// Removes the invitation associated with the opaque identifier.
        /// </summary>
        /// <param name="id">The identifier of the user invitation to process.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        Task RemoveAsync(Guid id);
    }
}
