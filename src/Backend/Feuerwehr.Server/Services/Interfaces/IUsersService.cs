using De.Openelp.Feuerwehr.Common.Models.Auth;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the user account operations required by application consumers.
    /// </summary>
    public interface IUsersService
    {
        /// <summary>
        /// Retrieves user profiles and their assigned roles.
        /// </summary>
        /// <returns>A task whose result is an application result containing the user profiles and their roles, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetAllUsers();

        /// <summary>
        /// Retrieves a user's profile and assigned roles by identifier.
        /// </summary>
        /// <param name="id">The identifier of the user account to retrieve.</param>
        /// <returns>A task whose result is an application result containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetUser(string id);

        /// <summary>
        /// Creates a user with the requested profile and roles and sends a password invitation.
        /// </summary>
        /// <param name="request">The register user input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the created user profile and invitation outcome, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CreateUser(RegisterUserRequest request);

        /// <summary>
        /// Issues a new password invitation and invalidates the account's earlier invitation state.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ResendInvitation(string id);

        /// <summary>
        /// Updates the user's profile, department membership, and assigned roles.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="request">The update user input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request);

        /// <summary>
        /// Deactivates the requested user account while preserving its existing operational records.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> DeleteUser(string id, string? currentUserId);

        /// <summary>
        /// Permanently removes the requested user account after enforcing self-deletion and ownership restrictions.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId);
    }
}
