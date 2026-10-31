using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server.Authorization;
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
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        /// <summary>
        /// Executes login.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return this.ToActionResult(await service.Login(request));
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        /// <summary>
        /// Executes refresh Token.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return this.ToActionResult(await service.RefreshToken(request, Request.Headers["Authorization"].ToString().Replace("Bearer ", "")));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        /// <summary>
        /// Executes reset Password.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return this.ToActionResult(await service.ChangePassword(request,
                User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
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
            return this.ToActionResult(await service.Logout(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
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
            return this.ToActionResult(await service.GetCurrentUser(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));
        }

    }

}
