using System.Reflection;
using System.Security.Claims;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Controller;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace de.openelp.feuerwehr.Tests.Backend.Controller;

public class HydrantImportControllerTests
{
    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Commander", false)]
    [InlineData("Firefighter", false)]
    [InlineData("Viewer", false)]
    [InlineData(null, false)]
    public async Task ImportPolicy_OnlyAllowsAdmins(string? role, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddFeuerwehrPolicies());
        using var provider = services.BuildServiceProvider();
        var method = typeof(HydrantController).GetMethod(nameof(HydrantController.Import))!;
        var attributes = typeof(HydrantController).GetCustomAttributes<AuthorizeAttribute>()
            .Concat(method.GetCustomAttributes<AuthorizeAttribute>());
        var policy = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(), attributes);
        Assert.NotNull(policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        var principal = new ClaimsPrincipal(role is null ? new ClaimsIdentity() :
            new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, policy);

        Assert.Equal(allowed, result.Succeeded);
    }

    [Fact]
    public async Task MissingEmptyOrOversizedFile_DoesNotCallTheService()
    {
        var service = new Mock<IHydrantService>(MockBehavior.Strict);
        var controller = new HydrantController(service.Object);
        Assert.IsType<BadRequestObjectResult>(await controller.Import(null, default));
        using var stream = new MemoryStream();
        var empty = new FormFile(stream, 0, 0, "file", "empty.csv");
        Assert.IsType<BadRequestObjectResult>(await controller.Import(empty, default));
        var oversized = new FormFile(stream, 0, HydrantController.MaximumImportBytes + 1, "file", "large.csv");
        Assert.IsType<BadRequestObjectResult>(await controller.Import(oversized, default));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FileStreamAndCancellation_ArePassedToTheService()
    {
        var service = new Mock<IHydrantService>();
        using var stream = new MemoryStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        service.Setup(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token))
            .ReturnsAsync(ServiceResult.Ok(new HydrantImportResponse(2, [])));

        var result = Assert.IsType<OkObjectResult>(await new HydrantController(service.Object)
            .Import(new FormFile(stream, 0, stream.Length, "file", "data.csv"), cancellation.Token));

        Assert.Equal(2, Assert.IsType<HydrantImportResponse>(result.Value).ImportedCount);
        service.Verify(s => s.ImportAsync(It.IsAny<Stream>(), cancellation.Token), Times.Once);
    }
}
