using Feuerwehr.Server.Models;
using Feuerwehr.Server.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace Feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly SmtpClient _smtpClient;

        public UsersController(UserManager<ApplicationUser> userManager, 
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
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.FullName,
                    u.FireDepartmentId,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync();

            // Get roles for each user
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(appUser!);

                usersWithRoles.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.FullName,
                    user.FireDepartmentId,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLoginAt,
                    Roles = roles
                });
            }

            return Ok(usersWithRoles);
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.FireDepartmentId,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                Roles = roles
            });
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            // Validate roles exist
            foreach (var role in request.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return BadRequest(new { message = $"Role '{role}' does not exist" });
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
                return BadRequest(new { message = "Failed to create user", errors = result.Errors });
            }

            // Assign roles
            if (request.Roles.Any())
            {
                var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    // Rollback user creation if role assignment fails
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new { message = "Failed to assign roles", errors = roleResult.Errors });
                }
            }

            var roles = await _userManager.GetRolesAsync(user);

            using var message = new MailMessage("noreply@localhost.de", user.Email, "Welcome to the system", $"Your account {user.Email} has been created.");
            await _smtpClient.SendMailAsync(message);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.FireDepartmentId,
                user.IsActive,
                Roles = roles
            });
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update properties
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.FireDepartmentId = request.FireDepartmentId ?? user.FireDepartmentId;
            user.IsActive = request.IsActive ?? user.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });
            }

            // Update roles if provided
            if (request.Roles != null && request.Roles.Any())
            {
                // Validate roles
                foreach (var role in request.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        return BadRequest(new { message = $"Role '{role}' does not exist" });
                    }
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, request.Roles);
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update password", errors = passwordResult.Errors });
                }
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.FireDepartmentId,
                user.IsActive,
                Roles = roles
            });
        }

        /// <summary>
        /// Delete user (Admin only) - Soft delete by setting IsActive = false
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent self-deletion
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            // Soft delete
            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to delete user", errors = result.Errors });
            }

            return Ok(new { message = "User deleted successfully" });
        }

        /// <summary>
        /// Hard delete user (Admin only) - Permanently remove from database
        /// </summary>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> PermanentlyDeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent self-deletion
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to permanently delete user", errors = result.Errors });
            }

            return Ok(new { message = "User permanently deleted" });
        }
    }
}

public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? FireDepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? Roles { get; set; }
        public string? NewPassword { get; set; }
    }
