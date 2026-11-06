using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.App
{
    /// <summary>
    /// Tests equipment behavior and its expected outcomes.
    /// </summary>
    public sealed class InventoryTests
    {
        /// <summary>
        /// Verifies that role controls navigation lookup and inspection.
        /// </summary>
        /// <param name="role">The Identity role under test.</param>
        /// <param name="view">The expected view flag for this test case.</param>
        /// <param name="manage">The expected manage flag for this test case.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("EquipmentOfficer", true, true)]
        [InlineData("InventoryViewer", true, false)]
        [InlineData("Admin", true, true)]
        [InlineData("Firefighter", false, false)]
        public async Task RoleControlsNavigationLookupAndInspection(string role, bool view, bool manage)
        {
            Mock<IAuthService> auth = Auth(role);
            Mock<IInventoryService> service = new ();
            _ = service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Detail());
            InventoryViewModel vm = new (service.Object, auth.Object, new UnsupportedBarcodeScanner()) { Barcode = "KPP-001" };
            Assert.Equal(view, new MainViewModel(auth.Object).CanViewInventory);
            Assert.Equal(view, vm.LookupCommand.CanExecute(null));
            await vm.LookupAsync(CancellationToken.None);
            Assert.Equal(manage, vm.CanInspect);
            service.Verify(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), view ? Times.Once() : Times.Never());
        }

        /// <summary>
        /// Verifies that camera result loads device and inspection uses its version and preserves retry command.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CameraResultLoadsDeviceAndInspectionUsesItsVersionAndPreservesRetryCommand()
        {
            Mock<IInventoryService> service = new ();
            EquipmentDetailDto detail = Detail();
            Mock<IBarcodeScanner> scanner = new ();
            _ = scanner.SetupGet(x => x.IsSupported).Returns(true);
            _ = scanner.Setup(x => x.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(" kpp-001 ");
            _ = service.Setup(x => x.FindBarcodeAsync("KPP-001", It.IsAny<CancellationToken>())).ReturnsAsync(detail);
            List<RecordEquipmentInspectionRequest> requests = new ();
            _ = service.Setup(x => x.InspectAsync(detail.Equipment.Id, It.IsAny<RecordEquipmentInspectionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, RecordEquipmentInspectionRequest, CancellationToken>((_, request, _) => requests.Add(request))
                .ThrowsAsync(new HttpRequestException("offline"));
            InventoryViewModel vm = new (service.Object, Auth("EquipmentOfficer").Object, scanner.Object);
            await vm.ScanAsync(CancellationToken.None);
            Assert.Contains("KPP-001", vm.DeviceTitle);
            Assert.Contains("Nächste Prüfung:", vm.NextInspection);
            vm.Comment = "Dichtung defekt";
            vm.SelectedStatus = 1;
            await vm.SaveInspectionAsync(CancellationToken.None);
            await vm.SaveInspectionAsync(CancellationToken.None);
            Assert.Equal(2, requests.Count);
            Assert.Equal(requests[0].CommandId, requests[1].CommandId);
            Assert.Equal(EquipmentStatus.Defective, requests[0].Status);
            Assert.Equal("Dichtung defekt", requests[0].Comment);
            Assert.Equal(7, requests[0].ExpectedVersion);
            Assert.Equal("Dichtung defekt", vm.Comment);
        }

        /// <summary>
        /// Verifies that inactive device and role revocation prevent maintenance and discard late lookup.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task InactiveDeviceAndRoleRevocationPreventMaintenanceAndDiscardLateLookup()
        {
            Mock<IAuthService> auth = Auth("EquipmentOfficer");
            Mock<IInventoryService> service = new ();
            _ = service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Detail(false));
            InventoryViewModel vm = new (service.Object, auth.Object, new UnsupportedBarcodeScanner());
            vm.Activate();
            await vm.LookupCommand.ExecuteAsync(null);
            Assert.False(vm.CanInspect);
            TaskCompletionSource<EquipmentDetailDto> completion = new ();
            _ = service.Setup(x => x.FindBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(completion.Task);
            Task loading = vm.LookupCommand.ExecuteAsync(null);
            _ = auth.SetupGet(x => x.UserRoles).Returns(Array.Empty<string>());
            auth.Raise(x => x.AuthStateChanged += null, EventArgs.Empty);
            completion.SetResult(Detail());
            await loading;
            Assert.Null(vm.Detail);
            Assert.False(vm.CanInspect);
            Assert.Empty(vm.History);
            vm.Deactivate();
        }

        /// <summary>
        /// Verifies that client sends authenticated JSON with string status and encodes barcode.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ClientSendsAuthenticatedJsonWithStringStatusAndEncodesBarcode()
        {
            InventoryHandler handler = new ();
            using HttpClient http = new (handler) { BaseAddress = new Uri("https://example.test") };
            Mock<IAuthService> auth = Auth("EquipmentOfficer");
            _ = auth.Setup(x => x.GetAccessTokenAsync()).ReturnsAsync("test-token");
            InventoryService service = new (new AuthenticatedHttpClient(http, auth.Object));
            _ = await service.FindBarcodeAsync("KPP-001", CancellationToken.None);
            Assert.Equal("/api/inventory/barcode/KPP-001", handler.Path);
            Assert.Equal("Bearer test-token", handler.Authorization);
            _ = await service.InspectAsync(Detail().Equipment.Id, new RecordEquipmentInspectionRequest { CommandId = Guid.NewGuid(), InspectedOn = DateOnly.FromDateTime(DateTime.UtcNow), Status = EquipmentStatus.InRepair, ExpectedVersion = 7 }, CancellationToken.None);
            Assert.Equal("InRepair", handler.Payload!.Value.GetProperty("status").GetString());
            Assert.Equal(7, handler.Payload.Value.GetProperty("expectedVersion").GetInt32());
        }

        private static Mock<IAuthService> Auth(string role)
        {
            Mock<IAuthService> auth = new ();
            _ = auth.SetupGet(x => x.IsAuthenticated).Returns(true);
            _ = auth.SetupGet(x => x.UserRoles).Returns(new[] { role });
            return auth;
        }

        private static EquipmentDetailDto Detail(bool active = true)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new (new EquipmentDto(
                Guid.Parse("30500200-0000-0000-0000-000000000001"),
                "KPP-001",
                Guid.NewGuid(),
                "Strahlrohr",
                "Armaturen",
                "SN-123",
                "Hersteller",
                EquipmentStatus.Ready,
                active,
                today,
                null,
                today.AddMonths(12),
                12,
                "Sicht- und Funktionsprüfung",
                7), []);
        }

        /// <summary>
        /// Captures inventory HTTP requests and supplies deterministic API responses to client tests.
        /// </summary>
        private sealed class InventoryHandler : HttpMessageHandler
        {
            /// <summary>
            /// Gets the request path captured by the test HTTP handler.
            /// </summary>
            public string? Path { get; private set; }

            /// <summary>
            /// Gets the Authorization header captured from the last client request.
            /// </summary>
            public string? Authorization { get; private set; }

            /// <summary>
            /// Gets the JSON request body captured by the test HTTP handler.
            /// </summary>
            public JsonElement? Payload { get; private set; }

            /// <summary>
            /// Sends the supplied email notification through the configured SMTP server.
            /// </summary>
            /// <param name="request">The HTTP request message input to validate and apply.</param>
            /// <param name="cancellationToken">A token that cancels the operation.</param>
            /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.Path = request.RequestUri!.AbsolutePath;
                this.Authorization = request.Headers.Authorization?.ToString();
                if (request.Content is not null)
                {
                    this.Payload = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellationToken));
                }

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Detail()) };
            }
        }
    }
}
