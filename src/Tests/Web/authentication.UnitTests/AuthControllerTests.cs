using de.openelp.authentification.Controllers;
using de.openelp.authentification.Data;
using de.openelp.authentification.Models;
using de.openelp.authentification.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace de.openelp.authentification.UnitTests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_WithExistingUsername_ReturnsBadRequest()
    {
        var context = CreateDbContext();
        context.Users.Add(new User { Username = "alice", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var jwtMock = new Mock<IJwtService>();
        var controller = CreateController(context, hasherMock.Object, jwtMock.Object);

        var result = await controller.Register(new RegisterRequest("alice", "secret"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Benutzername existiert bereits.", badRequest.Value);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var context = CreateDbContext();
        context.Users.Add(new User { Username = "bob", PasswordHash = "stored-hash" });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var jwtMock = new Mock<IJwtService>();
        var controller = CreateController(context, hasherMock.Object, jwtMock.Object);

        var result = await controller.Login(new LoginRequest("bob", "wrong-password"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Ungültige Anmeldedaten.", unauthorized.Value);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        var context = CreateDbContext();
        context.Users.Add(new User { Username = "charlie", PasswordHash = "stored-hash" });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var jwtMock = new Mock<IJwtService>();
        jwtMock
            .Setup(x => x.GenerateTokens(It.IsAny<User>()))
            .Returns(("access-1", "refresh-1"));

        var controller = CreateController(context, hasherMock.Object, jwtMock.Object);

        var result = await controller.Login(new LoginRequest("charlie", "secret"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var accessToken = ok.Value.GetType().GetProperty("AccessToken")?.GetValue(ok.Value) as string;
        var refreshToken = ok.Value.GetType().GetProperty("RefreshToken")?.GetValue(ok.Value) as string;

        Assert.Equal("access-1", accessToken);
        Assert.Equal("refresh-1", refreshToken);
        Assert.Single(context.RefreshTokens);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RevokesOldAndCreatesNewRefreshToken()
    {
        var context = CreateDbContext();
        var user = new User { Username = "dana", PasswordHash = "stored-hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var oldToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "refresh-old",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };
        context.RefreshTokens.Add(oldToken);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var jwtMock = new Mock<IJwtService>();
        jwtMock
            .Setup(x => x.GenerateTokens(It.IsAny<User>()))
            .Returns(("access-new", "refresh-new"));

        var controller = CreateController(context, hasherMock.Object, jwtMock.Object);

        var result = await controller.Refresh(new RefreshRequest("refresh-old"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var accessToken = ok.Value.GetType().GetProperty("AccessToken")?.GetValue(ok.Value) as string;
        var refreshToken = ok.Value.GetType().GetProperty("RefreshToken")?.GetValue(ok.Value) as string;

        Assert.Equal("access-new", accessToken);
        Assert.Equal("refresh-new", refreshToken);

        var storedOld = await context.RefreshTokens.FirstAsync(x => x.Token == "refresh-old");
        var storedNew = await context.RefreshTokens.FirstAsync(x => x.Token == "refresh-new");

        Assert.True(storedOld.IsRevoked);
        Assert.Equal("refresh-old", storedNew.ReplacedByToken);
    }

    private static AuthController CreateController(AuthDbContext context, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        var controller = new AuthController(context, passwordHasher, jwtService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }
}
