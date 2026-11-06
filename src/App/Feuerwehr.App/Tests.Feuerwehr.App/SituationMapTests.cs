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
    /// <summary>
    /// Tests situation map behavior and its expected outcomes.
    /// </summary>
    public class SituationMapTests
    {
        /// <summary>
        /// Verifies that only viewer role shows navigation and allows loading.
        /// </summary>
        /// <param name="role">The Identity role under test.</param>
        /// <param name="allowed">The expected allowed flag for this test case.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("SituationMapViewer", true)]
        [InlineData("SituationMapEditor", true)]
        [InlineData("IncidentDiaryViewer", false)]
        [InlineData("Admin", false)]
        public async Task OnlyViewerRole_ShowsNavigationAndAllowsLoading(string role, bool allowed)
        {
            Mock<IAuthService> auth = CreateAuth(role);
            Mock<ISituationMapService> service = new (MockBehavior.Strict);
            if (allowed)
            {
                _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IncidentSummaryDto?)null);
            }

            SituationMapViewModel viewModel = new (service.Object, auth.Object);

            Assert.Equal(allowed, new MainViewModel(auth.Object).CanViewSituationMap);
            Assert.Equal(allowed, viewModel.RefreshCommand.CanExecute(null));
            await viewModel.RefreshAsync(CancellationToken.None);

            service.Verify(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>()), allowed ? Times.Once() : Times.Never());
            Assert.Null(viewModel.Map);
        }

        /// <summary>
        /// Verifies that refresh replaces elements and keeps viewport then clears when incident ends.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Refresh_ReplacesElementsAndKeepsViewport_ThenClearsWhenIncidentEnds()
        {
            IncidentSummaryDto incident = Incident();
            Mock<ISituationMapService> service = new ();
            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            _ = service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(State(incident.Id, Element("[10.6,53.9]")));
            SituationMapViewModel viewModel = new (service.Object, CreateAuth().Object);
            viewModel.Activate();

            await viewModel.RefreshCommand.ExecuteAsync(null);
            Map map = Assert.IsType<Mapsui.Map>(viewModel.Map);
            map.Layers.First().Enabled = false;
            map.Navigator.SetSize(800, 450);
            (double x, double y) origin = SphericalMercator.FromLonLat(incident.OriginLongitude, incident.OriginLatitude);
            Assert.Equal(origin.x, map.Navigator.Viewport.CenterX, 5);
            Assert.Equal(origin.y, map.Navigator.Viewport.CenterY, 5);
            map.Navigator.CenterOn(1234, 5678);
            _ = service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(State(incident.Id));
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Same(map, viewModel.Map);
            Assert.Equal(1234, map.Navigator.Viewport.CenterX);
            Assert.Empty(Assert.IsType<MemoryLayer>(Assert.Single(map.Layers, layer => layer.Name == "Lage")).Features);
            _ = Assert.Single(map.Layers, layer => layer.Name == "Hydranten");
            Assert.NotEmpty(viewModel.LastUpdated);

            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IncidentSummaryDto?)null);
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Null(viewModel.Map);
            Assert.Empty(viewModel.LastUpdated);
            Assert.Equal("Kein aktiver Einsatz vorhanden.", viewModel.StatusMessage);
        }

        /// <summary>
        /// Verifies that role revocation cancels loading and discards late results.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task RoleRevocation_CancelsLoadingAndDiscardsLateResults()
        {
            TaskCompletionSource<MapStateResponse> completion = new ();
            IncidentSummaryDto incident = Incident();
            Mock<IAuthService> auth = CreateAuth();
            Mock<ISituationMapService> service = new ();
            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            _ = service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>())).Returns(completion.Task);
            SituationMapViewModel viewModel = new (service.Object, auth.Object);
            viewModel.Activate();
            Task pending = viewModel.RefreshCommand.ExecuteAsync(null);

            _ = auth.SetupGet(s => s.UserRoles).Returns(Array.Empty<string>());
            auth.Raise(s => s.AuthStateChanged += null, EventArgs.Empty);
            completion.SetResult(State(incident.Id));
            await pending;

            Assert.Null(viewModel.Map);
            Assert.False(viewModel.CanView);
            Assert.False(viewModel.RefreshCommand.CanExecute(null));
            viewModel.Deactivate();
        }

        /// <summary>
        /// Verifies that navigation away discards pending response.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task NavigationAway_DiscardsPendingResponse()
        {
            TaskCompletionSource<IncidentSummaryDto?> completion = new ();
            Mock<ISituationMapService> service = new ();
            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);
            SituationMapViewModel viewModel = new (service.Object, CreateAuth().Object);
            viewModel.Activate();
            Task pending = viewModel.RefreshCommand.ExecuteAsync(null);

            viewModel.Deactivate();
            completion.SetResult(Incident());
            await pending;

            Assert.Null(viewModel.Map);
            service.Verify(s => s.GetStateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that refresh failure clears old map and allows retry.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task RefreshFailure_ClearsOldMapAndAllowsRetry()
        {
            IncidentSummaryDto incident = Incident();
            Mock<ISituationMapService> service = new ();
            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(incident);
            _ = service.Setup(s => s.GetStateAsync(incident.Id, It.IsAny<CancellationToken>())).ReturnsAsync(State(incident.Id));
            SituationMapViewModel viewModel = new (service.Object, CreateAuth().Object);
            await viewModel.RefreshCommand.ExecuteAsync(null);

            _ = service.Setup(s => s.GetActiveIncidentAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
            await viewModel.RefreshCommand.ExecuteAsync(null);

            Assert.Null(viewModel.Map);
            Assert.Empty(viewModel.LastUpdated);
            Assert.Contains("erneut aktualisieren", viewModel.StatusMessage);
            Assert.True(viewModel.RefreshCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies that layers project coordinates and preserve polygon holes and SVG symbols.
        /// </summary>
        [Fact]
        public void Layers_ProjectCoordinatesAndPreservePolygonHolesAndSvgSymbols()
        {
#pragma warning disable SA1101 // StyleCop 1.1.118 misidentifies record 'with' initializer properties as local members.
            MapElementDto symbol = Element("[10.6,53.9]") with
            {
                SymbolSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"40\" /></svg>",
                RadioCallName = "Florian 1"
            };
            MapElementDto polygon = Element("[[[10,53],[11,53],[11,54],[10,53]],[[10.2,53.2],[10.3,53.2],[10.3,53.3],[10.2,53.2]]]") with
            {
                ElementType = MapElementType.Polygon
            };
#pragma warning restore SA1101
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

        /// <summary>
        /// Verifies that HTTP client uses only get and reads string enums handles missing active incident.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task HttpClient_UsesOnlyGetAndReadsStringEnums_HandlesMissingActiveIncident()
        {
            IncidentSummaryDto incident = Incident();
            using ReadOnlyHandler handler = new (incident);
            using HttpClient http = new (handler) { BaseAddress = new Uri("https://example.test") };
            Mock<IAuthService> auth = CreateAuth();
            _ = auth.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync("test-token");
            SituationMapService service = new (new AuthenticatedHttpClient(http, auth.Object));

            Assert.Equal(incident, await service.GetActiveIncidentAsync(CancellationToken.None));
            MapStateResponse state = await service.GetStateAsync(incident.Id, CancellationToken.None);
            Assert.Equal(MapElementType.Symbol, Assert.Single(state.Elements).ElementType);
            handler.NoActiveIncident = true;
            Assert.Null(await service.GetActiveIncidentAsync(CancellationToken.None));
            Assert.Equal(3, handler.RequestCount);
        }

        private static Mock<IAuthService> CreateAuth(string role = "SituationMapViewer")
        {
            Mock<IAuthService> auth = new ();
            _ = auth.SetupGet(s => s.IsAuthenticated).Returns(true);
            _ = auth.SetupGet(s => s.UserRoles).Returns(new[] { role });
            return auth;
        }

        private static IncidentSummaryDto Incident() => new (
            Guid.NewGuid(),
            "Einsatz",
            "123",
            null,
            null,
            10.6,
            53.9,
            IncidentStatus.Active,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            1);

        private static MapElementDto Element(string coordinates) => new (
            Guid.NewGuid(),
            Guid.NewGuid(),
            MapElementType.Symbol,
            null,
            null,
            "Fahrzeug",
            null,
            null,
            null,
            "#d62828",
            "{\"type\":\"Point\",\"coordinates\":" + coordinates + "}",
            1,
            DateTime.UtcNow,
            "Test");

        private static MapStateResponse State(Guid incidentId, params MapElementDto[] elements) =>
            new (incidentId, IncidentStatus.Active, 1, DateTime.UtcNow, null, elements, []);

        /// <summary>
        /// Supplies map API responses while counting client requests in read-only behavior tests.
        /// </summary>
        /// <param name="incident">The incident associated with this situation map.</param>
        private sealed class ReadOnlyHandler(IncidentSummaryDto incident) : HttpMessageHandler
        {
            /// <summary>
            /// Gets or sets a value indicating whether no active incident is enabled for this read only handler.
            /// </summary>
            public bool NoActiveIncident { get; set; }

            /// <summary>
            /// Gets the number of HTTP requests observed by the test handler.
            /// </summary>
            public int RequestCount { get; private set; }

            /// <summary>
            /// Sends the supplied email notification through the configured SMTP server.
            /// </summary>
            /// <param name="request">The HTTP request message input to validate and apply.</param>
            /// <param name="cancellationToken">A token that cancels the operation.</param>
            /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.RequestCount++;
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                JsonSerializerOptions options = new (JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
                if (request.RequestUri!.AbsolutePath == "/api/incidents/active")
                {
                    return Task.FromResult(this.NoActiveIncident
                        ? new HttpResponseMessage(HttpStatusCode.NotFound)
                        : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(incident, options: options) });
                }

                Assert.Equal($"/api/incidents/{incident.Id}/map/state", request.RequestUri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(State(incident.Id, Element("[10.6,53.9]")), options: options)
                });
            }
        }
    }
}
