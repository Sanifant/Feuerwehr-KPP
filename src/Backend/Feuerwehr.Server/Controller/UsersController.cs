using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "RequireAdmin")]
public class UsersController(IUsersService service) : ControllerBase
{

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        return this.ToActionResult(await service.GetAllUsers());
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        return this.ToActionResult(await service.GetUser(id));
    }

    /// <summary>
    /// Create new user (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] RegisterUserRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return this.ToActionResult(await service.CreateUser(request));
    }

    [HttpPost("{id}/resend-invitation")]
    public async Task<IActionResult> ResendInvitation(string id)
    {
        return this.ToActionResult(await service.ResendInvitation(id));
    }

    /// <summary>
    /// Update user (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return this.ToActionResult(await service.UpdateUser(id, request));
    }

    /// <summary>
    /// Delete user (Admin only) - Soft delete by setting IsActive = false
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        return this.ToActionResult(await service.DeleteUser(id, User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
    }

    /// <summary>
    /// Hard delete user (Admin only) - Permanently remove from database
    /// </summary>
    [HttpDelete("{id}/permanent")]
    public async Task<IActionResult> PermanentlyDeleteUser(string id)
    {
        return this.ToActionResult(await service.PermanentlyDeleteUser(id, User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
    }

}
