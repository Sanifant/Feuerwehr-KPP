using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for user account operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The users service handling the requested operations.</param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class UsersController(IUsersService service) : ControllerBase
    {

        /// <summary>
        /// Retrieves user profiles and their assigned roles.
        /// </summary>
        /// <returns>A task whose result is an HTTP response containing the user profiles and their roles, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            return this.ToActionResult(await service.GetAllUsers());
        }

        /// <summary>
        /// Retrieves a user's profile and assigned roles by identifier.
        /// </summary>
        /// <param name="id">The identifier of the user account to retrieve.</param>
        /// <returns>A task whose result is an HTTP response containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            return this.ToActionResult(await service.GetUser(id));
        }

        /// <summary>
        /// Creates a user with the requested profile and roles and sends a password invitation.
        /// </summary>
        /// <param name="request">The register user input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the created user profile and invitation outcome, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.CreateUser(request));
        }

        /// <summary>
        /// Issues a new password invitation and invalidates the account's earlier invitation state.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("{id}/resend-invitation")]
        public async Task<IActionResult> ResendInvitation(string id)
        {
            return this.ToActionResult(await service.ResendInvitation(id));
        }

        /// <summary>
        /// Updates the user's profile, department membership, and assigned roles.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <param name="request">The update user input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.UpdateUser(id, request));
        }

        /// <summary>
        /// Deactivates the requested user account while preserving its existing operational records.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            return this.ToActionResult(await service.DeleteUser(id, this.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

        /// <summary>
        /// Permanently removes the requested user account after enforcing self-deletion and ownership restrictions.
        /// </summary>
        /// <param name="id">The identifier of the user account to process.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the user account operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> PermanentlyDeleteUser(string id)
        {
            return this.ToActionResult(await service.PermanentlyDeleteUser(id, this.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }
    }
}
