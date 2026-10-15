using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Common.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

using de.openelp.feuerwehr.Server.Services;

namespace de.openelp.feuerwehr.Server.Services;

public sealed class UsersService : IUsersService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SmtpClient _smtpClient;

    public UsersService(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SmtpClient smtpClient)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _smtpClient = smtpClient;
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>

    public async Task<ServiceResult> GetAllUsers()
    {
        var users = await _userManager.Users
            .ToListAsync();

        // Get roles for each user
        var usersWithRoles = new List<UserDetailsDto>();
        foreach (var user in users)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id);
            var roles = await _userManager.GetRolesAsync(appUser!);

            usersWithRoles.Add(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList()));
        }

        return ServiceResult.Ok(usersWithRoles);

    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>

    public async Task<ServiceResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult.NotFound(new MessageResponse("User not found"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ServiceResult.Ok(new UserDetailsDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList()));

    }

    /// <summary>
    /// Create new user (Admin only)
    /// </summary>

    public async Task<ServiceResult> CreateUser(RegisterUserRequest request)
    {

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ServiceResult.BadRequest(new MessageResponse("User with this email already exists"));
        }

        // Validate roles exist
        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
            }
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FireDepartmentId = request.FireDepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to create user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        // Assign roles
        if (request.Roles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
            {
                // Rollback user creation if role assignment fails
                await _userManager.DeleteAsync(user);
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to assign roles", roleResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);

        using var message = new MailMessage("noreply@localhost.de", user.Email, "Welcome to the system", $"Your account {user.Email} has been created.");
        try
        {
            await _smtpClient.SendMailAsync(message);
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"Failed to send email: {smtpEx.Message}");
        }

        return ServiceResult.CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList()));

    }

    /// <summary>
    /// Update user (Admin only)
    /// </summary>

    public async Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request)
    {

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult.NotFound(new MessageResponse("User not found"));
        }

        if (request.Email != null)
        {
            var email = request.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ServiceResult.BadRequest(new MessageResponse("User with this email already exists"));
            }

            user.Email = email;
            user.UserName = email;
        }

        if (request.Roles != null)
        {
            foreach (var role in request.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return ServiceResult.BadRequest(new MessageResponse($"Role '{role}' does not exist"));
                }
            }
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.FireDepartmentId = request.FireDepartmentId ?? user.FireDepartmentId;
        user.IsActive = request.IsActive ?? user.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        if (request.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", removeResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }

            if (request.Roles.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!addResult.Succeeded)
                {
                    return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update roles", addResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
                }
            }
        }

        // Update password if provided
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to update password", passwordResult.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ServiceResult.Ok(new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.FireDepartmentId, user.IsActive, roles.ToList()));

    }

    /// <summary>
    /// Delete user (Admin only) - Soft delete by setting IsActive = false
    /// </summary>

    public async Task<ServiceResult> DeleteUser(string id, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
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
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to delete user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        return ServiceResult.Ok(new MessageResponse("User deleted successfully"));

    }

    /// <summary>
    /// Hard delete user (Admin only) - Permanently remove from database
    /// </summary>

    public async Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ServiceResult.NotFound(new MessageResponse("User not found"));
        }

        // Prevent self-deletion

        if (currentUserId == id)
        {
            return ServiceResult.BadRequest(new MessageResponse("Cannot delete your own account"));
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return ServiceResult.BadRequest(new IdentityErrorResponse("Failed to permanently delete user", result.Errors.Select(error => new IdentityErrorDto(error.Code, error.Description)).ToList()));
        }

        return ServiceResult.Ok(new MessageResponse("User permanently deleted"));

    }

}

public interface IUsersService
{
    Task<ServiceResult> GetAllUsers();
    Task<ServiceResult> GetUser(string id);
    Task<ServiceResult> CreateUser(RegisterUserRequest request);
    Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request);
    Task<ServiceResult> DeleteUser(string id, string? currentUserId);
    Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId);
}
