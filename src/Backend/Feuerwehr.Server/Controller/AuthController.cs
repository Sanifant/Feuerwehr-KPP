using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for authentication operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The auth service handling the requested operations.</param>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService service) : ControllerBase
    {

        /// <summary>
        /// Authenticates the supplied email and password and establishes a session subject to account activation and lockout checks.
        /// </summary>
        /// <param name="request">The login input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the access and refresh credentials and user profile, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.Login(request));
        }

        /// <summary>
        /// Renews the authenticated session after validating the refresh token and account state.
        /// </summary>
        /// <param name="request">The refresh token input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the renewed session credentials, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.RefreshToken(request, this.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty)));
        }

        /// <summary>
        /// Assigns a password after validating the invitation identifier and matching account email.
        /// </summary>
        /// <param name="request">The reset input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.ResetPassword(request));
        }

        /// <summary>
        /// Verifies the current password with lockout tracking and changes the authenticated user's password.
        /// </summary>
        /// <param name="request">The change password input to validate and apply.</param>
        /// <returns>A task whose result is an HTTP response containing the password assignment or change outcome, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.ChangePassword(
                request,
                this.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

        /// <summary>
        /// Revokes the current session's refresh token and signs the user out.
        /// </summary>
        /// <returns>A task whose result is an HTTP response containing the outcome of the authentication operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            return this.ToActionResult(await service.Logout(this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

        /// <summary>
        /// Retrieves the authenticated user's profile and assigned roles.
        /// </summary>
        /// <returns>A task whose result is an HTTP response containing the user profile and assigned roles, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            return this.ToActionResult(await service.GetCurrentUser(this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }
    }
}
