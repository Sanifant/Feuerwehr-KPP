using de.openelp.feuerwehr.App.Models;
using de.openelp.feuerwehr.App.Services;
using de.openelp.feuerwehr.App.ViewModels;
using de.openelp.feuerwehr.Common.Models;
using Moq;

namespace de.openelp.feuerwehr.Tests.App;

public class MapLocationTests
{
    [Fact]
    public void ConstructingMap_DoesNotRequestLocationBeforeTheViewIsLoaded()
    {
        var gps = new Mock<IGpsService>(MockBehavior.Strict);

        var viewModel = CreateViewModel(gps.Object);

        Assert.NotNull(viewModel.Map);
        gps.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Settings_WaitsAsynchronouslyAndUsesTheReturnedCoordinates()
    {
        var completion = new TaskCompletionSource<GpsLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gps = new Mock<IGpsService>();
        using var cancellation = new CancellationTokenSource();
        gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
        var viewModel = CreateViewModel(gps.Object);

        var pending = viewModel.SettingsAsync(cancellation.Token);

        Assert.False(pending.IsCompleted);
        Assert.False(viewModel.IsAddVisible);
        completion.SetResult(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });
        await pending;

        Assert.True(viewModel.IsAddVisible);
        Assert.Equal(53.9, viewModel.NewHydrant.Latitude);
        Assert.Equal(10.6, viewModel.NewHydrant.Longitude);
    }

    [Fact]
    public async Task UnavailableLocation_DoesNotCreateAPinOrOpenHydrantCreation()
    {
        var gps = new Mock<IGpsService>();
        gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GpsLocation?)null);
        var viewModel = CreateViewModel(gps.Object);

        await viewModel.InitializeLocationAsync();
        await viewModel.CenterOnLocationAsync(CancellationToken.None);
        await viewModel.SettingsAsync(CancellationToken.None);

        Assert.DoesNotContain(viewModel.Map!.Layers, layer => layer.Name == "Pins");
        Assert.False(viewModel.IsAddVisible);
    }

    [Fact]
    public async Task Initialization_ReplacesExistingLocationPin()
    {
        var gps = new Mock<IGpsService>();
        gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GpsLocation { Latitude = 53.9, Longitude = 10.6 });
        var viewModel = CreateViewModel(gps.Object);

        await viewModel.InitializeLocationAsync();
        await viewModel.InitializeLocationAsync();

        Assert.Single(viewModel.Map!.Layers, layer => layer.Name == "Pins");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancellation_PreventsLateLocationResultsFromChangingTheView(bool initialize)
    {
        var completion = new TaskCompletionSource<GpsLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gps = new Mock<IGpsService>();
        using var cancellation = new CancellationTokenSource();
        gps.Setup(service => service.GetCurrentLocationAsync(cancellation.Token)).Returns(completion.Task);
        var viewModel = CreateViewModel(gps.Object);
        var pending = initialize
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
    public async Task CancelingBoundCommands_CompletesWithoutAnUnhandledCancellation(bool center)
    {
        var gps = new Mock<IGpsService>();
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        gps.Setup(service => service.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                requested.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return null;
            });
        var viewModel = CreateViewModel(gps.Object);
        var command = center ? viewModel.CenterOnLocationCommand : viewModel.SettingsCommand;

        var pending = command.ExecuteAsync(null);
        await requested.Task;
        command.Cancel();
        await pending;

        Assert.True(pending.IsCompletedSuccessfully);
        Assert.False(viewModel.IsAddVisible);
    }

    private static MapViewModel CreateViewModel(IGpsService gps)
    {
        var hydrants = new Mock<IHydrantService>();
        // Hydrant loading is unrelated to these tests and must not mutate map layers concurrently.
        hydrants.Setup(service => service.GetAllAsync()).Returns(Task.FromCanceled<IEnumerable<Hydrant>>(new CancellationToken(true)));
        return new MapViewModel(hydrants.Object, gps);
    }
}
