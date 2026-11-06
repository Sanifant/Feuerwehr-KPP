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
    /// <summary>
    /// Tests hydrant import controller behavior and its expected outcomes.
    /// </summary>
    public class HydrantImportControllerTests
    {
        /// <summary>
        /// Verifies that CSV policy only allows admins.
        /// </summary>
        /// <param name="methodName">The display name of the associated method.</param>
        /// <param name="role">The Identity role under test.</param>
        /// <param name="allowed">The expected allowed flag for this test case.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
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
        public async Task CsvPolicy_OnlyAllowsAdmins(string methodName, string? role, bool allowed)
        {
            ServiceCollection services = new ();
            _ = services.AddLogging();
            _ = services.AddAuthorization(options => options.AddFeuerwehrPolicies());
            using ServiceProvider provider = services.BuildServiceProvider();
            MethodInfo method = typeof(HydrantController).GetMethod(methodName)!;
            IEnumerable<AuthorizeAttribute> attributes = typeof(HydrantController).GetCustomAttributes<AuthorizeAttribute>()
                .Concat(method.GetCustomAttributes<AuthorizeAttribute>());
            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(), attributes);
            Assert.NotNull(policy);
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            ClaimsPrincipal principal = new (role is null ? new ClaimsIdentity() :
                new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

            AuthorizationResult result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, policy);

            Assert.Equal(allowed, result.Succeeded);
        }

        /// <summary>
        /// Verifies that export returns CSV download and passes cancellation.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Export_ReturnsCsvDownloadAndPassesCancellation()
        {
            Mock<IHydrantService> service = new ();
            using CancellationTokenSource cancellation = new ();
            byte[] content = [0xef, 0xbb, 0xbf];
            _ = service.Setup(s => s.ExportAsync(cancellation.Token)).ReturnsAsync(content);

            FileContentResult result = Assert.IsType<FileContentResult>(await new HydrantController(service.Object).Export(cancellation.Token));

            Assert.Equal("hydranten.csv", result.FileDownloadName);
            Assert.Equal("text/csv; charset=utf-8", result.ContentType);
            Assert.Equal(content, result.FileContents);
            service.Verify(s => s.ExportAsync(cancellation.Token), Times.Once);
        }

        /// <summary>
        /// Verifies that missing empty or oversized file does not call the service.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task MissingEmptyOrOversizedFile_DoesNotCallTheService()
        {
            Mock<IHydrantService> service = new (MockBehavior.Strict);
            HydrantController controller = new (service.Object);
            _ = Assert.IsType<BadRequestObjectResult>(await controller.Import(null, default));
            using MemoryStream stream = new ();
            FormFile empty = new (stream, 0, 0, "file", "empty.csv");
            _ = Assert.IsType<BadRequestObjectResult>(await controller.Import(empty, default));
            FormFile oversized = new (stream, 0, HydrantController.MaximumImportBytes + 1, "file", "large.csv");
            _ = Assert.IsType<BadRequestObjectResult>(await controller.Import(oversized, default));
            service.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that file stream and cancellation are passed to the service.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task FileStreamAndCancellation_ArePassedToTheService()
        {
            Mock<IHydrantService> service = new ();
            using MemoryStream stream = new ([1, 2, 3]);
            using CancellationTokenSource cancellation = new ();
            _ = service.Setup(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token))
                .ReturnsAsync(ServiceResult.Ok(new HydrantImportResponse(2, [])));

            OkObjectResult result = Assert.IsType<OkObjectResult>(await new HydrantController(service.Object)
                .Import(new FormFile(stream, 0, stream.Length, "file", "data.csv"), cancellation.Token));

            Assert.Equal(2, Assert.IsType<HydrantImportResponse>(result.Value).ImportedCount);
            service.Verify(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token), Times.Once);
        }
    }
}
