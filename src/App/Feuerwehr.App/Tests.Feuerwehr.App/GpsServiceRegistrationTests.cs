using de.openelp.feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ClientApp = de.openelp.feuerwehr.App.App;

namespace de.openelp.feuerwehr.Tests.App;

public class GpsServiceRegistrationTests
{
    [Fact]
    public async Task DefaultRegistration_DoesNotInventALocation()
    {
        var app = new ClientApp();
        app.ReloadServices();

        var service = ClientApp.Services.GetRequiredService<IGpsService>();

        Assert.IsType<UnsupportedGpsService>(service);
        Assert.Null(await service.GetCurrentLocationAsync());
    }

    [Fact]
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
    public async Task UnsupportedService_RespectsCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UnsupportedGpsService().GetCurrentLocationAsync(cancellation.Token));
    }
}
