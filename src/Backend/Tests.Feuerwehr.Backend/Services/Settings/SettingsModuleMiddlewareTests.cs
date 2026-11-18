namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using System.Security.Claims;
    using System.Text.Json;
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

    public sealed class SettingsModuleMiddlewareTests
    {
        [Theory]
        [InlineData("/api/test-module")]
        [InlineData("/api/test-module/42")]
        [InlineData("/API/TEST-MODULE/export")]
        public async Task BlocksEveryDeclaredModuleEndpointWhenMandatoryConfigurationIsMissingAsync(string path)
        {
            await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
            DefaultHttpContext context = Context(path, services);
            bool nextCalled = false;
            SettingsModuleMiddleware middleware = new SettingsModuleMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            Mock<ISettingsRuntime> runtime = Runtime(false);
            await middleware.InvokeAsync(context, new SettingsCatalog([typeof(TestSettingsModule)]), runtime.Object);
            Assert.False(nextCalled);
            Assert.Equal(503, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal("test", response.RootElement.GetProperty("moduleId").GetString());
            Assert.Equal("Benachrichtigung", response.RootElement.GetProperty("missingParameters")[0].GetString());
        }

        [Theory]
        [InlineData("/api/test-module", true)]
        [InlineData("/api/test-module-other", false)]
        [InlineData("/api/Settings", false)]
        [InlineData("/api/Settings/modules", false)]
        public async Task AllowsConfiguredModulesUnrelatedPathsAndSettingsAdministrationAsync(string path, bool available)
        {
            await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
            DefaultHttpContext context = Context(path, services);
            bool nextCalled = false;
            SettingsModuleMiddleware middleware = new SettingsModuleMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            await middleware.InvokeAsync(context, new SettingsCatalog([typeof(TestSettingsModule)]), Runtime(available).Object);
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task LeavesUnauthenticatedRequestsToNormalAuthorizationAsync()
        {
            await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
            DefaultHttpContext context = Context("/api/test-module", services);
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            Mock<ISettingsRuntime> runtime = new Mock<ISettingsRuntime>(MockBehavior.Strict);
            bool nextCalled = false;
            SettingsModuleMiddleware middleware = new SettingsModuleMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            await middleware.InvokeAsync(context, new SettingsCatalog([typeof(TestSettingsModule)]), runtime.Object);
            Assert.True(nextCalled);
        }

        private static DefaultHttpContext Context(string path, IServiceProvider services)
        {
            DefaultHttpContext context = new DefaultHttpContext { RequestServices = services };
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test"));
            return context;
        }

        private static Mock<ISettingsRuntime> Runtime(bool available)
        {
            Mock<ISettingsRuntime> runtime = new Mock<ISettingsRuntime>();
            runtime.Setup(x => x.GetModuleStatusesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
                [new SettingsModuleStatusDto("test", "Testmodul", ["/test-module"], available, available ? [] : ["Benachrichtigung"])]);
            return runtime;
        }
    }
}
