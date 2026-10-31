using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.Common.Models;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.App
{
    public class MapLocationTests
    {
        [Fact]
        /// <summary>
        /// Executes constructing Map Does Not Request Location Before The View Is Loaded.
        /// </summary>
        public void ConstructingMap_DoesNotRequestLocationBeforeTheViewIsLoaded()
        {
            Mock<IGpsService> gps = new Mock<IGpsService>(MockBehavior.Strict);

            MapViewModel viewModel = CreateViewModel(gps.Object);

            Assert.NotNull(viewModel.Map);
            gps.VerifyNoOtherCalls();
        }

        [Fact]
        /// <summary>
        /// Executes settings Waits Asynchronously And Uses The Returned Coordinates.
        /// </summary>
        public async Task Settings_WaitsAsynchronouslyAndUsesTheReturnedCoordinates()
        {
            TaskCompletionSource<GpsLocation?> completion = new TaskCompletionSource<GpsLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<IGpsService> gps = new Mock<IGpsService>();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
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

        [Fact]
        /// <summary>
        /// Executes unavailable Location Does Not Create A Pin Or Open Hydrant Creation.
        /// </summary>
        public async Task UnavailableLocation_DoesNotCreateAPinOrOpenHydrantCreation()
        {
            Mock<IGpsService> gps = new Mock<IGpsService>();
            gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((GpsLocation?)null);
            MapViewModel viewModel = CreateViewModel(gps.Object);

            await viewModel.InitializeLocationAsync();
            await viewModel.CenterOnLocationAsync(CancellationToken.None);
            await viewModel.SettingsAsync(CancellationToken.None);

            Assert.DoesNotContain(viewModel.Map!.Layers, layer => layer.Name == "Pins");
            Assert.False(viewModel.IsAddVisible);
        }

        [Fact]
        /// <summary>
        /// Executes initialization Replaces Existing Location Pin.
        /// </summary>
        public async Task Initialization_ReplacesExistingLocationPin()
        {
            Mock<IGpsService> gps = new Mock<IGpsService>();
            gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });
            MapViewModel viewModel = CreateViewModel(gps.Object);

            await viewModel.InitializeLocationAsync();
            await viewModel.InitializeLocationAsync();

            Assert.Single(viewModel.Map!.Layers, layer => layer.Name == "Pins");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        /// <summary>
        /// Executes cancellation Prevents Late Location Results From Changing The View.
        /// </summary>
        /// <param name="initialize">The initialize.</param>
        public async Task Cancellation_PreventsLateLocationResultsFromChangingTheView(bool initialize)
        {
            TaskCompletionSource<GpsLocation?> completion = new TaskCompletionSource<GpsLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<IGpsService> gps = new Mock<IGpsService>();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
            MapViewModel viewModel = CreateViewModel(gps.Object);
            Task pending = initialize
                ? viewModel.InitializeLocationAsync(cancellation.Token)
                : viewModel.SettingsAsync(cancellation.Token);

            cancellation.Cancel();
            completion.SetResult(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });

            if (initialize)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            else
                await pending;
            Assert.DoesNotContain(viewModel.Map!.Layers, layer => layer.Name == "Pins");
            Assert.False(viewModel.IsAddVisible);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        /// <summary>
        /// Executes canceling Bound Commands Completes Without An Unhandled Cancellation.
        /// </summary>
        /// <param name="center">The center.</param>
        public async Task CancelingBoundCommands_CompletesWithoutAnUnhandledCancellation(bool center)
        {
            Mock<IGpsService> gps = new Mock<IGpsService>();
            TaskCompletionSource requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
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
            Mock<IHydrantService> hydrants = new Mock<IHydrantService>();
            // Hydrant loading is unrelated to these tests and must not mutate map layers concurrently.
            hydrants.Setup(service => service.GetAllAsync()).Returns(Task.FromCanceled<IEnumerable<Hydrant>>(new CancellationToken(true)));
            return new MapViewModel(hydrants.Object, gps);
        }
    }

}
