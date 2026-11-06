using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Implements user account operations for the API application layer.
    /// </summary>
    public sealed class UsersService : IUsersService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserInvitationService _invitations;
        private readonly ILogger<UsersService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersService" /> class with its required dependencies.
        /// </summary>
        /// <param name="userManager">The Identity user manager used to load and update accounts.</param>
        /// <param name="roleManager">The Identity role manager used to validate and create roles.</param>
        /// <param name="invitations">The service that sends password invitations for newly created or re-invited users.</param>
        /// <param name="logger">The logger used to record operational failures and diagnostics.</param>
        public UsersService(
            UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager,
                IUserInvitationService invitations,
            ILogger<UsersService> logger)
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._invitations = invitations;
            this._logger = logger;
        }

        /// <summary>
        /// Retrieves user profiles and their assigned roles.
        /// </summary>
        /// <returns>A task whose result is an application result containing the user profiles and their roles, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetAllUsers()
        {
            List<ApplicationUser> users = await this._userManager.Users
                .ToListAsync();

            // Get roles for each user
            List<UserDetailsDto> usersWithRoles = new ();
            foreach (ApplicationUser? user in users)
            {
                ApplicationUser? appUser = await this._userManager.FindByIdAsync(user.Id);
                IList<string> roles = await this._userManager.GetRolesAsync(appUser!);

                usersWithRoles.Add(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList(), user.PasswordResetRequired));
            }

            return ServiceResult.Ok(usersWithRoles);
        }

        /// <summary>
        /// Retrieves a user's profile and assigned roles by identifier.
        /// </summary>
        /// <param name="id">The identifier of the user account to retrieve.</param>
        /// <returns>A task whose result is an application result containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetUser(string id)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            IList<string> roles = await this._userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList(), user.PasswordResetRequired));
        }

        /// <summary>
        /// Creates a user with the requested profile and roles and sends a password invitation.
        /// </summary>
        /// <param name="request">The register user input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the created user profile and invitation outcome, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> CreateUser(RegisterUserRequest request)
        {

            // Check if user already exists
            string email = request.Email.Trim();
            ApplicationUser? existingUser = await this._userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return ServiceResult.BadRequest(new MessageResponse("User with this email already exists"));
            }

            // Validate roles exist
            foreach (string role in request.Roles)
            {
                if (!await this._roleManager.RoleExistsAsync(role))
                {
                    return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
                }
            }

            ApplicationUser user = new ()
            {
                UserName = email,
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FireDepartmentId = request.FireDepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            IdentityResult result = await this._userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to create user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            // Assign roles
            if (request.Roles.Any())
            {
                IdentityResult roleResult = await this._userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    // Rollback user creation if role assignment fails
                    _ = await this._userManager.DeleteAsync(user);
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to assign roles", roleResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }

            IList<string> roles = await this._userManager.GetRolesAsync(user);

            try
            {
                await this._invitations.SendAsync(user);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "Einladung konnte nicht versendet werden; Benutzeranlage wird zurückgenommen.");
                IdentityResult rollback = await this._userManager.DeleteAsync(user);
                if (!rollback.Succeeded)
                {
                    this._logger.LogError("Benutzeranlage konnte nach fehlgeschlagener Einladung nicht zurückgenommen werden.");
                }

                return ServiceResult.StatusCode(503, new MessageResponse("Die Einladung konnte nicht versendet werden. Bitte versuchen Sie es erneut."));
            }

            return ServiceResult.CreatedAtAction(nameof(this.GetUser), new { id = user.Id }, new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList(), user.PasswordResetRequired));
        }

        /// <summary>
        /// Issues a new password invitation and invalidates the account's earlier invitation state.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> ResendInvitation(string id)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(id);
            if (user is null)
            {
                return ServiceResult.NotFound(new MessageResponse("Benutzer nicht gefunden."));
            }

            if (!user.IsActive)
            {
                return ServiceResult.BadRequest(new MessageResponse("Inaktive Benutzer können nicht eingeladen werden."));
            }

            try
            {
                await this._invitations.SendAsync(user);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "Einladung konnte nicht erneut versendet werden.");
                return ServiceResult.StatusCode(503, new MessageResponse("Die Einladung konnte nicht versendet werden. Bitte versuchen Sie es erneut."));
            }

            return ServiceResult.Ok(new MessageResponse("Einladung erneut versendet. Das Konto ist bis zur Passwortvergabe gesperrt."));
        }

        /// <summary>
        /// Updates the user's profile, department membership, and assigned roles.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="request">The update user input to validate and apply.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request)
        {

            ApplicationUser? user = await this._userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            if (user.PasswordResetRequired && !string.IsNullOrEmpty(request.NewPassword))
            {
                return ServiceResult.BadRequest(new MessageResponse("Das Passwort muss über den Einladungslink festgelegt werden."));
            }

            if (request.ClearFireDepartment && request.FireDepartmentId.HasValue)
            {
                return ServiceResult.BadRequest(new MessageResponse("Bitte entweder eine Feuerwehr auswählen oder die Zuordnung entfernen."));
            }

            if (request.Email != null)
            {
                string email = request.Email.Trim();
                ApplicationUser? existingUser = await this._userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return ServiceResult.BadRequest(new MessageResponse("User with this email already exists"));
                }

                user.Email = email;
                user.UserName = email;
            }

            if (request.Roles != null)
            {
                foreach (string role in request.Roles)
                {
                    if (!await this._roleManager.RoleExistsAsync(role))
                    {
                        return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
                    }
                }
            }

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.FireDepartmentId = request.ClearFireDepartment ? null : request.FireDepartmentId ?? user.FireDepartmentId;
            user.IsActive = request.IsActive ?? user.IsActive;

            IdentityResult result = await this._userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            if (request.Roles != null)
            {
                IList<string> currentRoles = await this._userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                {
                    IdentityResult removeResult = await this._userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", removeResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                    }
                }

                if (request.Roles.Count > 0)
                {
                    IdentityResult addResult = await this._userManager.AddToRolesAsync(user, request.Roles);
                    if (!addResult.Succeeded)
                    {
                        return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", addResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                    }
                }
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                string token = await this._userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult passwordResult = await this._userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update password", passwordResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }

            IList<string> roles = await this._userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList(), user.PasswordResetRequired));
        }

        /// <summary>
        /// Deactivates the requested user account while preserving its existing operational records.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> DeleteUser(string id, string? currentUserId)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            // Prevent self-deletion

            if (currentUserId == id)
            {
                return ServiceResult.BadRequest(new MessageResponse("Cannot delete your own account"));
            }

            // Soft delete
            user.IsActive = false;
            IdentityResult result = await this._userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to delete user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            return ServiceResult.Ok(new MessageResponse("User deleted successfully"));
        }

        /// <summary>
        /// Permanently removes the requested user account after enforcing self-deletion and ownership restrictions.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="currentUserId">The authenticated caller's identifier, used for ownership and self-deletion checks.</param>
        /// <returns>A task whose result is an application result containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId)
        {
            ApplicationUser? user = await this._userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            // Prevent self-deletion

            if (currentUserId == id)
            {
                return ServiceResult.BadRequest(new MessageResponse("Cannot delete your own account"));
            }

            IdentityResult result;
            try
            {
                result = await this._userManager.DeleteAsync(user);
            }
            catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
            { SqlState: Npgsql.PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_Vehicles_AspNetUsers_EquipmentOfficerId" })
            {
                return ServiceResult.StatusCode(409, new MessageResponse("Der Benutzer ist als Gerätewart Fahrzeugen zugeordnet. Bitte die Zuständigkeit vor dem endgültigen Löschen übertragen."));
            }

            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to permanently delete user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            return ServiceResult.Ok(new MessageResponse("User permanently deleted"));
        }
    }
}
