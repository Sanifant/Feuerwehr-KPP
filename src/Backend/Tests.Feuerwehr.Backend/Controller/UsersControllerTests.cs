using global::Feuerwehr.Server.Controller;
using global::Feuerwehr.Server.Models;
using global::Feuerwehr.Server.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Feuerwehr.Backend.Controller
{
    public class UsersControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<SmtpClient> _mockSmtpClient;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                new Mock<IRoleStore<IdentityRole>>().Object, null, null, null, null);
            _mockSmtpClient = new Mock<SmtpClient>();

            _controller = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _mockSmtpClient.Object);
        }

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            var appUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var userList = new List<ApplicationUser> { appUser }.AsQueryable();
            _mockUserManager.Setup(um => um.Users).Returns(userList);
            _mockUserManager.Setup(um => um.FindByIdAsync("user-1")).ReturnsAsync(appUser);
            _mockUserManager.Setup(um => um.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<object>>(okResult.Value);
            Assert.Single(returnedUsers);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            var emptyUserList = new List<ApplicationUser>().AsQueryable();
            _mockUserManager.Setup(um => um.Users).Returns(emptyUserList);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<object>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }

        #endregion

        #region GetUser Tests

        [Fact]
        public async Task GetUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            var userId = "user-1";
            var appUser = new ApplicationUser
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
            var result = await _controller.GetUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_ReturnsCreatedAtAction_WithValidRequest()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Password = "SecurePassword123!",
                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            var createdUser = new ApplicationUser
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
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .Callback<ApplicationUser, string>((user, pwd) => user.Id = "new-user-1")
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(request.Roles);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetUser), createdResult.ActionName);
            _mockSmtpClient.Verify(sc => sc.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenUserEmailAlreadyExists()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "existing@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Password = "SecurePassword123!",
                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            var existingUser = new ApplicationUser { Email = request.Email };
            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync(existingUser);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenRoleDoesNotExist()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Password = "SecurePassword123!",
                FireDepartmentId = 1,
                Roles = new List<string> { "InvalidRole" }
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("InvalidRole")).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateUser_RollsBackUserCreation_WhenRoleAssignmentFails()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Password = "SecurePassword123!",
                FireDepartmentId = 1,
                Roles = new List<string> { "User" }
            };

            var createdUser = new ApplicationUser
            {
                Id = "new-user-1",
                Email = request.Email
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("User")).ReturnsAsync(true);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .Callback<ApplicationUser, string>((user, pwd) => user.Id = "new-user-1")
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), request.Roles))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenUserCreationFails()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "newuser@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Password = "WeakPassword",
                FireDepartmentId = 1,
                Roles = new List<string>()
            };

            _mockUserManager.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null!);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password not strong enough" }));

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_ReturnsOkResult_WithValidRequest()
        {
            // Arrange
            var userId = "user-1";
            var existingUser = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true
            };

            var request = new UpdateUserRequest
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
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "non-existent";
            var request = new UpdateUserRequest { FirstName = "Jane" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUser_UpdatesOnlyProvidedFields()
        {
            // Arrange
            var userId = "user-1";
            var existingUser = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                FireDepartmentId = 1,
                IsActive = true
            };

            var request = new UpdateUserRequest { FirstName = "Johnny" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser)).ReturnsAsync(new List<string>());

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateUser_UpdatesPassword_WhenNewPasswordProvided()
        {
            // Arrange
            var userId = "user-1";
            var existingUser = new ApplicationUser { Id = userId };
            var request = new UpdateUserRequest { NewPassword = "NewSecurePassword123!" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GeneratePasswordResetTokenAsync(existingUser)).ReturnsAsync("reset-token");
            _mockUserManager.Setup(um => um.ResetPasswordAsync(existingUser, "reset-token", request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser)).ReturnsAsync(new List<string>());

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockUserManager.Verify(um => um.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), request.NewPassword), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var userId = "user-1";
            var existingUser = new ApplicationUser { Id = userId };
            var request = new UpdateUserRequest { FirstName = "Jane" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenInvalidRoleProvided()
        {
            // Arrange
            var userId = "user-1";
            var existingUser = new ApplicationUser { Id = userId };
            var request = new UpdateUserRequest { Roles = new List<string> { "InvalidRole" } };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync("InvalidRole")).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUser(userId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId, IsActive = true };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            var result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.False(user.IsActive);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteUser_ReturnsBadRequest_WhenTryingToDeleteOwnAccount()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

            var controllerWithClaims = SetupControllerWithClaims(userId);

            // Act
            var result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteUser_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            var controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            var result = await controllerWithClaims.DeleteUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region PermanentlyDeleteUser Tests

        [Fact]
        public async Task PermanentlyDeleteUser_ReturnsOkResult_WithValidUserId()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            var result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task PermanentlyDeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "non-existent";
            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.PermanentlyDeleteUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task PermanentlyDeleteUser_ReturnsBadRequest_WhenTryingToDeleteOwnAccount()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

            var controllerWithClaims = SetupControllerWithClaims(userId);

            // Act
            var result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task PermanentlyDeleteUser_ReturnsBadRequest_WhenDeleteFails()
        {
            // Arrange
            var userId = "user-1";
            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

            var controllerWithClaims = SetupControllerWithClaims("different-user");

            // Act
            var result = await controllerWithClaims.PermanentlyDeleteUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region Helper Methods

        private UsersController SetupControllerWithClaims(string userId)
        {
            var controller = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _mockSmtpClient.Object);

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
            };

            return controller;
        }

        #endregion
    }
}