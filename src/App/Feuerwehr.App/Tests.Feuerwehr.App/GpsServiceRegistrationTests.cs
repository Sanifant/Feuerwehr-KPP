using De.Openelp.Feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ClientApp = De.Openelp.Feuerwehr.App.App;

namespace De.Openelp.Feuerwehr.Tests.App;

public class GpsServiceRegistrationTests
{
    [Fact]
    /// <summary>
    /// Executes default Registration Does Not Invent A Location.
    /// </summary>
    public async Task DefaultRegistration_DoesNotInventALocation()
    {
        var app = new ClientApp();
        app.ReloadServices();

        var service = ClientApp.Services.GetRequiredService<IGpsService>();

        Assert.IsType<UnsupportedGpsService>(service);
        Assert.Null(await service.GetCurrentLocationAsync());
    }

    [Fact]
    /// <summary>
    /// Executes platform Registration Is Preserved When Services Are Reloaded.
    /// </summary>
    public void PlatformRegistration_IsPreservedWhenServicesAreReloaded()
    {
        var gps = Mock.Of<IGpsService>();
        var app = new ClientApp
        {
            ConfigurePlatformServices = services => services.Replace(ServiceDescriptor.Singleton(gps))
        };

        app.ReloadServices();
        Assert.Same(gps, ClientApp.Services.GetRequiredService<IGpsService>());

        app.ReloadServices();
        Assert.Same(gps, Assert.Single(ClientApp.Services.GetServices<IGpsService>()));
    }

    [Fact]
    /// <summary>
    /// Executes unsupported Service Respects Caller Cancellation.
    /// </summary>
    public async Task UnsupportedService_RespectsCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UnsupportedGpsService().GetCurrentLocationAsync(cancellation.Token));
    }
}

