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

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller
{
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
            ServiceCollection services = new ServiceCollection();
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
            this._provider = services.BuildServiceProvider();
            this._provider.GetRequiredService<FeuerwehrDbContext>().Database.EnsureCreated();
            this._users = this._provider.GetRequiredService<UserManager<ApplicationUser>>();
            this._roles = this._provider.GetRequiredService<RoleManager<IdentityRole>>();
            IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://feuerwehr.example",
                ["JwtSettings:SecretKey"] = "invitation-tests-signing-key-at-least-32-characters",
                ["JwtSettings:Issuer"] = "invitation-tests",
                ["JwtSettings:Audience"] = "invitation-tests",
            }).Build();
            this._realTokens = new TokenService(config, this._users);
            this._tokens.Setup(tokens => tokens.GenerateAccessTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
                .ReturnsAsync("test-access-token");
            this._tokens.Setup(tokens => tokens.GenerateRefreshToken()).Returns("test-refresh-token");
            UserInvitationService invitations = new UserInvitationService(this._users, this._store, this._mail, this._tokens.Object, config, this._clock,
                NullLogger<UserInvitationService>.Instance);
            this._usersApi = new UsersController(new UsersService(this._users, this._roles, invitations, NullLogger<UsersService>.Instance));
            this._authApi = new AuthController(new AuthService(this._users, this._provider.GetRequiredService<SignInManager<ApplicationUser>>(),
                invitations, this._tokens.Object, config));
        }

        [Fact]
        /// <summary>
        /// Creates the user Sends Uuid Link And Creates Account Without Password.
        /// </summary>
        public async Task CreateUser_SendsUuidLinkAndCreatesAccountWithoutPassword()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            Assert.False(await this._users.HasPasswordAsync(user));
            Assert.True(user.PasswordResetRequired);
            Assert.True(await this._users.IsInRoleAsync(user, "Viewer"));
            (string Recipient, string Subject, string Body) mail = Assert.Single(this._mail.Messages);
            Assert.Equal(user.Email, mail.Recipient);
            Uri link = new Uri(Regex.Match(mail.Body, @"https://[^\s]+").Value);
            Assert.Equal($"https://feuerwehr.example/password/{id:D}", link.AbsoluteUri);
            UserInvitation invitation = this._store.Entries[id];
            Assert.DoesNotContain(invitation.ResetToken, mail.Body);
            Assert.Equal(user.Id, invitation.UserId);
            Assert.Equal(this._clock.GetUtcNow().AddMinutes(60), invitation.ExpiresAtUtc);
            Assert.Equal(TimeSpan.FromMinutes(60), this._store.StoredLifetime);
            Assert.Contains("60 Minuten", mail.Body);
        }

        [Fact]
        /// <summary>
        /// Executes fire Department Assignment Is Persisted And Returned And Can Be Changed Or Cleared.
        /// </summary>
        public async Task FireDepartmentAssignment_IsPersistedAndReturned_AndCanBeChangedOrCleared()
        {
            FeuerwehrDbContext db = this._provider.GetRequiredService<FeuerwehrDbContext>();
            FireDepartment first = new FireDepartment { Name = "Feuerwehr A" };
            FireDepartment second = new FireDepartment { Name = "Feuerwehr B" };
            db.FireDepartments.AddRange(first, second);
            await db.SaveChangesAsync();
            Assert.True((await this._roles.CreateAsync(new IdentityRole("EquipmentOfficer"))).Succeeded);

            CreatedAtActionResult response = Assert.IsType<CreatedAtActionResult>(await this._usersApi.CreateUser(new RegisterUserRequest
            {
                Email = "officer@example.com", FirstName = "Ada", LastName = "Muster",
                Roles = ["EquipmentOfficer"], FireDepartmentId = first.Id
            }));
            UserSummaryDto created = Assert.IsType<UserSummaryDto>(response.Value);
            Assert.Equal(first.Id, created.FireDepartmentId);
            Assert.Equal(first.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
            List<UserDetailsDto> listed = Assert.IsType<List<UserDetailsDto>>(Assert.IsType<OkObjectResult>(await this._usersApi.GetAllUsers()).Value);
            Assert.Equal(first.Id, Assert.Single(listed).FireDepartmentId);

            Assert.IsType<OkObjectResult>(await this._usersApi.UpdateUser(created.Id, new UpdateUserRequest { FireDepartmentId = second.Id }));
            Assert.Equal(second.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
            // Existing clients can still update unrelated fields without removing the assignment.
            Assert.IsType<OkObjectResult>(await this._usersApi.UpdateUser(created.Id, new UpdateUserRequest { FirstName = "Bea" }));
            Assert.Equal(second.Id, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
            Assert.IsType<OkObjectResult>(await this._usersApi.UpdateUser(created.Id, new UpdateUserRequest { ClearFireDepartment = true }));
            Assert.Null((await db.Users.AsNoTracking().SingleAsync(x => x.Id == created.Id)).FireDepartmentId);
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Validates Email Sets Real Password And Rejects Replay.
        /// </summary>
        public async Task ResetPassword_ValidatesEmailSetsRealPasswordAndRejectsReplay()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            UserInvitation staleInvitation = this._store.Entries[id];
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id, " NEW.USER@EXAMPLE.COM ")));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
            Assert.Empty(this._store.Entries);
            this._tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(2));
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id)));
            // Even a stale cache copy cannot replay the underlying Identity token.
            this._store.Entries[id] = staleInvitation;
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, password: "AnotherPassword123")));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Wrong Email Cannot Select Another User Or Consume Link.
        /// </summary>
        public async Task ResetPassword_WrongEmailCannotSelectAnotherUserOrConsumeLink()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            ApplicationUser other = new ApplicationUser { UserName = "other@example.com", Email = "other@example.com" };
            Assert.True((await this._users.CreateAsync(other, "OtherPassword123")).Succeeded);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, other.Email!)));
            Assert.False(await this._users.HasPasswordAsync(user));
            Assert.True(await this._users.CheckPasswordAsync(other, "OtherPassword123"));
            Assert.Contains(id, this._store.Entries.Keys);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id)));
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Expired Invitation Does Not Set Password.
        /// </summary>
        public async Task ResetPassword_ExpiredInvitationDoesNotSetPassword()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            this._clock.Now = this._clock.Now.AddMinutes(60);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id)));
            Assert.False(await this._users.HasPasswordAsync(user));
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Invitation Is Valid Just Before Sixty Minute Expiry.
        /// </summary>
        public async Task ResetPassword_InvitationIsValidJustBeforeSixtyMinuteExpiry()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            this._clock.Now = this._clock.Now.AddMinutes(60).AddSeconds(-1);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id)));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Cache Deletion Failure Still Revokes Tokens And Returns Success.
        /// </summary>
        public async Task ResetPassword_CacheDeletionFailureStillRevokesTokensAndReturnsSuccess()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            bool revoked = false;
            bool revokedBeforeCleanup = false;
            this._tokens.Setup(tokens => tokens.RevokeRefreshTokenAsync(user.Id))
                .Callback(() => revoked = true).Returns(Task.CompletedTask);
            this._store.BeforeRemove = () => revokedBeforeCleanup = revoked;
            this._store.FailRemoval = true;

            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id)));
            Assert.True(revokedBeforeCleanup);
            this._tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(2));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));

            // Failed cleanup leaves the entry with its original expiry, but Identity prevents replay.
            UserInvitation staleInvitation = Assert.Single(this._store.Entries).Value;
            Assert.Equal(this._clock.GetUtcNow().AddMinutes(60), staleInvitation.ExpiresAtUtc);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, password: "AnotherPassword123")));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
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
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(new ResetRequest
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
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            BadRequestObjectResult failure = Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, password: "weak")));
            Assert.NotEmpty(Assert.IsType<IdentityErrorResponse>(failure.Value).Errors);
            Assert.False(await this._users.HasPasswordAsync(user));
            Assert.Contains(id, this._store.Entries.Keys);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id)));
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
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            if (change == "deleted") await this._users.DeleteAsync(user);
            else if (change == "changed-email") await this._users.SetEmailAsync(user, "changed@example.com");
            else { user.IsActive = false;
                await this._users.UpdateAsync(user); }
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id)));
        }

        [Fact]
        /// <summary>
        /// Creates the user Mail Failure Rolls Back Account And Invitation.
        /// </summary>
        public async Task CreateUser_MailFailureRollsBackAccountAndInvitation()
        {
            this._mail.Fail = true;
            ObjectResult result = Assert.IsType<ObjectResult>(await this._usersApi.CreateUser(new RegisterUserRequest
            {
                Email = "new.user@example.com", FirstName = "Ada", LastName = "Muster", Roles = [],
            }));
            Assert.Equal(503, result.StatusCode);
            Assert.Null(await this._users.FindByEmailAsync("new.user@example.com"));
            Assert.Empty(this._store.Entries);
        }

        [Fact]
        /// <summary>
        /// Executes reset Password Invalid Model Is Rejected At Api Boundary.
        /// </summary>
        public async Task ResetPassword_InvalidModelIsRejectedAtApiBoundary()
        {
            this._authApi.ModelState.AddModelError("Email", "Eine E-Mail-Adresse ist erforderlich.");
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(new ResetRequest()));
            List<ValidationResult> validation = new List<ValidationResult>();
            ResetRequest request = new ResetRequest();
            Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), validation, true));
            Assert.Contains(validation, result => result.MemberNames.Contains(nameof(ResetRequest.Email)));
        }

        /// <summary>
        /// Executes dispose.
        /// </summary>
        public void Dispose() => this._provider.Dispose();

        [Fact]
        /// <summary>
        /// Executes change Password Uses Signed In User And Requires Current Password.
        /// </summary>
        public async Task ChangePassword_UsesSignedInUserAndRequiresCurrentPassword()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            this.SignInAs(user.Id);
            ChangePasswordRequest request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ChangePassword(request));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
            request.CurrentPassword = "MyNewPassword123";
            Assert.IsType<OkObjectResult>(await this._authApi.ChangePassword(request));
            Assert.True(await this._users.CheckPasswordAsync(user, "ChangedPassword123"));
            Assert.False(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
            this._tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Exactly(3));
        }

        [Fact]
        /// <summary>
        /// Executes change Password Rejects Missing User Or Invalid Model.
        /// </summary>
        public async Task ChangePassword_RejectsMissingUserOrInvalidModel()
        {
            ChangePasswordRequest request = new ChangePasswordRequest { CurrentPassword = "CurrentPassword123", NewPassword = "ChangedPassword123" };
            Assert.IsType<UnauthorizedResult>(await this._authApi.ChangePassword(request));
            this.SignInAs("unknown");
            Assert.IsType<UnauthorizedResult>(await this._authApi.ChangePassword(request));
            this._authApi.ModelState.AddModelError("CurrentPassword", "Required");
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ChangePassword(request));
        }

        [Fact]
        /// <summary>
        /// Executes change Password Five Wrong Current Passwords Lock Account And Prevent Changes.
        /// </summary>
        public async Task ChangePassword_FiveWrongCurrentPasswordsLockAccountAndPreventChanges()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            this.SignInAs(user.Id);
            this._tokens.Invocations.Clear();
            ChangePasswordRequest request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
            for (int attempt = 1; attempt < 5; attempt++)
            {
                Assert.IsType<BadRequestObjectResult>(await this._authApi.ChangePassword(request));
                Assert.Equal(attempt, await this._users.GetAccessFailedCountAsync(user));
            }

            DateTimeOffset beforeLockout = DateTimeOffset.UtcNow;
            ObjectResult locked = Assert.IsType<ObjectResult>(await this._authApi.ChangePassword(request));
            Assert.Equal(423, locked.StatusCode);
            Assert.True(await this._users.IsLockedOutAsync(user));
            DateTimeOffset? lockoutEnd = await this._users.GetLockoutEndDateAsync(user);
            Assert.NotNull(lockoutEnd);
            Assert.InRange(lockoutEnd.Value, beforeLockout.AddMinutes(15), DateTimeOffset.UtcNow.AddMinutes(15));

            // The valid bearer token and even the correct password cannot bypass the lockout.
            request.CurrentPassword = "MyNewPassword123";
            Assert.Equal(423, Assert.IsType<ObjectResult>(await this._authApi.ChangePassword(request)).StatusCode);
            Assert.Equal(423, Assert.IsType<ObjectResult>((await this._authApi.Login(new LoginRequest
            {
                Email = user.Email!, Password = request.CurrentPassword,
            })).Result).StatusCode);
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
            Assert.False(await this._users.CheckPasswordAsync(user, "ChangedPassword123"));
            this._tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        /// <summary>
        /// Executes change Password Shares Failed Attempt Counter With Login.
        /// </summary>
        public async Task ChangePassword_SharesFailedAttemptCounterWithLogin()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            this.SignInAs(user.Id);
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                Assert.IsType<UnauthorizedObjectResult>((await this._authApi.Login(new LoginRequest
                {
                    Email = user.Email!, Password = "WrongPassword123",
                })).Result);
            }
            Assert.Equal(3, await this._users.GetAccessFailedCountAsync(user));
            ChangePasswordRequest request = new ChangePasswordRequest { CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123" };
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ChangePassword(request));
            Assert.Equal(4, await this._users.GetAccessFailedCountAsync(user));
            Assert.Equal(423, Assert.IsType<ObjectResult>(await this._authApi.ChangePassword(request)).StatusCode);
        }

        [Fact]
        /// <summary>
        /// Executes change Password Rejects Preexisting Lockout And Allows Change After Expiry.
        /// </summary>
        public async Task ChangePassword_RejectsPreexistingLockoutAndAllowsChangeAfterExpiry()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            this.SignInAs(user.Id);
            await this._users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
            ChangePasswordRequest request = new ChangePasswordRequest { CurrentPassword = "MyNewPassword123", NewPassword = "ChangedPassword123" };
            Assert.Equal(423, Assert.IsType<ObjectResult>(await this._authApi.ChangePassword(request)).StatusCode);
            await this._users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));
            Assert.IsType<OkObjectResult>(await this._authApi.ChangePassword(request));
            Assert.True(await this._users.CheckPasswordAsync(user, request.NewPassword));
        }

        [Fact]
        /// <summary>
        /// Executes change Password Correct Current Password Resets Failures Even When New Password Is Weak.
        /// </summary>
        public async Task ChangePassword_CorrectCurrentPasswordResetsFailuresEvenWhenNewPasswordIsWeak()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            this.SignInAs(user.Id);
            await this._authApi.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword123", NewPassword = "ChangedPassword123",
            });
            Assert.Equal(1, await this._users.GetAccessFailedCountAsync(user));
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = "MyNewPassword123", NewPassword = "weak",
            }));
            Assert.Equal(0, await this._users.GetAccessFailedCountAsync(user));
            Assert.False(await this._users.IsLockedOutAsync(user));
            Assert.True(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
        }

        [Fact]
        /// <summary>
        /// Executes resend Invitation Locks Existing Account Until Successful Password Reset.
        /// </summary>
        public async Task ResendInvitation_LocksExistingAccountUntilSuccessfulPasswordReset()
        {
            (ApplicationUser? user, Guid firstId) = await this.CreateInvitedUser();
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(firstId)));
            this._tokens.Invocations.Clear();
            Assert.IsType<OkObjectResult>(await this._usersApi.ResendInvitation(user.Id));
            Guid id = Assert.Single(this._store.Entries).Key;
            Assert.NotEqual(firstId, id);
            Assert.Equal(2, this._mail.Messages.Count);
            this._tokens.Verify(tokens => tokens.RevokeRefreshTokenAsync(user.Id), Times.Once);

            // Reload the persisted lock, including the status returned to the administration UI.
            this._provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
            user = (await this._users.FindByIdAsync(user.Id))!;
            Assert.True(user.PasswordResetRequired);
            Assert.True(user.IsActive);
            UserDetailsDto details = Assert.IsType<UserDetailsDto>(Assert.IsType<OkObjectResult>(await this._usersApi.GetUser(user.Id)).Value);
            Assert.True(details.PasswordResetRequired);
            Assert.Equal(423, Assert.IsType<ObjectResult>((await this._authApi.Login(new LoginRequest
            {
                Email = user.Email!, Password = "MyNewPassword123",
            })).Result).StatusCode);
            this.SignInAs(user.Id);
            Assert.IsType<UnauthorizedResult>(await this._authApi.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = "MyNewPassword123", NewPassword = "ChangedPassword123",
            }));
            Assert.IsType<BadRequestObjectResult>(await this._usersApi.UpdateUser(user.Id, new UpdateUserRequest
            {
                NewPassword = "ChangedPassword123",
            }));
            this._authApi.Request.Headers.Authorization = "Bearer previous-access-token";
            this._tokens.Setup(tokens => tokens.GetPrincipalFromExpiredToken("previous-access-token")).Returns(this._authApi.User);
            this._tokens.Setup(tokens => tokens.ValidateRefreshTokenAsync(user.Id, "previous-refresh-token")).ReturnsAsync(true);
            Assert.IsType<UnauthorizedObjectResult>((await this._authApi.RefreshToken(new RefreshTokenRequest
            {
                RefreshToken = "previous-refresh-token",
            })).Result);

            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, password: "weak")));
            Assert.True(user.PasswordResetRequired);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id, "wrong@example.com")));
            Assert.True(user.PasswordResetRequired);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id, password: "ChangedPassword123")));
            Assert.False(user.PasswordResetRequired);
            Assert.IsType<OkObjectResult>((await this._authApi.Login(new LoginRequest
            {
                Email = user.Email!, Password = "ChangedPassword123",
            })).Result);
            Assert.False(await this._users.CheckPasswordAsync(user, "MyNewPassword123"));
        }

        [Fact]
        /// <summary>
        /// Executes resend Invitation Invalidates Previous Links And Keeps Lock After Expiry.
        /// </summary>
        public async Task ResendInvitation_InvalidatesPreviousLinksAndKeepsLockAfterExpiry()
        {
            (ApplicationUser? user, Guid oldId) = await this.CreateInvitedUser();
            Assert.IsType<OkObjectResult>(await this._usersApi.ResendInvitation(user.Id));
            Guid id = this._store.Entries.Keys.Single(key => key != oldId);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(oldId)));
            Assert.True(user.PasswordResetRequired);
            this._clock.Now = this._clock.Now.AddMinutes(60);
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(id)));
            Assert.True(user.PasswordResetRequired);

            Assert.IsType<OkObjectResult>(await this._usersApi.ResendInvitation(user.Id));
            Guid freshId = this._store.Entries.Keys.Single(key => key != oldId && key != id);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(freshId)));
            Assert.False(user.PasswordResetRequired);
        }

        [Fact]
        /// <summary>
        /// Executes resend Invitation Revokes Existing Access Tokens Including After Unlock.
        /// </summary>
        public async Task ResendInvitation_RevokesExistingAccessTokensIncludingAfterUnlock()
        {
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            string jwt = await this._realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
            ClaimsPrincipal principal = Assert.IsType<ClaimsPrincipal>(this._realTokens.GetPrincipalFromExpiredToken(jwt));
            Assert.True(await this._realTokens.IsAccessTokenValidAsync(principal));
            Assert.False(await this._realTokens.IsAccessTokenValidAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.Id)]))));

            await this._usersApi.ResendInvitation(user.Id);
            Assert.False(await this._realTokens.IsAccessTokenValidAsync(principal));
            // Even a token with the current stamp cannot bypass the persisted invitation lock.
            string pendingJwt = await this._realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
            ClaimsPrincipal pendingPrincipal = this._realTokens.GetPrincipalFromExpiredToken(pendingJwt)!;
            Assert.False(await this._realTokens.IsAccessTokenValidAsync(pendingPrincipal));
            Guid newId = Assert.Single(this._store.Entries).Key;
            await this._authApi.ResetPassword(Request(newId));
            Assert.False(await this._realTokens.IsAccessTokenValidAsync(principal));
            Assert.False(await this._realTokens.IsAccessTokenValidAsync(pendingPrincipal));
            string newJwt = await this._realTokens.GenerateAccessTokenAsync(user, ["Viewer"]);
            Assert.True(await this._realTokens.IsAccessTokenValidAsync(this._realTokens.GetPrincipalFromExpiredToken(newJwt)!));
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
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            if (!alreadyInvited)
            {
                await this._authApi.ResetPassword(Request(id));
            }

            string? stamp = user.SecurityStamp;
            this._mail.Fail = true;
            Assert.Equal(503, Assert.IsType<ObjectResult>(await this._usersApi.ResendInvitation(user.Id)).StatusCode);
            this._provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
            user = (await this._users.FindByIdAsync(user.Id))!;
            Assert.Equal(alreadyInvited, user.PasswordResetRequired);
            Assert.Equal(stamp, user.SecurityStamp);
            if (alreadyInvited)
            {
                Assert.Equal(id, Assert.Single(this._store.Entries).Key);
                Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(id)));
            }
            else
            {
                Assert.Empty(this._store.Entries);
                Assert.IsType<OkObjectResult>((await this._authApi.Login(new LoginRequest
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
            (ApplicationUser? user, Guid id) = await this.CreateInvitedUser();
            await this._authApi.ResetPassword(Request(id));
            for (int attempt = 0; attempt < 5; attempt++)
            {
                await this._authApi.Login(new LoginRequest { Email = user.Email!, Password = "WrongPassword123" });
            }

            Assert.True(await this._users.IsLockedOutAsync(user));
            await this._usersApi.ResendInvitation(user.Id);
            Guid newId = Assert.Single(this._store.Entries).Key;
            Assert.IsType<BadRequestObjectResult>(await this._authApi.ResetPassword(Request(newId, password: "weak")));
            Assert.True(await this._users.IsLockedOutAsync(user));
            Assert.True(user.PasswordResetRequired);
            Assert.IsType<OkObjectResult>(await this._authApi.ResetPassword(Request(newId)));
            this._provider.GetRequiredService<FeuerwehrDbContext>().ChangeTracker.Clear();
            user = (await this._users.FindByIdAsync(user.Id))!;
            Assert.False(user.PasswordResetRequired);
            Assert.False(await this._users.IsLockedOutAsync(user));
            Assert.Equal(0, await this._users.GetAccessFailedCountAsync(user));
            Assert.IsType<OkObjectResult>((await this._authApi.Login(new LoginRequest
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
            Assert.IsType<NotFoundObjectResult>(await this._usersApi.ResendInvitation("missing"));
            (ApplicationUser? user, Guid _) = await this.CreateInvitedUser();
            user.IsActive = false;
            await this._users.UpdateAsync(user);
            Assert.IsType<BadRequestObjectResult>(await this._usersApi.ResendInvitation(user.Id));
            Assert.Single(this._mail.Messages);
        }

        private async Task<(ApplicationUser User, Guid Id)> CreateInvitedUser()
        {
            if (!await this._roles.RoleExistsAsync("Viewer"))
            {
                Assert.True((await this._roles.CreateAsync(new IdentityRole("Viewer"))).Succeeded);
            }

            IActionResult response = await this._usersApi.CreateUser(new RegisterUserRequest
            {
                Email = "new.user@example.com", FirstName = "Ada", LastName = "Muster", Roles = ["Viewer"],
            });
            Assert.IsType<CreatedAtActionResult>(response);
            ApplicationUser? user = await this._users.FindByEmailAsync("new.user@example.com");
            Assert.NotNull(user);
            return (user, Assert.Single(this._store.Entries).Key);
        }

        private static ResetRequest Request(Guid id, string email = "new.user@example.com", string password = "MyNewPassword123") =>
            new() { Token = id.ToString("D"), Email = email, NewPassword = password };

        private void SignInAs(string userId)
        {
            this._authApi.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)], "test")),
                },
            };
        }

        private sealed class MutableClock : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
            /// <summary>
            /// Gets the utc Now.
            /// </summary>
            public override DateTimeOffset GetUtcNow() => this.Now;
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
                this.Entries[id] = invitation;
                this.StoredLifetime = lifetime;
                return Task.CompletedTask;
            }
            /// <summary>
            /// Executes find.
            /// </summary>
            public Task<UserInvitation?> FindAsync(Guid id) => Task.FromResult(this.Entries.GetValueOrDefault(id));
            /// <summary>
            /// Removes the operation.
            /// </summary>
            /// <param name="id">The id.</param>
            public Task RemoveAsync(Guid id)
            {
                this.BeforeRemove?.Invoke();
                if (this.FailRemoval)
                {
                    throw new TimeoutException("Redis unavailable");
                }

                this.Entries.Remove(id);
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
                if (this.Fail)
                {
                    throw new System.Net.Mail.SmtpException("SMTP unavailable");
                }

                this.Messages.Add((recipient, subject, body));
                return Task.CompletedTask;
            }
        }
    }
}
