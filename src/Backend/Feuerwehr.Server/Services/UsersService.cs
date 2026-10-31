using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public sealed class UsersService : IUsersService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserInvitationService _invitations;
        private readonly ILogger<UsersService> _logger;

        public UsersService(UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager,
                IUserInvitationService invitations, ILogger<UsersService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _invitations = invitations;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>

        public async Task<ServiceResult> GetAllUsers()
        {
            List<ApplicationUser> users = await _userManager.Users
                .ToListAsync();

            // Get roles for each user
            List<UserDetailsDto> usersWithRoles = new List<UserDetailsDto>();
            foreach (ApplicationUser? user in users)
            {
                ApplicationUser? appUser = await _userManager.FindByIdAsync(user.Id);
                IList<string> roles = await _userManager.GetRolesAsync(appUser!);

                usersWithRoles.Add(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList(), user.PasswordResetRequired));
            }

            return ServiceResult.Ok(usersWithRoles);

        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        /// <param name="id">The id.</param>

        public async Task<ServiceResult> GetUser(string id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList(), user.PasswordResetRequired));

        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        /// <param name="request">The request.</param>

        public async Task<ServiceResult> CreateUser(RegisterUserRequest request)
        {

            // Check if user already exists
            string email = request.Email.Trim();
            ApplicationUser? existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return ServiceResult.BadRequest(new MessageResponse("User with this email already exists"));
            }

            // Validate roles exist
            foreach (string role in request.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
                }
            }

            ApplicationUser user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FireDepartmentId = request.FireDepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to create user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            // Assign roles
            if (request.Roles.Any())
            {
                IdentityResult roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    // Rollback user creation if role assignment fails
                    await _userManager.DeleteAsync(user);
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to assign roles", roleResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            try
            {
                await _invitations.SendAsync(user);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Einladung konnte nicht versendet werden; Benutzeranlage wird zurückgenommen.");
                IdentityResult rollback = await _userManager.DeleteAsync(user);
                if (!rollback.Succeeded)
                    _logger.LogError("Benutzeranlage konnte nach fehlgeschlagener Einladung nicht zurückgenommen werden.");
                return ServiceResult.StatusCode(503, new MessageResponse("Die Einladung konnte nicht versendet werden. Bitte versuchen Sie es erneut."));
            }

            return ServiceResult.CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList(), user.PasswordResetRequired));

        }

        /// <summary>
        /// Executes resend Invitation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<ServiceResult> ResendInvitation(string id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            if (user is null) return ServiceResult.NotFound(new MessageResponse("Benutzer nicht gefunden."));
            if (!user.IsActive)
                return ServiceResult.BadRequest(new MessageResponse("Inaktive Benutzer können nicht eingeladen werden."));

            try
            {
                await _invitations.SendAsync(user);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Einladung konnte nicht erneut versendet werden.");
                return ServiceResult.StatusCode(503, new MessageResponse("Die Einladung konnte nicht versendet werden. Bitte versuchen Sie es erneut."));
            }

            return ServiceResult.Ok(new MessageResponse("Einladung erneut versendet. Das Konto ist bis zur Passwortvergabe gesperrt."));
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="request">The request.</param>

        public async Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request)
        {

            ApplicationUser? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound(new MessageResponse("User not found"));
            }

            if (user.PasswordResetRequired && !string.IsNullOrEmpty(request.NewPassword))
                return ServiceResult.BadRequest(new MessageResponse("Das Passwort muss über den Einladungslink festgelegt werden."));

            if (request.ClearFireDepartment && request.FireDepartmentId.HasValue)
                return ServiceResult.BadRequest(new MessageResponse("Bitte entweder eine Feuerwehr auswählen oder die Zuordnung entfernen."));

            if (request.Email != null)
            {
                string email = request.Email.Trim();
                ApplicationUser? existingUser = await _userManager.FindByEmailAsync(email);
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
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
                    }
                }
            }

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.FireDepartmentId = request.ClearFireDepartment ? null : request.FireDepartmentId ?? user.FireDepartmentId;
            user.IsActive = request.IsActive ?? user.IsActive;

            IdentityResult result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            if (request.Roles != null)
            {
                IList<string> currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                {
                    IdentityResult removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", removeResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                    }
                }

                if (request.Roles.Count > 0)
                {
                    IdentityResult addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                    if (!addResult.Succeeded)
                    {
                        return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", addResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                    }
                }
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update password", passwordResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            return ServiceResult.Ok(new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList(), user.PasswordResetRequired));

        }

        /// <summary>
        /// Delete user (Admin only) - Soft delete by setting IsActive = false
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="currentUserId">The current User Id.</param>

        public async Task<ServiceResult> DeleteUser(string id, string? currentUserId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
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
            IdentityResult result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to delete user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }

            return ServiceResult.Ok(new MessageResponse("User deleted successfully"));

        }

        /// <summary>
        /// Hard delete user (Admin only) - Permanently remove from database
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="currentUserId">The current User Id.</param>

        public async Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);
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
            try { result = await _userManager.DeleteAsync(user); }
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
