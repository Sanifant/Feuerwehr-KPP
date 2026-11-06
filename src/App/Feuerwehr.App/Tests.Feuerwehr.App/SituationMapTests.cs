using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Moq;
using NetTopologySuite.Geometries;

namespace De.Openelp.Feuerwehr.Tests.App
{
    public class SituationMapTests
    {
        [Theory]
        [InlineData("SituationMapViewer", true)]
        [InlineData("SituationMapEditor", true)]
        [InlineData("IncidentDiaryViewer", false)]
        [InlineData("Admin", false)]
        /// <summary>
        /// Executes only Viewer Role Shows Navigation And Allows Loading.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="allowed">The allowed.</param>
        public async Task OnlyViewerRole_ShowsNavigationAndAllowsLoading(string role, bool allowed)
        {
            Mock<IAuthService> auth = CreateAuth(role);
            Mock<ISituationMapService> service = new Mock<ISituationMapService>(MockBehavior.Strict);
            if (allowed)
                service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IncidentSummaryDto?)null);
            SituationMapViewModel viewModel = new SituationMapViewModel(service.Object, auth.Object);

            Assert.Equal(allowed, new MainViewModel(auth.Object).CanViewSituationMap);
            Assert.Equal(allowed, viewModel.RefreshCommand.CanExecute(null));
            await viewModel.RefreshAsync(CancellationToken.None);

            service.Verify(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>()), allowed ? Times.Once() : Times.Never());
            Assert.Null(viewModel.Map);
        }

        [Fact]
        /// <summary>
        /// Executes refresh Replaces Elements And Keeps Viewport Then Clears When Incident Ends.
        /// </summary>
        public async Task Refresh_ReplacesElementsAndKeepsViewport_ThenClearsWhenIncidentEnds()
        {
            IncidentSummaryDto incident = Incident();
            Mock<ISituationMapService> service = new Mock<ISituationMapService>();
            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(State(incident.Id, Element("[10.6,53.9]")));
            SituationMapViewModel viewModel = new SituationMapViewModel(service.Object, CreateAuth().Object);
            viewModel.Activate();

            await viewModel.RefreshCommand.ExecuteAsync(null);
            Map map = Assert.IsType<Mapsui.Map>(viewModel.Map);
            // A real MapControl supplies the size; suppress tile downloads in this unit test.
            map.Layers.First().Enabled = false;
            map.Navigator.SetSize(800, 450);
            (double x, double y) origin = SphericalMercator.FromLonLat(incident.OriginLongitude, incident.OriginLatitude);
            Assert.Equal(origin.x, map.Navigator.Viewport.CenterX, 5);
            Assert.Equal(origin.y, map.Navigator.Viewport.CenterY, 5);
            map.Navigator.CenterOn(1234, 5678);
            service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(State(incident.Id));
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Same(map, viewModel.Map);
            Assert.Equal(1234, map.Navigator.Viewport.CenterX);
            Assert.Empty(Assert.IsType<MemoryLayer>(Assert.Single(map.Layers, layer => layer.Name == "Lage")).Features);
            Assert.Single(map.Layers, layer => layer.Name == "Hydranten");
            Assert.NotEmpty(viewModel.LastUpdated);

            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IncidentSummaryDto?)null);
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Null(viewModel.Map);
            Assert.Empty(viewModel.LastUpdated);
            Assert.Equal("Kein aktiver Einsatz vorhanden.", viewModel.StatusMessage);
        }

        [Fact]
        /// <summary>
        /// Executes role Revocation Cancels Loading And Discards Late Results.
        /// </summary>
        public async Task RoleRevocation_CancelsLoadingAndDiscardsLateResults()
        {
            TaskCompletionSource<MapStateResponse> completion = new TaskCompletionSource<MapStateResponse>();
            IncidentSummaryDto incident = Incident();
            Mock<IAuthService> auth = CreateAuth();
            Mock<ISituationMapService> service = new Mock<ISituationMapService>();
            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>())).Returns(completion.Task);
            SituationMapViewModel viewModel = new SituationMapViewModel(service.Object, auth.Object);
            viewModel.Activate();
            Task pending = viewModel.RefreshCommand.ExecuteAsync(null);

            auth.SetupGet(s => s.UserRoles).Returns(Array.Empty<string>());
            auth.Raise(s => s.AuthStateChanged += null, EventArgs.Empty);
            completion.SetResult(State(incident.Id));
            await pending;

            Assert.Null(viewModel.Map);
            Assert.False(viewModel.CanView);
            Assert.False(viewModel.RefreshCommand.CanExecute(null));
            viewModel.Deactivate();
        }

        [Fact]
        /// <summary>
        /// Executes navigation Away Discards Pending Response.
        /// </summary>
        public async Task NavigationAway_DiscardsPendingResponse()
        {
            TaskCompletionSource<IncidentSummaryDto?> completion = new TaskCompletionSource<IncidentSummaryDto?>();
            Mock<ISituationMapService> service = new Mock<ISituationMapService>();
            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);
            SituationMapViewModel viewModel = new SituationMapViewModel(service.Object, CreateAuth().Object);
            viewModel.Activate();
            Task pending = viewModel.RefreshCommand.ExecuteAsync(null);

            viewModel.Deactivate();
            completion.SetResult(Incident());
            await pending;

            Assert.Null(viewModel.Map);
            service.Verify(s => s.GetStateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        /// <summary>
        /// Executes refresh Failure Clears Old Map And Allows Retry.
        /// </summary>
        public async Task RefreshFailure_ClearsOldMapAndAllowsRetry()
        {
            IncidentSummaryDto incident = Incident();
            Mock<ISituationMapService> service = new Mock<ISituationMapService>();
            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>())).ReturnsAsync(State(incident.Id));
            SituationMapViewModel viewModel = new SituationMapViewModel(service.Object, CreateAuth().Object);
            await viewModel.RefreshCommand.ExecuteAsync(null);

            service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Null(viewModel.Map);
            Assert.Empty(viewModel.LastUpdated);
            Assert.Contains("erneut aktualisieren", viewModel.StatusMessage);
            Assert.True(viewModel.RefreshCommand.CanExecute(null));
        }

        [Fact]
        /// <summary>
        /// Executes layers Project Coordinates And Preserve Polygon Holes And Svg Symbols.
        /// </summary>
        public void Layers_ProjectCoordinatesAndPreservePolygonHolesAndSvgSymbols()
        {
            MapElementDto symbol = Element("[10.6,53.9]") with
            {
                SymbolSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"40\" /></svg>",
                RadioCallName = "Florian 1"
            };
            MapElementDto polygon = Element("[[[10,53],[11,53],[11,54],[10,53]],[[10.2,53.2],[10.3,53.2],[10.3,53.3],[10.2,53.2]]]") with
            {
                ElementType = MapElementType.Polygon
            };
            using MemoryLayer layer = SituationMapLayers.CreateElements([symbol, polygon]);
            IFeature[] features = layer.Features.ToArray();
            PointFeature point = Assert.IsType<PointFeature>(features[0]);
            (double x, double y) expected = SphericalMercator.FromLonLat(10.6, 53.9);

            Assert.Equal(expected.x, point.Point.X, 5);
            Assert.Equal(expected.y, point.Point.Y, 5);
            Assert.Contains("svg-content://", Assert.IsType<Image>(Assert.Single(point.Styles.OfType<ImageStyle>()).Image).Source);
            Assert.Equal("Florian 1", Assert.Single(point.Styles.OfType<LabelStyle>()).GetLabelText(point));
            Assert.Equal(1, Assert.IsType<Polygon>(Assert.IsType<GeometryFeature>(features[1]).Geometry).NumInteriorRings);
        }

        [Fact]
        /// <summary>
        /// Executes http Client Uses Only Get And Reads String Enums Handles Missing Active Incident.
        /// </summary>
        public async Task HttpClient_UsesOnlyGetAndReadsStringEnums_HandlesMissingActiveIncident()
        {
            IncidentSummaryDto incident = Incident();
            using ReadOnlyHandler handler = new ReadOnlyHandler(incident);
            using HttpClient http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
            Mock<IAuthService> auth = CreateAuth();
            auth.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync("test-token");
            SituationMapService service = new SituationMapService(new AuthenticatedHttpClient(http, auth.Object));

            Assert.Equal(incident, await service.GetActiveIncidentAsync(CancellationToken.None));
            MapStateResponse state = await service.GetStateAsync(incident.Id, CancellationToken.None);
            Assert.Equal(MapElementType.Symbol, Assert.Single(state.Elements).ElementType);
            handler.NoActiveIncident = true;
            Assert.Null(await service.GetActiveIncidentAsync(CancellationToken.None));
            Assert.Equal(3, handler.RequestCount);
        }

        private static Mock<IAuthService> CreateAuth(string role = "SituationMapViewer")
        {
            Mock<IAuthService> auth = new Mock<IAuthService>();
            auth.SetupGet(s => s.IsAuthenticated).Returns(true);
            auth.SetupGet(s => s.UserRoles).Returns(new[] { role });
            return auth;
        }

        private static IncidentSummaryDto Incident() => new(Guid.NewGuid(), "Einsatz", "123", null, null,
            10.6, 53.9, IncidentStatus.Active, DateTime.UtcNow, DateTime.UtcNow, null, 1);

        private static MapElementDto Element(string coordinates) => new(Guid.NewGuid(), Guid.NewGuid(), MapElementType.Symbol,
            null, null, "Fahrzeug", null, null, null, "#d62828", "{\"type\":\"Point\",\"coordinates\":" + coordinates + "}", 1, DateTime.UtcNow, "Test");

        private static MapStateResponse State(Guid incidentId, params MapElementDto[] elements) =>
            new(incidentId, IncidentStatus.Active, 1, DateTime.UtcNow, null, elements, []);

        private sealed class ReadOnlyHandler(IncidentSummaryDto incident) : HttpMessageHandler
        {
            public bool NoActiveIncident { get; set; }
            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
                if (request.RequestUri!.AbsolutePath == "/api/incidents/active")
                    return Task.FromResult(NoActiveIncident
                        ? new HttpResponseMessage(HttpStatusCode.NotFound)
                        : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(incident, options: options) });

                Assert.Equal($"/api/incidents/{incident.Id}/map/state", request.RequestUri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(State(incident.Id, Element("[10.6,53.9]")), options: options)
                });
            }
        }
    }

}
