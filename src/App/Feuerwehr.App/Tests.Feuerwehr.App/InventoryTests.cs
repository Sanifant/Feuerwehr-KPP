using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.App;

public sealed class InventoryTests
{
    [Theory]
    [InlineData("EquipmentOfficer", true, true)]
    [InlineData("InventoryViewer", true, false)]
    [InlineData("Admin", true, true)]
    [InlineData("Firefighter", false, false)]
    /// <summary>
    /// Executes role Controls Navigation Lookup And Inspection.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="view">The view.</param>
    /// <param name="manage">The manage.</param>
    public async Task RoleControlsNavigationLookupAndInspection(string role, bool view, bool manage)
    {
        var auth = Auth(role); var service = new Mock<IInventoryService>();
        service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Detail());
        var vm = new InventoryViewModel(service.Object, auth.Object, new UnsupportedBarcodeScanner()) { Barcode = "KPP-001" };
        Assert.Equal(view, new MainViewModel(auth.Object).CanViewInventory);
        Assert.Equal(view, vm.LookupCommand.CanExecute(null));
        await vm.LookupAsync(CancellationToken.None);
        Assert.Equal(manage, vm.CanInspect);
        service.Verify(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), view ? Times.Once() : Times.Never());
    }

    [Fact]
    /// <summary>
    /// Executes camera Result Loads Device And Inspection Uses Its Version And Preserves Retry Command.
    /// </summary>
    public async Task CameraResultLoadsDeviceAndInspectionUsesItsVersionAndPreservesRetryCommand()
    {
        var service = new Mock<IInventoryService>(); var detail = Detail();
        var scanner = new Mock<IBarcodeScanner>(); scanner.SetupGet(x => x.IsSupported).Returns(true);
        scanner.Setup(x => x.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(" kpp-001 ");
        service.Setup(x => x.FindBarcodeAsync("KPP-001", It.IsAny<CancellationToken>())).ReturnsAsync(detail);
        var requests = new List<RecordEquipmentInspectionRequest>();
        service.Setup(x => x.InspectAsync(detail.Equipment.Id, It.IsAny<RecordEquipmentInspectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, RecordEquipmentInspectionRequest, CancellationToken>((_, request, _) => requests.Add(request))
            .ThrowsAsync(new HttpRequestException("offline"));
        var vm = new InventoryViewModel(service.Object, Auth("EquipmentOfficer").Object, scanner.Object);
        await vm.ScanAsync(CancellationToken.None);
        Assert.Contains("KPP-001", vm.DeviceTitle); Assert.Contains("Nächste Prüfung:", vm.NextInspection);
        vm.Comment = "Dichtung defekt"; vm.SelectedStatus = 1;
        await vm.SaveInspectionAsync(CancellationToken.None); await vm.SaveInspectionAsync(CancellationToken.None);
        Assert.Equal(2, requests.Count); Assert.Equal(requests[0].CommandId, requests[1].CommandId);
        Assert.Equal(EquipmentStatus.Defective, requests[0].Status); Assert.Equal("Dichtung defekt", requests[0].Comment); Assert.Equal(7, requests[0].ExpectedVersion);
        Assert.Equal("Dichtung defekt", vm.Comment);
    }

    [Fact]
    /// <summary>
    /// Executes inactive Device And Role Revocation Prevent Maintenance And Discard Late Lookup.
    /// </summary>
    public async Task InactiveDeviceAndRoleRevocationPreventMaintenanceAndDiscardLateLookup()
    {
        var auth = Auth("EquipmentOfficer"); var service = new Mock<IInventoryService>();
        service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Detail(false));
        var vm = new InventoryViewModel(service.Object, auth.Object, new UnsupportedBarcodeScanner()); vm.Activate();
        await vm.LookupCommand.ExecuteAsync(null); Assert.False(vm.CanInspect);
        var completion = new TaskCompletionSource<EquipmentDetailDto>();
        service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(completion.Task);
        var loading = vm.LookupCommand.ExecuteAsync(null);
        auth.SetupGet(x => x.UserRoles).Returns(Array.Empty<string>()); auth.Raise(x => x.AuthStateChanged += null, EventArgs.Empty);
        completion.SetResult(Detail()); await loading;
        Assert.Null(vm.Detail); Assert.False(vm.CanInspect); Assert.Empty(vm.History); vm.Deactivate();
    }

    [Fact]
    /// <summary>
    /// Executes client Sends Authenticated Json With String Status And Encodes Barcode.
    /// </summary>
    public async Task ClientSendsAuthenticatedJsonWithStringStatusAndEncodesBarcode()
    {
        var handler = new InventoryHandler(); using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        var auth = Auth("EquipmentOfficer"); auth.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("test-token");
        var service = new InventoryService(new AuthenticatedHttpClient(http, auth.Object));
        await service.FindBarcodeAsync("KPP-001", CancellationToken.None);
        Assert.Equal("/api/inventory/barcode/KPP-001", handler.Path); Assert.Equal("Bearer test-token", handler.Authorization);
        await service.InspectAsync(Detail().Equipment.Id, new RecordEquipmentInspectionRequest { CommandId = Guid.NewGuid(), InspectedOn = DateOnly.FromDateTime(DateTime.UtcNow), Status = EquipmentStatus.InRepair, ExpectedVersion = 7 }, CancellationToken.None);
        Assert.Equal("InRepair", handler.Payload!.Value.GetProperty("status").GetString());
        Assert.Equal(7, handler.Payload.Value.GetProperty("expectedVersion").GetInt32());
    }

    private static Mock<IAuthService> Auth(string role)
    {
        var auth = new Mock<IAuthService>(); auth.SetupGet(x => x.IsAuthenticated).Returns(true); auth.SetupGet(x => x.UserRoles).Returns(new[] { role }); return auth;
    }
    private static EquipmentDetailDto Detail(bool active = true)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new(new EquipmentDto(Guid.Parse("30500200-0000-0000-0000-000000000001"), "KPP-001", Guid.NewGuid(), "Strahlrohr", "Armaturen", "SN-123", "Hersteller", EquipmentStatus.Ready,
            active, today, null, today.AddMonths(12), 12, "Sicht- und Funktionsprüfung", 7), []);
    }
    private sealed class InventoryHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public JsonElement? Payload { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath; Authorization = request.Headers.Authorization?.ToString();
            if (request.Content is not null) Payload = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Detail()) };
        }
    }
}

