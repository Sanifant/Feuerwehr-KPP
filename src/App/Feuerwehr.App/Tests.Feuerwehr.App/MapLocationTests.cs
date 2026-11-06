using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.Common.Models;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.App
{
    /// <summary>
    /// Tests map location behavior and its expected outcomes.
    /// </summary>
    public class MapLocationTests
    {
        /// <summary>
        /// Verifies that constructing map does not request location before the view is loaded.
        /// </summary>
        [Fact]
        public void ConstructingMap_DoesNotRequestLocationBeforeTheViewIsLoaded()
        {
            Mock<IGpsService> gps = new (MockBehavior.Strict);

            MapViewModel viewModel = CreateViewModel(gps.Object);

            Assert.NotNull(viewModel.Map);
            gps.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Verifies that settings waits asynchronously and uses the returned coordinates.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Settings_WaitsAsynchronouslyAndUsesTheReturnedCoordinates()
        {
            TaskCompletionSource<GpsLocation?> completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<IGpsService> gps = new ();
            using CancellationTokenSource cancellation = new ();
            _ = gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
            MapViewModel viewModel = CreateViewModel(gps.Object);

            Task pending = viewModel.SettingsAsync(cancellation.Token);

            Assert.False(pending.IsCompleted);
            Assert.False(viewModel.IsAddVisible);
            completion.SetResult(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });
            await pending;

            Assert.True(viewModel.IsAddVisible);
            Assert.Equal(53.9, viewModel.NewHydrant.Latitude);
            Assert.Equal(10.6, viewModel.NewHydrant.Longitude);
        }

        /// <summary>
        /// Verifies that unavailable location does not create a pin or open hydrant creation.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task UnavailableLocation_DoesNotCreateAPinOrOpenHydrantCreation()
        {
            Mock<IGpsService> gps = new ();
            _ = gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((GpsLocation?)null);
            MapViewModel viewModel = CreateViewModel(gps.Object);

            await viewModel.InitializeLocationAsync();
            await viewModel.CenterOnLocationAsync(CancellationToken.None);
            await viewModel.SettingsAsync(CancellationToken.None);

            Assert.DoesNotContain(viewModel.Map!.Layers, layer => layer.Name == "Pins");
            Assert.False(viewModel.IsAddVisible);
        }

        /// <summary>
        /// Verifies that initialization replaces existing location pin.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Initialization_ReplacesExistingLocationPin()
        {
            Mock<IGpsService> gps = new ();
            _ = gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });
            MapViewModel viewModel = CreateViewModel(gps.Object);

            await viewModel.InitializeLocationAsync();
            await viewModel.InitializeLocationAsync();

            _ = Assert.Single(viewModel.Map!.Layers, layer => layer.Name == "Pins");
        }

        /// <summary>
        /// Verifies that cancellation prevents late location results from changing the view.
        /// </summary>
        /// <param name="initialize">The expected initialize flag for this test case.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Cancellation_PreventsLateLocationResultsFromChangingTheView(bool initialize)
        {
            TaskCompletionSource<GpsLocation?> completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<IGpsService> gps = new ();
            using CancellationTokenSource cancellation = new ();
            _ = gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
            MapViewModel viewModel = CreateViewModel(gps.Object);
            Task pending = initialize
                ? viewModel.InitializeLocationAsync(cancellation.Token)
                : viewModel.SettingsAsync(cancellation.Token);

            cancellation.Cancel();
            completion.SetResult(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });

            if (initialize)
            {
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            }
            else
            {
                await pending;
            }

            Assert.DoesNotContain(viewModel.Map!.Layers, layer => layer.Name == "Pins");
            Assert.False(viewModel.IsAddVisible);
        }

        /// <summary>
        /// Verifies that canceling bound commands completes without an unhandled cancellation.
        /// </summary>
        /// <param name="center">The expected center flag for this test case.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CancelingBoundCommands_CompletesWithoutAnUnhandledCancellation(bool center)
        {
            Mock<IGpsService> gps = new ();
            TaskCompletionSource requested = new (TaskCreationOptions.RunContinuationsAsynchronously);
            _ = gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken token) =>
                {
                    requested.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return null;
                });
            MapViewModel viewModel = CreateViewModel(gps.Object);
            IAsyncRelayCommand command = center ? viewModel.CenterOnLocationCommand : viewModel.SettingsCommand;

            Task pending = command.ExecuteAsync(null);
            await requested.Task;
            command.Cancel();
            await pending;

            Assert.True(pending.IsCompletedSuccessfully);
            Assert.False(viewModel.IsAddVisible);
        }

        private static MapViewModel CreateViewModel(IGpsService gps)
        {
            Mock<IHydrantService> hydrants = new ();
            _ = hydrants.Setup(service => service.GetAllAsync()).Returns(Task.FromCanceled<IEnumerable<Hydrant>>(new CancellationToken(true)));
            return new MapViewModel(hydrants.Object, gps);
        }
    }
}
