using System.Reflection;
using System.Security.Claims;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Controller;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller
{
    public class HydrantImportControllerTests
    {
        [Theory]
        [InlineData("Import", "Admin", true)]
        [InlineData("Import", "Commander", false)]
        [InlineData("Import", "Firefighter", false)]
        [InlineData("Import", "Viewer", false)]
        [InlineData("Import", null, false)]
        [InlineData("Export", "Admin", true)]
        [InlineData("Export", "Commander", false)]
        [InlineData("Export", "Firefighter", false)]
        [InlineData("Export", "Viewer", false)]
        [InlineData("Export", null, false)]
        /// <summary>
        /// Executes csv Policy Only Allows Admins.
        /// </summary>
        /// <param name="methodName">The method Name.</param>
        /// <param name="role">The role.</param>
        /// <param name="allowed">The allowed.</param>
        public async Task CsvPolicy_OnlyAllowsAdmins(string methodName, string? role, bool allowed)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(options => options.AddFeuerwehrPolicies());
            using ServiceProvider provider = services.BuildServiceProvider();
            MethodInfo method = typeof(HydrantController).GetMethod(methodName)!;
            IEnumerable<AuthorizeAttribute> attributes = typeof(HydrantController).GetCustomAttributes<AuthorizeAttribute>()
                .Concat(method.GetCustomAttributes<AuthorizeAttribute>());
            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(), attributes);
            Assert.NotNull(policy);
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            ClaimsPrincipal principal = new ClaimsPrincipal(role is null ? new ClaimsIdentity() :
                new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

            AuthorizationResult result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, policy);

            Assert.Equal(allowed, result.Succeeded);
        }

        [Fact]
        /// <summary>
        /// Executes export Returns Csv Download And Passes Cancellation.
        /// </summary>
        public async Task Export_ReturnsCsvDownloadAndPassesCancellation()
        {
            Mock<IHydrantService> service = new Mock<IHydrantService>();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            byte[] content = [0xef, 0xbb, 0xbf];
            service.Setup(s => s.ExportAsync(cancellation.Token)).ReturnsAsync(content);

            FileContentResult result = Assert.IsType<FileContentResult>(await new HydrantController(service.Object).Export(cancellation.Token));

            Assert.Equal("hydranten.csv", result.FileDownloadName);
            Assert.Equal("text/csv; charset=utf-8", result.ContentType);
            Assert.Equal(content, result.FileContents);
            service.Verify(s => s.ExportAsync(cancellation.Token), Times.Once);
        }

        [Fact]
        /// <summary>
        /// Executes missing Empty Or Oversized File Does Not Call The Service.
        /// </summary>
        public async Task MissingEmptyOrOversizedFile_DoesNotCallTheService()
        {
            Mock<IHydrantService> service = new Mock<IHydrantService>(MockBehavior.Strict);
            HydrantController controller = new HydrantController(service.Object);
            Assert.IsType<BadRequestObjectResult>(await controller.Import(null, default));
            using MemoryStream stream = new MemoryStream();
            FormFile empty = new FormFile(stream, 0, 0, "file", "empty.csv");
            Assert.IsType<BadRequestObjectResult>(await controller.Import(empty, default));
            FormFile oversized = new FormFile(stream, 0, HydrantController.MaximumImportBytes + 1, "file", "large.csv");
            Assert.IsType<BadRequestObjectResult>(await controller.Import(oversized, default));
            service.VerifyNoOtherCalls();
        }

        [Fact]
        /// <summary>
        /// Executes file Stream And Cancellation Are Passed To The Service.
        /// </summary>
        public async Task FileStreamAndCancellation_ArePassedToTheService()
        {
            Mock<IHydrantService> service = new Mock<IHydrantService>();
            using MemoryStream stream = new MemoryStream([1, 2, 3]);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            service.Setup(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token))
                .ReturnsAsync(ServiceResult.Ok(new HydrantImportResponse(2, [])));

            OkObjectResult result = Assert.IsType<OkObjectResult>(await new HydrantController(service.Object)
                .Import(new FormFile(stream, 0, stream.Length, "file", "data.csv"), cancellation.Token));

            Assert.Equal(2, Assert.IsType<HydrantImportResponse>(result.Value).ImportedCount);
            service.Verify(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token), Times.Once);
        }
    }

}
