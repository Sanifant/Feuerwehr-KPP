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
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddFeuerwehrPolicies());
        using var provider = services.BuildServiceProvider();
        var method = typeof(HydrantController).GetMethod(methodName)!;
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
    public async Task Export_ReturnsCsvDownloadAndPassesCancellation()
    {
        var service = new Mock<IHydrantService>();
        using var cancellation = new CancellationTokenSource();
        byte[] content = [0xef, 0xbb, 0xbf];
        service.Setup(s => s.ExportAsync(cancellation.Token)).ReturnsAsync(content);

        var result = Assert.IsType<FileContentResult>(await new HydrantController(service.Object).Export(cancellation.Token));

        Assert.Equal("hydranten.csv", result.FileDownloadName);
        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.Equal(content, result.FileContents);
        service.Verify(s => s.ExportAsync(cancellation.Token), Times.Once);
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
