using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Controller;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace de.openelp.feuerwehr.Tests.Backend.Controller;

// Real Identity token/password handling and EF storage, with mail/Redis replaced at their boundaries.
public sealed class UserInvitationApiTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly MemoryInvitations _store = new();
    private readonly CapturingMail _mail = new();
    private readonly MutableClock _clock = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly UsersController _usersApi;
    private readonly AuthController _authApi;

    public UserInvitationApiTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FeuerwehrDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FeuerwehrDbContext>().AddDefaultTokenProviders();
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<FeuerwehrDbContext>().Database.EnsureCreated();
        _users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        _roles = _provider.GetRequiredService<RoleManager<IdentityRole>>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frontend:BaseUrl"] = "https://feuerwehr.example",
        }).Build();
        var invitations = new UserInvitationService(_users, _store, _mail, _tokens.Object, config, _clock);
        _usersApi = new UsersController(new UsersService(_users, _roles, invitations, NullLogger<UsersService>.Instance));
        _authApi = new AuthController(new AuthService(_users, null!, invitations, _tokens.Object, config));
    }

    private async Task<(ApplicationUser User, Guid Id)> CreateInvitedUser()
    {
        if (!await _roles.RoleExistsAsync("Viewer"))
            Assert.True((await _roles.CreateAsync(new IdentityRole("Viewer"))).Succeeded);
        var response = await _usersApi.CreateUser(new RegisterUserRequest
        {
            Email = "new.user@example.com", FirstName = "Ada", LastName = "Muster", Roles = ["Viewer"],
        });
        Assert.IsType<CreatedAtActionResult>(response);
        var user = await _users.FindByEmailAsync("new.user@example.com");
        Assert.NotNull(user);
        return (user, Assert.Single(_store.Entries).Key);
    }

    private static ResetRequest Request(Guid id, string email = "new.user@example.com", string password = "MyNewPassword123") =>
        new() { Token = id.ToString("D"), Email = email, NewPassword = password };

    [Fact]
    public async Task CreateUser_SendsUuidLinkAndCreatesAccountWithoutPassword()
    {
        var (user, id) = await CreateInvitedUser();
        Assert.False(await _users.HasPasswordAsync(user));
        Assert.True(await _users.IsInRoleAsync(user, "Viewer"));
        var mail = Assert.Single(_mail.Messages);
        Assert.Equal(user.Email, mail.Recipient);
        var link = new Uri(Regex.Match(mail.Body, @"https://[^\s]+").Value);
        Assert.Equal($"https://feuerwehr.example/password/{id:D}", link.AbsoluteUri);
        var invitation = _store.Entries[id];
        Assert.DoesNotContain(invitation.ResetToken, mail.Body);
        Assert.Equal(user.Id, invitation.UserId);
        Assert.Equal(_clock.GetUtcNow().AddHours(24), invitation.ExpiresAtUtc);
        Assert.Equal(TimeSpan.FromHours(24), _store.StoredLifetime);
    }

    [Fact]
    public async Task ResetPassword_ValidatesEmailSetsRealPasswordAndRejectsReplay()
    {
        var (user, id) = await CreateInvitedUser();
        var staleInvitation = _store.Entries[id];
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id, " NEW.USER@EXAMPLE.COM ")));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
        Assert.Empty(_store.Entries);
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Once);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
        // Even a stale cache copy cannot replay the underlying Identity token.
        _store.Entries[id] = staleInvitation;
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, password: "AnotherPassword123")));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Fact]
    public async Task ResetPassword_WrongEmailCannotSelectAnotherUserOrConsumeLink()
    {
        var (user, id) = await CreateInvitedUser();
        var other = new ApplicationUser { UserName = "other@example.com", Email = "other@example.com" };
        Assert.True((await _users.CreateAsync(other, "OtherPassword123")).Succeeded);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, other.Email!)));
        Assert.False(await _users.HasPasswordAsync(user));
        Assert.True(await _users.CheckPasswordAsync(other, "OtherPassword123"));
        Assert.Contains(id, _store.Entries.Keys);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id)));
    }

    [Fact]
    public async Task ResetPassword_ExpiredInvitationDoesNotSetPassword()
    {
        var (user, id) = await CreateInvitedUser();
        _clock.Now = _clock.Now.AddHours(24);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
        Assert.False(await _users.HasPasswordAsync(user));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("0db09979-818f-4b48-91ba-1a5d836b8da2")]
    public async Task ResetPassword_InvalidOrUnknownUuidIsRejected(string token)
    {
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(new ResetRequest
        {
            Token = token, Email = "new.user@example.com", NewPassword = "MyNewPassword123",
        }));
    }

    [Fact]
    public async Task ResetPassword_WeakPasswordLeavesInvitationAvailableForRetry()
    {
        var (user, id) = await CreateInvitedUser();
        var failure = Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, password: "weak")));
        Assert.NotEmpty(Assert.IsType<IdentityErrorResponse>(failure.Value).Errors);
        Assert.False(await _users.HasPasswordAsync(user));
        Assert.Contains(id, _store.Entries.Keys);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id)));
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("changed-email")]
    public async Task ResetPassword_RejectsChangedOrUnavailableAccount(string change)
    {
        var (user, id) = await CreateInvitedUser();
        if (change == "deleted") await _users.DeleteAsync(user);
        else if (change == "changed-email") await _users.SetEmailAsync(user, "changed@example.com");
        else { user.IsActive = false; await _users.UpdateAsync(user); }
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
    }

    [Fact]
    public async Task CreateUser_MailFailureRollsBackAccountAndInvitation()
    {
        _mail.Fail = true;
        var result = Assert.IsType<ObjectResult>(await _usersApi.CreateUser(new RegisterUserRequest
        {
            Email = "new.user@example.com", FirstName = "Ada", LastName = "Muster", Roles = [],
        }));
        Assert.Equal(503, result.StatusCode);
        Assert.Null(await _users.FindByEmailAsync("new.user@example.com"));
        Assert.Empty(_store.Entries);
    }

    [Fact]
    public async Task ResetPassword_InvalidModelIsRejectedAtApiBoundary()
    {
        _authApi.ModelState.AddModelError("Email", "Eine E-Mail-Adresse ist erforderlich.");
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(new ResetRequest()));
        var validation = new List<ValidationResult>();
        var request = new ResetRequest();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), validation, true));
        Assert.Contains(validation, result => result.MemberNames.Contains(nameof(ResetRequest.Email)));
    }

    public void Dispose() => _provider.Dispose();

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryInvitations : IUserInvitationStore
    {
        public Dictionary<Guid, UserInvitation> Entries { get; } = [];
        public TimeSpan StoredLifetime { get; private set; }
        public Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime)
        {
            Entries[id] = invitation; StoredLifetime = lifetime; return Task.CompletedTask;
        }
        public Task<UserInvitation?> FindAsync(Guid id) => Task.FromResult(Entries.GetValueOrDefault(id));
        public Task RemoveAsync(Guid id) { Entries.Remove(id); return Task.CompletedTask; }
    }

    private sealed class CapturingMail : IMailService
    {
        public List<(string Recipient, string Subject, string Body)> Messages { get; } = [];
        public bool Fail { get; set; }
        public Task SendAsync(string recipient, string subject, string body)
        {
            if (Fail) throw new System.Net.Mail.SmtpException("SMTP unavailable");
            Messages.Add((recipient, subject, body)); return Task.CompletedTask;
        }
    }
}
