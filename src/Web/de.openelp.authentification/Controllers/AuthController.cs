
using de.openelp.authentification.Data;
using de.openelp.authentification.Models;
using de.openelp.authentification.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.authentification.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthController(AuthDbContext context, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("Benutzername existiert bereits.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return await Login(new LoginRequest(request.Username, request.Password));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Ungültige Anmeldedaten.");

        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        if (tokenRecord == null)
            return Unauthorized("Ungültiger oder abgelaufener Refresh Token.");

        // ROTATION: Altes Token widerrufen
        tokenRecord.IsRevoked = true;
        tokenRecord.RevokedAt = DateTime.UtcNow;

        var user = await _context.Users.FindAsync(tokenRecord.UserId);
        if (user == null)
            return Unauthorized("Benutzer nicht gefunden.");

        var (newAccessToken, newRefreshToken) = _jwtService.GenerateTokens(user);

        // Neues Refresh Token speichern
        var newTokenRecord = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ReplacedByToken = tokenRecord.Token // Verknüpfung für Audit
        };

        _context.RefreshTokens.Add(newTokenRecord);
        await _context.SaveChangesAsync();

        return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
    }
}

// DTOs
public record RegisterRequest(string Username, string Password);

public record LoginRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);