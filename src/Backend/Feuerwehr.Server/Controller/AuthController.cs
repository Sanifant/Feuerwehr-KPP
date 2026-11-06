using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService service) : ControllerBase
    {

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!this.ModelState.IsValid)
                return this.BadRequest(this.ModelState);
            return this.ToActionResult(await service.Login(request));
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!this.ModelState.IsValid)
                return this.BadRequest(this.ModelState);
            return this.ToActionResult(await service.RefreshToken(request, this.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty)));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        {
            if (!this.ModelState.IsValid)
                return this.BadRequest(this.ModelState);
            return this.ToActionResult(await service.ResetPassword(request));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!this.ModelState.IsValid)
                return this.BadRequest(this.ModelState);
            return this.ToActionResult(await service.ChangePassword(request,
                this.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

        /// <summary>
        /// Logout and revoke refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        /// <summary>
        /// Executes logout.
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            return this.ToActionResult(await service.Logout(this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        /// <summary>
        /// Gets the current User.
        /// </summary>
        public async Task<IActionResult> GetCurrentUser()
        {
            return this.ToActionResult(await service.GetCurrentUser(this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

    }

}
