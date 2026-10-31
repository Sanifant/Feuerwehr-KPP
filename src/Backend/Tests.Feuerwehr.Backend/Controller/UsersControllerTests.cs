using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Server.Services;
using global::De.Openelp.Feuerwehr.Common.Models.Auth;
using global::De.Openelp.Feuerwehr.Server.Controller;
using global::De.Openelp.Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller
{
    public class UsersControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<IUserInvitationService> _mockInvitations;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                new Mock<IRoleStore<IdentityRole>>().Object, null, null, null, null);
            _mockInvitations = new Mock<IUserInvitationService>();

            _controller = new UsersController(new UsersService(_mockUserManager.Object, _mockRoleManager.Object, _mockInvitations.Object, NullLogger<UsersService>.Instance));
        }

        #region GetAllUsers Tests
        /*
        [Fact]
        /// <summary>
        /// Gets the all Users Returns Ok Result With List Of Users.
        /// </summary>
        public async Task GetAllUsers_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            ApplicationUser appUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            IQueryable<ApplicationUser> userList = new List<ApplicationUser> { appUser }.AsQueryable();
            _mockUserManager.Setup(um => um.Users).Returns(userList);
            _mockUserManager.Setup(um => um.FindByIdAsync("user-1")).ReturnsAsync(appUser);
            _mockUserManager.Setup(um => um.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            IActionResult result = await _controller.GetAllUsers();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<object> returnedUsers = Assert.IsType<List<object>>(okResult.Value);
            Assert.Single(returnedUsers);
        }

        [Fact]
        /// <summary>
        /// Gets the all Users Returns Empty List When No Users Exist.
        /// </summary>
        public async Task GetAllUsers_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            IQueryable<ApplicationUser> emptyUserList = new List<ApplicationUser>().AsQueryable();
            _mockUserManager.Setup(um => um.Users).Returns(emptyUserList);

            // Act
            IActionResult result = await _controller.GetAllUsers();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<object> returnedUsers = Assert.IsType<List<object>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }
        */
        #endregion

        #region GetUser Tests

        [Fact]
        /// <summary>
        /// Gets the user Returns Ok Result With Valid User Id.
        /// </summary>
        public async Task GetUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser appUser = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(appUser);
            _mockUserManager.Setup(um => um.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            IActionResult result = await _controller.GetUser(userId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        /// <summary>
        /// Updates the user Updates Email And Username.
        /// </summary>
        public async Task UpdateUser_UpdatesEmailAndUsername()
        {
            ApplicationUser user = new ApplicationUser { Id = "user-1", Email = "old@example.com" };
            UpdateUserRequest request = new UpdateUserRequest { Email = "new@example.com" };
            _mockUserManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email!)).ReturnsAsync((ApplicationUser)null!);
            _mockUserManager.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            IActionResult result = await _controller.UpdateUser(user.Id, request);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(request.Email, user.Email);
            Assert.Equal(request.Email, user.UserName);
        }

        [Fact]
        /// <summary>
        /// Updates the user Clears Roles When Empty Role List Provided.
        /// </summary>
        public async Task UpdateUser_ClearsRoles_WhenEmptyRoleListProvided()
        {
            ApplicationUser user = new ApplicationUser { Id = "user-1" };
            List<string> currentRoles = new List<string> { "Admin" };
            UpdateUserRequest request = new UpdateUserRequest { Roles = new List<string>() };
            _mockUserManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.SetupSequence(um => um.GetRolesAsync(user))
                .ReturnsAsync(currentRoles)
                .ReturnsAsync(new List<string>());
            _mockUserManager.Setup(um => um.RemoveFromRolesAsync(user, currentRoles)).ReturnsAsync(IdentityResult.Success);

            IActionResult result = await _controller.UpdateUser(user.Id, request);

            Assert.IsType<OkObjectResult>(result);
            _mockUserManager.Verify(um => um.RemoveFromRolesAsync(user, currentRoles), Times.Once);
            _mockUserManager.Verify(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Theory]
        [InlineData(null, false, 1)]
        [InlineData(2, false, 2)]
        [InlineData(null, true, null)]
        /// <summary>
        /// Updates the user Preserves Changes Or Clears Fire Department Explicitly.
        /// </summary>
        /// <param name="departmentId">The department Id.</param>
        /// <param name="clear">The clear.</param>
        /// <param name="expected">The expected.</param>
        public async Task UpdateUser_PreservesChangesOrClearsFireDepartmentExplicitly(int? departmentId, bool clear, int? expected)
        {
            ApplicationUser user = new ApplicationUser { Id = "user-1", FireDepartmentId = 1 };
            _mockUserManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(manager => manager.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            IActionResult result = await _controller.UpdateUser(user.Id, new UpdateUserRequest { FireDepartmentId = departmentId, ClearFireDepartment = clear });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, user.FireDepartmentId);
        }

        [Fact]
        /// <summary>
        /// Updates the user Rejects Conflicting Department Changes.
        /// </summary>
        public async Task UpdateUser_RejectsConflictingDepartmentChanges()
        {
            ApplicationUser user = new ApplicationUser { Id = "user-1", FireDepartmentId = 1 };
            _mockUserManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);

            IActionResult result = await _controller.UpdateUser(user.Id, new UpdateUserRequest { FireDepartmentId = 2, ClearFireDepartment = true });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(1, user.FireDepartmentId);
            _mockUserManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        /// <summary>
        /// Updates the user Returns Bad Request When Email Belongs To Another User.
        /// </summary>
        public async Task UpdateUser_ReturnsBadRequest_WhenEmailBelongsToAnotherUser()
        {
            ApplicationUser user = new ApplicationUser { Id = "user-1", Email = "old@example.com" };
            ApplicationUser existingUser = new ApplicationUser { Id = "user-2", Email = "taken@example.com" };
            UpdateUserRequest request = new UpdateUserRequest { Email = existingUser.Email };
            _mockUserManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email!)).ReturnsAsync(existingUser);

            IActionResult result = await _controller.UpdateUser(user.Id, request);

            Assert.IsType<BadRequestObjectResult>(result);
            _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        /// <summary>
        /// Gets the user Returns Not Found When User Does Not Exist.
        /// </summary>
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            IActionResult result = await _controller.GetUser(userId);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        #endregion

        #region CreateUser Tests
        /*
        [Fact]
        /// <summary>
        /// Creates the user Returns Created At Action With Valid Request.
        /// </summary>
        public async Task CreateUser_ReturnsCreatedAtAction_WithValidRequest()
        {
            // Arrange
            RegisterUserRequest request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",

                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            ApplicationUser createdUser = new ApplicationUser
            {
                Id = "new-user-1",
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FireDepartmentId = request.FireDepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("User")).ReturnsAsync(true);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(user => user.Id = "new-user-1")
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(request.Roles);

            // Act
            IActionResult result = await _controller.CreateUser(request);

            // Assert
            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetUser), createdResult.ActionName);
            _mockInvitations.Verify(service => service.SendAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }
        */
        [Fact]
        /// <summary>
        /// Creates the user Returns Bad Request When User Email Already Exists.
        /// </summary>
        public async Task CreateUser_ReturnsBadRequest_WhenUserEmailAlreadyExists()
        {
            // Arrange
            RegisterUserRequest request = new RegisterUserRequest
            {
                Email = "existing@example.com",
                FirstName = "Jane",
                LastName = "Smith",

                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            ApplicationUser existingUser = new ApplicationUser { Email = request.Email };
            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync(existingUser);

            // Act
            IActionResult result = await _controller.CreateUser(request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        /// <summary>
        /// Creates the user Returns Bad Request When Role Does Not Exist.
        /// </summary>
        public async Task CreateUser_ReturnsBadRequest_WhenRoleDoesNotExist()
        {
            // Arrange
            RegisterUserRequest request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",

                FireDepartmentId = 1,
                Roles = new List<string> { "InvalidRole" }
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("InvalidRole")).ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.CreateUser(request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        /// <summary>
        /// Creates the user Rolls Back User Creation When Role Assignment Fails.
        /// </summary>
        public async Task CreateUser_RollsBackUserCreation_WhenRoleAssignmentFails()
        {
            // Arrange
            RegisterUserRequest request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",

                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            ApplicationUser createdUser = new ApplicationUser
            {
                Id = "new-user-1",
                Email = request.Email
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("User")).ReturnsAsync(true);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(user => user.Id = "new-user-1")
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            IActionResult result = await _controller.CreateUser(request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [Fact]
        /// <summary>
        /// Creates the user Returns Bad Request When User Creation Fails.
        /// </summary>
        public async Task CreateUser_ReturnsBadRequest_WhenUserCreationFails()
        {
            // Arrange
            RegisterUserRequest request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",

                FireDepartmentId = 1,
                Roles = new List<string>()
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password not strong enough" }));

            // Act
            IActionResult result = await _controller.CreateUser(request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        /// <summary>
        /// Updates the user Returns Ok Result With Valid Request.
        /// </summary>
        public async Task UpdateUser_ReturnsOkResult_WithValidRequest()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser existingUser = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true
            };

            UpdateUserRequest request = new UpdateUserRequest
            {
                FirstName = "Johnny",
                LastName = "Smith",
                FireDepartmentId = 2,
                IsActive = true,
                Roles = new List<string> { "Admin" }
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("Admin")).ReturnsAsync(true);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles!))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        /// <summary>
        /// Updates the user Returns Not Found When User Does Not Exist.
        /// </summary>
        public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "non-existent";
            UpdateUserRequest request = new UpdateUserRequest { FirstName = "Jane" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        /// <summary>
        /// Updates the user Updates Only Provided Fields.
        /// </summary>
        public async Task UpdateUser_UpdatesOnlyProvidedFields()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser existingUser = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true
            };

            UpdateUserRequest request = new UpdateUserRequest { FirstName = "Johnny" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser)).ReturnsAsync(new List<string>());

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        /// <summary>
        /// Updates the user Updates Password When New Password Provided.
        /// </summary>
        public async Task UpdateUser_UpdatesPassword_WhenNewPasswordProvided()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser existingUser = new ApplicationUser { Id = userId };
            UpdateUserRequest request = new UpdateUserRequest { NewPassword = "NewSecurePassword123!" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GeneratePasswordResetTokenAsync(existingUser)).ReturnsAsync("reset-token");
            _mockUserManager.Setup(um => um.ResetPasswordAsync(existingUser, "reset-token", request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser)).ReturnsAsync(new List<string>());

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            _mockUserManager.Verify(um => um.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), request.NewPassword), Times.Once);
        }

        [Fact]
        /// <summary>
        /// Updates the user Returns Bad Request When Update Fails.
        /// </summary>
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser existingUser = new ApplicationUser { Id = userId };
            UpdateUserRequest request = new UpdateUserRequest { FirstName = "Jane" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }
        [Fact]
        /// <summary>
        /// Updates the user Returns Bad Request When Invalid Role Provided.
        /// </summary>
        public async Task UpdateUser_ReturnsBadRequest_WhenInvalidRoleProvided()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser existingUser = new ApplicationUser { Id = userId };
            UpdateUserRequest request = new UpdateUserRequest { Roles = new List<string> { "InvalidRole" } };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("InvalidRole")).ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.UpdateUser(userId, request);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
        #endregion

        #region DeleteUser Tests

        [Fact]
        /// <summary>
        /// Deletes the user Returns Ok Result With Valid User Id.
        /// </summary>
        public async Task DeleteUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId, IsActive = true };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            UsersController controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            IActionResult result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.False(user.IsActive);
        }

        [Fact]
        /// <summary>
        /// Deletes the user Returns Not Found When User Does Not Exist.
        /// </summary>
        public async Task DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            IActionResult result = await _controller.DeleteUser(userId);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        /// <summary>
        /// Deletes the user Returns Bad Request When Trying To Delete Own Account.
        /// </summary>
        public async Task DeleteUser_ReturnsBadRequest_WhenTryingToDeleteOwnAccount()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

            UsersController controllerWithClaims = SetupControllerWithClaims(userId);

            // Act
            IActionResult result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        /// <summary>
        /// Deletes the user Returns Bad Request When Update Fails.
        /// </summary>
        public async Task DeleteUser_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            UsersController controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            IActionResult result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region PermanentlyDeleteUser Tests

        [Fact]
        /// <summary>
        /// Executes permanently Delete User Returns Ok Result With Valid User Id.
        /// </summary>
        public async Task PermanentlyDeleteUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            UsersController controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            IActionResult result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        /// <summary>
        /// Executes permanently Delete User Returns Not Found When User Does Not Exist.
        /// </summary>
        public async Task PermanentlyDeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            IActionResult result = await _controller.PermanentlyDeleteUser(userId);

            // Assert
            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        /// <summary>
        /// Executes permanently Delete User Returns Bad Request When Trying To Delete Own Account.
        /// </summary>
        public async Task PermanentlyDeleteUser_ReturnsBadRequest_WhenTryingToDeleteOwnAccount()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

            UsersController controllerWithClaims = SetupControllerWithClaims(userId);

            // Act
            IActionResult result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        /// <summary>
        /// Executes permanently Delete User Returns Bad Request When Delete Fails.
        /// </summary>
        public async Task PermanentlyDeleteUser_ReturnsBadRequest_WhenDeleteFails()
        {
            // Arrange
            string userId = "user-1";
            ApplicationUser user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

            UsersController controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            IActionResult result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region Helper Methods

        private UsersController SetupControllerWithClaims(string userId)
        {
            UsersController controller = new UsersController(new UsersService(_mockUserManager.Object, _mockRoleManager.Object, _mockInvitations.Object, NullLogger<UsersService>.Instance));

            List<Claim> claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
            };

            ClaimsIdentity identity = new System.Security.Claims.ClaimsIdentity(claims);
            ClaimsPrincipal principal = new System.Security.Claims.ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
            };

            return controller;
        }

        #endregion
    }
}

