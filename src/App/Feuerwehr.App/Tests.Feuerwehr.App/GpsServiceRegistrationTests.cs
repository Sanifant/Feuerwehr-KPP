using De.Openelp.Feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ClientApp = De.Openelp.Feuerwehr.App.App;

namespace De.Openelp.Feuerwehr.Tests.App
{
    /// <summary>
    /// Tests GPS service registration behavior and its expected outcomes.
    /// </summary>
    public class GpsServiceRegistrationTests
    {
        /// <summary>
        /// Verifies that default registration does not invent a location.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task DefaultRegistration_DoesNotInventALocation()
        {
            ClientApp app = new ();
            app.ReloadServices();

            IGpsService service = ClientApp.Services.GetRequiredService<IGpsService>();

            _ = Assert.IsType<UnsupportedGpsService>(service);
            Assert.Null(await service.GetCurrentLocationAsync());
        }

        /// <summary>
        /// Verifies that platform registration is preserved when services are reloaded.
        /// </summary>
        [Fact]
        public void PlatformRegistration_IsPreservedWhenServicesAreReloaded()
        {
            IGpsService gps = Mock.Of<IGpsService>();
            ClientApp app = new ()
            {
                ConfigurePlatformServices = services => services.Replace(ServiceDescriptor.Singleton(gps))
            };

            app.ReloadServices();
            Assert.Same(gps, ClientApp.Services.GetRequiredService<IGpsService>());

            app.ReloadServices();
            Assert.Same(gps, Assert.Single(ClientApp.Services.GetServices<IGpsService>()));
        }

        /// <summary>
        /// Verifies that unsupported service respects caller cancellation.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task UnsupportedService_RespectsCallerCancellation()
        {
            using CancellationTokenSource cancellation = new ();
            cancellation.Cancel();

            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new UnsupportedGpsService().GetCurrentLocationAsync(cancellation.Token));
        }
    }
}
