using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Controller;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller;

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
    private readonly TokenService _realTokens;

    public UserInvitationApiTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddDbContext<FeuerwehrDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FeuerwehrDbContext>().AddDefaultTokenProviders().AddSignInManager();
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<FeuerwehrDbContext>().Database.EnsureCreated();
        _users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        _roles = _provider.GetRequiredService<RoleManager<IdentityRole>>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frontend:BaseUrl"] = "https://feuerwehr.example",
            ["JwtSettings:SecretKey"] = "invitation-tests-signing-key-at-least-32-characters",
            ["JwtSettings:Issuer"] = "invitation-tests",
            ["JwtSettings:Audience"] = "invitation-tests",
        }).Build();
        _realTokens = new TokenService(config, _users);
        _tokens.Setup(tokens => tokens.GenerateAccessTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("test-access-token");
        _tokens.Setup(tokens => tokens.GenerateRefreshToken()).Returns("test-refresh-token");
        var invitations = new UserInvitationService(_users, _store, _mail, _tokens.Object, config, _clock,
            NullLogger<UserInvitationService>.Instance);
        _usersApi = new UsersController(new UsersService(_users, _roles, invitations, NullLogger<UsersService>.Instance));
        _authApi = new AuthController(new AuthService(_users, _provider.GetRequiredService<SignInManager<ApplicationUser>>(),
            invitations, _tokens.Object, config));
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
    /// <summary>
    /// Creates the user Sends Uuid Link And Creates Account Without Password.
    /// </summary>
    public async Task CreateUser_SendsUuidLinkAndCreatesAccountWithoutPassword()
    {
        var (user, id) = await CreateInvitedUser();
        Assert.False(await _users.HasPasswordAsync(user));
        Assert.True(user.PasswordResetRequired);
        Assert.True(await _users.IsInRoleAsync(user, "Viewer"));
        var mail = Assert.Single(_mail.Messages);
        Assert.Equal(user.Email, mail.Recipient);
        var link = new Uri(Regex.Match(mail.Body, @"https://[^\s]+").Value);
        Assert.Equal($"https://feuerwehr.example/password/{id:D}", link.AbsoluteUri);
        var invitation = _store.Entries[id];
        Assert.DoesNotContain(invitation.ResetToken, mail.Body);
        Assert.Equal(user.Id, invitation.UserId);
        Assert.Equal(_clock.GetUtcNow().AddMinutes(60), invitation.ExpiresAtUtc);
        Assert.Equal(TimeSpan.FromMinutes(60), _store.StoredLifetime);
        Assert.Contains("60 Minuten", mail.Body);
    }

    [Fact]
    /// <summary>
    /// Executes fire Department Assignment Is Persisted And Returned And Can Be Changed Or Cleared.
    /// </summary>
    public async Task FireDepartmentAssignment_IsPersistedAndReturned_AndCanBeChangedOrCleared()
    {
        var db = _provider.GetRequiredService<FeuerwehrDbContext>();
        var first = new FireDepartment { Name = "Feuerwehr A" };
        var second = new FireDepartment { Name = "Feuerwehr B" };
        db.FireDepartments.AddRange(first, second);
        await db.SaveChangesAsync();
        Assert.True((await _roles.CreateAsync(new IdentityRole("EquipmentOfficer"))).Succeeded);

        var response = Assert.IsType<CreatedAtActionResult>(await _usersApi.CreateUser(new RegisterUserRequest
        {
            Email = "officer@example.com", FirstName = "Ada", LastName = "Muster",
            Roles = ["EquipmentOfficer"], FireDepartmentId = first.Id
        }));
        var created = Assert.IsType<UserSummaryDto>(response.Value);
        Assert.Equal(first.Id, created.FireDepartmentId);
        Assert.Equal(first.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
        var listed = Assert.IsType<List<UserDetailsDto>>(Assert.IsType<OkObjectResult>(await _usersApi.GetAllUsers()).Value);
        Assert.Equal(first.Id, Assert.Single(listed).FireDepartmentId);

        Assert.IsType<OkObjectResult>(await _usersApi.UpdateUser(created.Id, new UpdateUserRequest { FireDepartmentId = second.Id }));
        Assert.Equal(second.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
        // Existing clients can still update unrelated fields without removing the assignment.
        Assert.IsType<OkObjectResult>(await _usersApi.UpdateUser(created.Id, new UpdateUserRequest { FirstName = "Bea" }));
        Assert.Equal(second.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
        Assert.IsType<OkObjectResult>(await _usersApi.UpdateUser(created.Id, new UpdateUserRequest { ClearFireDepartment = true }));
        Assert.Null((await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
    }

    [Fact]
    /// <summary>
    /// Executes reset Password Validates Email Sets Real Password And Rejects Replay.
    /// </summary>
    public async Task ResetPassword_ValidatesEmailSetsRealPasswordAndRejectsReplay()
    {
        var (user, id) = await CreateInvitedUser();
        var staleInvitation = _store.Entries[id];
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id, " NEW.USER@EXAMPLE.COM ")));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
        Assert.Empty(_store.Entries);
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(2));
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
        // Even a stale cache copy cannot replay the underlying Identity token.
        _store.Entries[id] = staleInvitation;
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, password: "AnotherPassword123")));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Fact]
    /// <summary>
    /// Executes reset Password Wrong Email Cannot Select Another User Or Consume Link.
    /// </summary>
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
    /// <summary>
    /// Executes reset Password Expired Invitation Does Not Set Password.
    /// </summary>
    public async Task ResetPassword_ExpiredInvitationDoesNotSetPassword()
    {
        var (user, id) = await CreateInvitedUser();
        _clock.Now = _clock.Now.AddMinutes(60);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
        Assert.False(await _users.HasPasswordAsync(user));
    }

    [Fact]
    /// <summary>
    /// Executes reset Password Invitation Is Valid Just Before Sixty Minute Expiry.
    /// </summary>
    public async Task ResetPassword_InvitationIsValidJustBeforeSixtyMinuteExpiry()
    {
        var (user, id) = await CreateInvitedUser();
        _clock.Now = _clock.Now.AddMinutes(60).AddSeconds(-1);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id)));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Fact]
    /// <summary>
    /// Executes reset Password Cache Deletion Failure Still Revokes Tokens And Returns Success.
    /// </summary>
    public async Task ResetPassword_CacheDeletionFailureStillRevokesTokensAndReturnsSuccess()
    {
        var (user, id) = await CreateInvitedUser();
        var revoked = false;
        var revokedBeforeCleanup = false;
        _tokens.Setup(tokens => tokens.RevokeRefreshTokenAsync(user.Id))
            .Callback(() => revoked = true).Returns(Task.CompletedTask);
        _store.BeforeRemove = () => revokedBeforeCleanup = revoked;
        _store.FailRemoval = true;

        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id)));
        Assert.True(revokedBeforeCleanup);
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(2));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));

        // Failed cleanup leaves the entry with its original expiry, but Identity prevents replay.
        var staleInvitation = Assert.Single(_store.Entries).Value;
        Assert.Equal(_clock.GetUtcNow().AddMinutes(60), staleInvitation.ExpiresAtUtc);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, password: "AnotherPassword123")));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("0db09979-818f-4b48-91ba-1a5d836b8da2")]
    /// <summary>
    /// Executes reset Password Invalid Or Unknown Uuid Is Rejected.
    /// </summary>
    /// <param name="token">The token.</param>
    public async Task ResetPassword_InvalidOrUnknownUuidIsRejected(string token)
    {
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(new ResetRequest
        {
            Token = token, Email = "new.user@example.com", NewPassword = "MyNewPassword123",
        }));
    }

    [Fact]
    /// <summary>
    /// Executes reset Password Weak Password Leaves Invitation Available For Retry.
    /// </summary>
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
    /// <summary>
    /// Executes reset Password Rejects Changed Or Unavailable Account.
    /// </summary>
    /// <param name="change">The change.</param>
    public async Task ResetPassword_RejectsChangedOrUnavailableAccount(string change)
    {
        var (user, id) = await CreateInvitedUser();
        if (change == "deleted") await _users.DeleteAsync(user);
        else if (change == "changed-email") await _users.SetEmailAsync(user, "changed@example.com");
        else { user.IsActive = false; await _users.UpdateAsync(user); }
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
    }

    [Fact]
    /// <summary>
    /// Creates the user Mail Failure Rolls Back Account And Invitation.
    /// </summary>
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
    /// <summary>
    /// Executes reset Password Invalid Model Is Rejected At Api Boundary.
    /// </summary>
    public async Task ResetPassword_InvalidModelIsRejectedAtApiBoundary()
    {
        _authApi.ModelState.AddModelError("Email", "Eine E-Mail-Adresse ist erforderlich.");
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(new ResetRequest()));
        var validation = new List<ValidationResult>();
        var request = new ResetRequest();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), validation, true));
        Assert.Contains(validation, result => result.MemberNames.Contains(nameof(ResetRequest.Email)));
    }

    /// <summary>
    /// Executes dispose.
    /// </summary>
    public void Dispose() => _provider.Dispose();

    private void SignInAs(string userId)
    {
        _authApi.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)], "test")),
            },
        };
    }

    [Fact]
    /// <summary>
    /// Executes change Password Uses Signed In User And Requires Current Password.
    /// </summary>
    public async Task ChangePassword_UsesSignedInUserAndRequiresCurrentPassword()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        SignInAs(user.Id);
        var request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
        Assert.IsType<BadRequestObjectResult>(await _authApi.ChangePassword(request));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
        request.CurrentPassword = "MyNewPassword123";
        Assert.IsType<OkObjectResult>(await _authApi.ChangePassword(request));
        Assert.True(await _users.CheckPasswordAsync(user, "ChangedPassword123"));
        Assert.False(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(3));
    }

    [Fact]
    /// <summary>
    /// Executes change Password Rejects Missing User Or Invalid Model.
    /// </summary>
    public async Task ChangePassword_RejectsMissingUserOrInvalidModel()
    {
        var request = new ChangePasswordRequest { CurrentPassword = "CurrentPassword123", NewPassword = "ChangedPassword123" };
        Assert.IsType<UnauthorizedResult>(await _authApi.ChangePassword(request));
        SignInAs("unknown");
        Assert.IsType<UnauthorizedResult>(await _authApi.ChangePassword(request));
        _authApi.ModelState.AddModelError("CurrentPassword", "Required");
        Assert.IsType<BadRequestObjectResult>(await _authApi.ChangePassword(request));
    }

    [Fact]
    /// <summary>
    /// Executes change Password Five Wrong Current Passwords Lock Account And Prevent Changes.
    /// </summary>
    public async Task ChangePassword_FiveWrongCurrentPasswordsLockAccountAndPreventChanges()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        SignInAs(user.Id);
        _tokens.Invocations.Clear();
        var request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
        for (var attempt = 1; attempt < 5; attempt++)
        {
            Assert.IsType<BadRequestObjectResult>(await _authApi.ChangePassword(request));
            Assert.Equal(attempt, await _users.GetAccessFailedCountAsync(user));
        }

        var beforeLockout = DateTimeOffset.UtcNow;
        var locked = Assert.IsType<ObjectResult>(await _authApi.ChangePassword(request));
        Assert.Equal(423, locked.StatusCode);
        Assert.True(await _users.IsLockedOutAsync(user));
        var lockoutEnd = await _users.GetLockoutEndDateAsync(user);
        Assert.NotNull(lockoutEnd);
        Assert.InRange(lockoutEnd.Value, beforeLockout.AddMinutes(15), DateTimeOffset.UtcNow.AddMinutes(15));

        // The valid bearer token and even the correct password cannot bypass the lockout.
        request.CurrentPassword = "MyNewPassword123";
        Assert.Equal(423, Assert.IsType<ObjectResult>(await _authApi.ChangePassword(request)).StatusCode);
        Assert.Equal(423, Assert.IsType<ObjectResult>((await _authApi.Login(new LoginRequest
        {
            Email = user.Email!, Password = request.CurrentPassword,
        })).Result).StatusCode);
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
        Assert.False(await _users.CheckPasswordAsync(user, "ChangedPassword123"));
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    /// <summary>
    /// Executes change Password Shares Failed Attempt Counter With Login.
    /// </summary>
    public async Task ChangePassword_SharesFailedAttemptCounterWithLogin()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        SignInAs(user.Id);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Assert.IsType<UnauthorizedObjectResult>((await _authApi.Login(new LoginRequest
            {
                Email = user.Email!, Password = "WrongPassword123",
            })).Result);
        }
        Assert.Equal(3, await _users.GetAccessFailedCountAsync(user));
        var request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
        Assert.IsType<BadRequestObjectResult>(await _authApi.ChangePassword(request));
        Assert.Equal(4, await _users.GetAccessFailedCountAsync(user));
        Assert.Equal(423, Assert.IsType<ObjectResult>(await _authApi.ChangePassword(request)).StatusCode);
    }

    [Fact]
    /// <summary>
    /// Executes change Password Rejects Preexisting Lockout And Allows Change After Expiry.
    /// </summary>
    public async Task ChangePassword_RejectsPreexistingLockoutAndAllowsChangeAfterExpiry()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        SignInAs(user.Id);
        await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
        var request = new ChangePasswordRequest { CurrentPassword = "MyNewPassword123", NewPassword = "ChangedPassword123" };
        Assert.Equal(423, Assert.IsType<ObjectResult>(await _authApi.ChangePassword(request)).StatusCode);
        await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.IsType<OkObjectResult>(await _authApi.ChangePassword(request));
        Assert.True(await _users.CheckPasswordAsync(user, request.NewPassword));
    }

    [Fact]
    /// <summary>
    /// Executes change Password Correct Current Password Resets Failures Even When New Password Is Weak.
    /// </summary>
    public async Task ChangePassword_CorrectCurrentPasswordResetsFailuresEvenWhenNewPasswordIsWeak()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        SignInAs(user.Id);
        await _authApi.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123",
        });
        Assert.Equal(1, await _users.GetAccessFailedCountAsync(user));
        Assert.IsType<BadRequestObjectResult>(await _authApi.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "MyNewPassword123", NewPassword = "weak",
        }));
        Assert.Equal(0, await _users.GetAccessFailedCountAsync(user));
        Assert.False(await _users.IsLockedOutAsync(user));
        Assert.True(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Fact]
    /// <summary>
    /// Executes resend Invitation Locks Existing Account Until Successful Password Reset.
    /// </summary>
    public async Task ResendInvitation_LocksExistingAccountUntilSuccessfulPasswordReset()
    {
        var (user, firstId) = await CreateInvitedUser();
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(firstId)));
        _tokens.Invocations.Clear();
        Assert.IsType<OkObjectResult>(await _usersApi.ResendInvitation(user.Id));
        var id = Assert.Single(_store.Entries).Key;
        Assert.NotEqual(firstId, id);
        Assert.Equal(2, _mail.Messages.Count);
        _tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Once);

        // Reload the persisted lock, including the status returned to the administration UI.
        _provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
        user = (await _users.FindByIdAsync(user.Id))!;
        Assert.True(user.PasswordResetRequired);
        Assert.True(user.IsActive);
        var details = Assert.IsType<UserDetailsDto>(Assert.IsType<OkObjectResult>(await _usersApi.GetUser(user.Id)).Value);
        Assert.True(details.PasswordResetRequired);
        Assert.Equal(423, Assert.IsType<ObjectResult>((await _authApi.Login(new LoginRequest
        {
            Email = user.Email!, Password = "MyNewPassword123",
        })).Result).StatusCode);
        SignInAs(user.Id);
        Assert.IsType<UnauthorizedResult>(await _authApi.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "MyNewPassword123", NewPassword = "ChangedPassword123",
        }));
        Assert.IsType<BadRequestObjectResult>(await _usersApi.UpdateUser(user.Id, new UpdateUserRequest
        {
            NewPassword = "ChangedPassword123",
        }));
        _authApi.Request.Headers.Authorization = "Bearer previous-access-token";
        _tokens.Setup(tokens => tokens.GetPrincipalFromExpiredToken("previous-access-token")).Returns(_authApi.User);
        _tokens.Setup(tokens => tokens.ValidateRefreshTokenAsync(user.Id, "previous-refresh-token")).ReturnsAsync(true);
        Assert.IsType<UnauthorizedObjectResult>((await _authApi.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "previous-refresh-token",
        })).Result);

        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, password: "weak")));
        Assert.True(user.PasswordResetRequired);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id, "wrong@example.com")));
        Assert.True(user.PasswordResetRequired);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id, password: "ChangedPassword123")));
        Assert.False(user.PasswordResetRequired);
        Assert.IsType<OkObjectResult>((await _authApi.Login(new LoginRequest
        {
            Email = user.Email!, Password = "ChangedPassword123",
        })).Result);
        Assert.False(await _users.CheckPasswordAsync(user, "MyNewPassword123"));
    }

    [Fact]
    /// <summary>
    /// Executes resend Invitation Invalidates Previous Links And Keeps Lock After Expiry.
    /// </summary>
    public async Task ResendInvitation_InvalidatesPreviousLinksAndKeepsLockAfterExpiry()
    {
        var (user, oldId) = await CreateInvitedUser();
        Assert.IsType<OkObjectResult>(await _usersApi.ResendInvitation(user.Id));
        var id = _store.Entries.Keys.Single(key => key != oldId);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(oldId)));
        Assert.True(user.PasswordResetRequired);
        _clock.Now = _clock.Now.AddMinutes(60);
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(id)));
        Assert.True(user.PasswordResetRequired);

        Assert.IsType<OkObjectResult>(await _usersApi.ResendInvitation(user.Id));
        var freshId = _store.Entries.Keys.Single(key => key != oldId && key != id);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(freshId)));
        Assert.False(user.PasswordResetRequired);
    }

    [Fact]
    /// <summary>
    /// Executes resend Invitation Revokes Existing Access Tokens Including After Unlock.
    /// </summary>
    public async Task ResendInvitation_RevokesExistingAccessTokensIncludingAfterUnlock()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        var jwt = await _realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
        var principal = Assert.IsType<ClaimsPrincipal>(_realTokens.GetPrincipalFromExpiredToken(jwt));
        Assert.True(await _realTokens.IsAccessTokenValidAsync(principal));
        Assert.False(await _realTokens.IsAccessTokenValidAsync(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id)]))));

        await _usersApi.ResendInvitation(user.Id);
        Assert.False(await _realTokens.IsAccessTokenValidAsync(principal));
        // Even a token with the current stamp cannot bypass the persisted invitation lock.
        var pendingJwt = await _realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
        var pendingPrincipal = _realTokens.GetPrincipalFromExpiredToken(pendingJwt)!;
        Assert.False(await _realTokens.IsAccessTokenValidAsync(pendingPrincipal));
        var newId = Assert.Single(_store.Entries).Key;
        await _authApi.ResetPassword(Request(newId));
        Assert.False(await _realTokens.IsAccessTokenValidAsync(principal));
        Assert.False(await _realTokens.IsAccessTokenValidAsync(pendingPrincipal));
        var newJwt = await _realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
        Assert.True(await _realTokens.IsAccessTokenValidAsync(_realTokens.GetPrincipalFromExpiredToken(newJwt)!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    /// <summary>
    /// Executes resend Invitation Mail Failure Restores Previous Lock And Invitation.
    /// </summary>
    /// <param name="alreadyInvited">The already Invited.</param>
    public async Task ResendInvitation_MailFailureRestoresPreviousLockAndInvitation(bool alreadyInvited)
    {
        var (user, id) = await CreateInvitedUser();
        if (!alreadyInvited) await _authApi.ResetPassword(Request(id));
        var stamp = user.SecurityStamp;
        _mail.Fail = true;
        Assert.Equal(503, Assert.IsType<ObjectResult>(await _usersApi.ResendInvitation(user.Id)).StatusCode);
        _provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
        user = (await _users.FindByIdAsync(user.Id))!;
        Assert.Equal(alreadyInvited, user.PasswordResetRequired);
        Assert.Equal(stamp, user.SecurityStamp);
        if (alreadyInvited)
        {
            Assert.Equal(id, Assert.Single(_store.Entries).Key);
            Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(id)));
        }
        else
        {
            Assert.Empty(_store.Entries);
            Assert.IsType<OkObjectResult>((await _authApi.Login(new LoginRequest
            {
                Email = user.Email!, Password = "MyNewPassword123",
            })).Result);
        }
    }

    [Fact]
    /// <summary>
    /// Executes resend Invitation Password Reset Also Clears Existing Failed Login Lockout.
    /// </summary>
    public async Task ResendInvitation_PasswordResetAlsoClearsExistingFailedLoginLockout()
    {
        var (user, id) = await CreateInvitedUser();
        await _authApi.ResetPassword(Request(id));
        for (var attempt = 0; attempt < 5; attempt++)
            await _authApi.Login(new LoginRequest { Email = user.Email!, Password = "WrongPassword123" });
        Assert.True(await _users.IsLockedOutAsync(user));
        await _usersApi.ResendInvitation(user.Id);
        var newId = Assert.Single(_store.Entries).Key;
        Assert.IsType<BadRequestObjectResult>(await _authApi.ResetPassword(Request(newId, password: "weak")));
        Assert.True(await _users.IsLockedOutAsync(user));
        Assert.True(user.PasswordResetRequired);
        Assert.IsType<OkObjectResult>(await _authApi.ResetPassword(Request(newId)));
        _provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
        user = (await _users.FindByIdAsync(user.Id))!;
        Assert.False(user.PasswordResetRequired);
        Assert.False(await _users.IsLockedOutAsync(user));
        Assert.Equal(0, await _users.GetAccessFailedCountAsync(user));
        Assert.IsType<OkObjectResult>((await _authApi.Login(new LoginRequest
        {
            Email = user.Email!, Password = "MyNewPassword123",
        })).Result);
    }

    [Fact]
    /// <summary>
    /// Executes resend Invitation Rejects Unknown And Inactive Users.
    /// </summary>
    public async Task ResendInvitation_RejectsUnknownAndInactiveUsers()
    {
        Assert.IsType<NotFoundObjectResult>(await _usersApi.ResendInvitation("missing"));
        var (user, _) = await CreateInvitedUser();
        user.IsActive = false;
        await _users.UpdateAsync(user);
        Assert.IsType<BadRequestObjectResult>(await _usersApi.ResendInvitation(user.Id));
        Assert.Single(_mail.Messages);
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// Gets the utc Now.
        /// </summary>
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryInvitations : IUserInvitationStore
    {
        public Dictionary<Guid, UserInvitation> Entries { get; } = [];
        public TimeSpan StoredLifetime { get; private set; }
        public bool FailRemoval { get; set; }
        public Action? BeforeRemove { get; set; }
        /// <summary>
        /// Executes store.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="invitation">The invitation.</param>
        /// <param name="lifetime">The lifetime.</param>
        public Task StoreAsync(Guid id, UserInvitation invitation, TimeSpan lifetime)
        {
            Entries[id] = invitation; StoredLifetime = lifetime; return Task.CompletedTask;
        }
        /// <summary>
        /// Executes find.
        /// </summary>
        public Task<UserInvitation?> FindAsync(Guid id) => Task.FromResult(Entries.GetValueOrDefault(id));
        /// <summary>
        /// Removes the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public Task RemoveAsync(Guid id)
        {
            BeforeRemove?.Invoke();
            if (FailRemoval) throw new TimeoutException("Redis unavailable");
            Entries.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingMail : IMailService
    {
        public List<(string Recipient, string Subject, string Body)> Messages { get; } = [];
        public bool Fail { get; set; }
        /// <summary>
        /// Sends the operation.
        /// </summary>
        /// <param name="recipient">The recipient.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        public Task SendAsync(string recipient, string subject, string body)
        {
            if (Fail) throw new System.Net.Mail.SmtpException("SMTP unavailable");
            Messages.Add((recipient, subject, body)); return Task.CompletedTask;
        }
    }
}

