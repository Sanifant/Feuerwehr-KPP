using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService service) : ControllerBase
    {

        /// <summary>
        /// Login with email and password.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        [HttpPost("login")]
        [AllowAnonymous]
        /// <summary>
        /// Executes login.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.Login(request));
        }

        /// <summary>
        /// Refresh access token using refresh token.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        /// <summary>
        /// Executes refresh Token.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.RefreshToken(request, this.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty)));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        /// <summary>
        /// Executes reset Password.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            return this.ToActionResult(await service.ResetPassword(request));
        }

        [HttpPost("change-password")]
        [Authorize]
        /// <summary>
        /// Executes change Password.
        /// </summary>
        /// <param name="request">The request.</param>
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
        /// Logout and revoke refresh token.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Get current user info.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
