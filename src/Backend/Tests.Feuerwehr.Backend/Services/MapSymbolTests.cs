using System.Data.Common;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Controller.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    public sealed class MapSymbolTests : IAsyncLifetime
    {
        private const string Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 50'><rect width='100' height='50' fill='red'/></svg>";
        private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aV7sAAAAASUVORK5CYII=";
        private const string Jpeg = "/9j/4AAQSkZJRgABAQEAYABgAAD/4QA6RXhpZgAATU0AKgAAAAgAA1EQAAEAAAABAQAAAFERAAQAAAABAAAAAFESAAQAAAABAAAAAAAAAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD9U6KKKAP/2Q==";
        private readonly SqliteConnection _connection = new ("Data Source=:memory:");
        private readonly Mock<IUserContextAccessor> _user = new ();
        private readonly TransientCommitFailure _commitFailure = new ();
        private FeuerwehrDbContext _db = null!;
        private MapSymbolService _symbols = null!;
        private SituationMapService _map = null!;
        private long _sequence;
        private readonly Guid _incidentId = Guid.NewGuid();

        /// <summary>
        /// Initializes the operation.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task InitializeAsync()
        {
            await this._connection.OpenAsync();
            this._connection.CreateFunction<string, long>("nextval", _ => ++this._sequence);
            this._db = new SqliteTestContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
                .UseSqlite(this._connection, options => options.ExecutionStrategy(dependencies => new NpgsqlRetryingExecutionStrategy(dependencies)))
                .AddInterceptors(this._commitFailure)
                .Options);
            _ = await this._db.Database.EnsureCreatedAsync();
            _ = this._user.SetupGet(x => x.UserId).Returns("editor-1");
            _ = this._user.SetupGet(x => x.DisplayName).Returns("Ada Muster");
            _ = this._user.SetupGet(x => x.IsAdmin).Returns(true);
            this._symbols = new MapSymbolService(this._db, this._user.Object, NullLogger<MapSymbolService>.Instance);
            this._map = new SituationMapService(this._db, this._user.Object, new CommandIdempotencyService(this._db), new OutboxService(this._db), new MapLeaseService(this._db));
            _ = this._db.Incidents.Add(new Incident { Id = this._incidentId, Name = "Brand", Status = IncidentStatus.Active, Revision = 1 });
            _ = this._db.EditorLeases.Add(new EditorLease
            {
                Id = Guid.NewGuid(),
                IncidentId = this._incidentId,
                UserId = "editor-1",
                UserDisplayName = "Ada Muster",
                SessionId = "session-1",
                LeaseToken = "lease-1",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                LastHeartbeatAtUtc = DateTime.UtcNow,
            });
            _ = await this._db.SaveChangesAsync();
        }

        [Fact]
        /// <summary>
        /// Executes catalog Has Four Categories And Stores Validated Svg With Consistent Dimensions.
        /// </summary>
        public async Task CatalogHasFourCategoriesAndStoresValidatedSvgWithConsistentDimensions()
        {
            Assert.Equal(MapSymbolCategories.All.OrderBy(x => x), Assert.IsAssignableFrom<IEnumerable<string>>((await this._symbols.GetCategories(default)).Value));
            foreach (string category in MapSymbolCategories.All)
            {
                ServiceResult result = await this._symbols.Save(null, SymbolRequest(category: category), default);
                Assert.Equal(201, result.Code);
                MapSymbolDto symbol = Assert.IsType<MapSymbolDto>(result.Value);
                Assert.Equal(category, symbol.Category);
                Assert.Contains("width=\"64\"", symbol.SvgContent);
                Assert.Contains("viewBox=\"0 0 100 50\"", symbol.SvgContent);
                Assert.Contains("preserveAspectRatio=\"xMidYMid meet\"", symbol.SvgContent);
            }

            Assert.Equal(4, await this._db.MapSymbols.CountAsync());
        }

        [Fact]
        /// <summary>
        /// Executes svg Allows Internal Gradients Styles And Editor Metadata.
        /// </summary>
        public async Task SvgAllowsInternalGradientsStylesAndEditorMetadata()
        {
            const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='32px' height='32px'><metadata><data xmlns='urn:metadata'>Editor</data></metadata><style>.a{fill:url(#red);stroke:#000}</style><defs><linearGradient id='red'><stop offset='0' stop-color='red'/></linearGradient></defs><path class='a' d='M0 0 L32 32'/></svg>";
            ServiceResult result = await this._symbols.Save(null, SymbolRequest(svg), default);
            Assert.Equal(201, result.Code);
            MapSymbolDto saved = Assert.IsType<MapSymbolDto>(result.Value);
            Assert.DoesNotContain("metadata", saved.SvgContent);
            Assert.Contains("viewBox=\"0 0 32 32\"", saved.SvgContent);
        }

        [Theory]
        [InlineData("href", "png", Png)]
        [InlineData("xlink:href", "png", Png)]
        [InlineData("href", "jpeg", Jpeg)]
        [InlineData("xlink:href", "jpeg", Jpeg)]
        /// <summary>
        /// Executes embedded Raster Can Be Uploaded Listed And Placed On The Map.
        /// </summary>
        /// <param name="referenceAttribute">The reference Attribute.</param>
        /// <param name="format">The format.</param>
        /// <param name="data">The data.</param>
        public async Task EmbeddedRasterCanBeUploadedListedAndPlacedOnTheMap(string referenceAttribute, string format, string data)
        {
            string svg = $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 32 32'><image width='32' height='32' {referenceAttribute}='data:image/{format};base64,{data}'/></svg>";
            ServiceResult result = await this._symbols.Save(null, SymbolRequest(svg), default);
            Assert.Equal(201, result.Code);
            MapSymbolDto saved = Assert.IsType<MapSymbolDto>(result.Value);
            Assert.Contains($"data:image/{format};base64,{data}", saved.SvgContent);
            Assert.Equal(saved.SvgContent, Assert.Single(Assert.IsAssignableFrom<IEnumerable<MapSymbolDto>>((await this._symbols.GetSymbols(default)).Value)).SvgContent);
            ServiceResult placed = await this._map.UpsertElement(this._incidentId, this.Write(saved.Id.ToString()), default);
            Assert.True(placed.Code == 200, placed.ProblemTitle);
            MapElementDto element = Assert.IsType<MapElementResponse>(placed.Value).Element;
            Assert.Equal(saved.SvgContent, element.SymbolSvg);
            Assert.Equal(saved.SvgContent, (await this._db.MapElements.SingleAsync()).SymbolSvg);
        }

        [Theory]
        [InlineData("https://example.org/image.png")]
        [InlineData("//example.org/image.png")]
        [InlineData("file:///C:/image.png")]
        [InlineData("image.png")]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:image/svg+xml;base64,PHN2Zy8+")]
        [InlineData("data:text/html;base64,PHNjcmlwdC8+")]
        [InlineData("data:image/png;base64,PHN2Zy8+")]
        [InlineData("data:image/jpeg;base64," + Png)]
        [InlineData("data:image/png;base64,not-base64!")]
        [InlineData("data:image/png;base64,")]
        [InlineData("data:image/png,%89PNG")]
        /// <summary>
        /// Executes embedded Images Reject External Active Mislabeled And Invalid Content.
        /// </summary>
        /// <param name="reference">The reference.</param>
        public async Task EmbeddedImagesRejectExternalActiveMislabeledAndInvalidContent(string reference)
        {
            string svg = $"<svg viewBox='0 0 32 32'><image href='{reference}'/></svg>";
            Assert.Equal(400, (await this._symbols.Save(null, SymbolRequest(svg), default)).Code);
            Assert.Empty(await this._db.MapSymbols.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes embedded Raster Data Remains Disallowed For Use Elements.
        /// </summary>
        public async Task EmbeddedRasterDataRemainsDisallowedForUseElements()
        {
            string svg = $"<svg viewBox='0 0 32 32'><use href='data:image/png;base64,{Png}'/></svg>";
            Assert.Equal(400, (await this._symbols.Save(null, SymbolRequest(svg), default)).Code);
            Assert.Empty(await this._db.MapSymbols.ToListAsync());
        }

        [Theory]
        [InlineData("<svg viewBox='0 0 10 10'><script>alert(1)</script></svg>")]
        [InlineData("<svg viewBox='0 0 10 10' onload='alert(1)'/>")]
        [InlineData("<svg viewBox='0 0 10 10'><foreignObject/></svg>")]
        [InlineData("<svg viewBox='0 0 10 10'><use href='https://example.org/icon.svg#x'/></svg>")]
        [InlineData("<svg viewBox='0 0 10 10'><style>@import 'https://example.org/x.css';</style></svg>")]
        [InlineData("<svg viewBox='0 0 10 10'><path fill='url(https://example.org/x)'/></svg>")]
        [InlineData("<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///secret'>]><svg viewBox='0 0 10 10'>&x;</svg>")]
        [InlineData("<html>Not SVG</html>")]
        [InlineData("\u0089PNG\r\n\u001a\n")]
        [InlineData("<svg viewBox='0 0 0 10'/>")]
        /// <summary>
        /// Executes upload Rejects Active External Or Invalid Svg.
        /// </summary>
        /// <param name="svg">The svg.</param>
        public async Task UploadRejectsActiveExternalOrInvalidSvg(string svg)
        {
            Assert.Equal(400, (await this._symbols.Save(null, SymbolRequest(svg), default)).Code);
            Assert.Empty(await this._db.MapSymbols.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes upload Checks Size Category And Admin Role And Hides Inactive Symbols For Viewers.
        /// </summary>
        public async Task UploadChecksSizeCategoryAndAdminRoleAndHidesInactiveSymbolsForViewers()
        {
            Assert.Equal(400, (await this._symbols.Save(null, SymbolRequest(new string('x', 262145)), default)).Code);
            Assert.Equal(400, (await this._symbols.Save(null, SymbolRequest(category: "Unbekannt"), default)).Code);
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            SaveMapSymbolRequest update = SymbolRequest();
            update.IsActive = false;
            Assert.Equal(200, (await this._symbols.Save(symbol.Id, update, default)).Code);
            _ = Assert.Single(Assert.IsAssignableFrom<IEnumerable<MapSymbolDto>>((await this._symbols.GetSymbols(default)).Value));
            _ = this._user.SetupGet(x => x.IsAdmin).Returns(false);
            Assert.Equal(403, (await this._symbols.Save(null, SymbolRequest(), default)).Code);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<MapSymbolDto>>((await this._symbols.GetSymbols(default)).Value));
        }

        [Fact]
        /// <summary>
        /// Executes symbol Can Be Placed Moved Reloaded And Deleted With Consistent Audit And Idempotency.
        /// </summary>
        public async Task SymbolCanBePlacedMovedReloadedAndDeletedWithConsistentAuditAndIdempotency()
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            UpsertMapElementRequest create = this.Write(symbol.Id.ToString());
            MapElementResponse created = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, create, default)).Value);
            Assert.Equal(symbol.SvgContent, created.Element.SymbolSvg);
            Assert.Equal("Feuerwehr", created.Element.Category);
            ServiceResult replay = await this._map.UpsertElement(this._incidentId, create, default);
            _ = Assert.IsType<JsonElement>(replay.Value);
            _ = Assert.Single(await this._db.MapElements.ToListAsync());
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());

            UpsertMapElementRequest move = this.Write(symbol.Id.ToString(), created.Element.Id, 1, "{\"type\":\"Point\",\"coordinates\":[10.26,51.51]}");
            move.Label = "LF 10";
            move.RadioCallName = "Florian 1";
            move.Note = "Neue Position";
            MapElementResponse moved = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, move, default)).Value);
            Assert.Equal(2, moved.Element.Version);
            this._db.ChangeTracker.Clear();
            MapElement stored = await this._db.MapElements.SingleAsync();
            Assert.Equal(move.GeometryJson, stored.GeometryJson);
            Assert.Equal(move.Label, stored.Label);
            Assert.Equal(move.RadioCallName, stored.RadioCallName);
            Assert.Equal(symbol.SvgContent, stored.SymbolSvg);
            Assert.Equal(move.GeometryJson, Assert.Single(Assert.IsType<MapStateResponse>((await this._map.GetState(this._incidentId, default)).Value).Elements).GeometryJson);
            ServiceResult deleted = await this._map.DeleteElement(
                this._incidentId,
                stored.Id,
                new DeleteMapElementRequest
                { CommandId = Guid.NewGuid().ToString(), SessionId = "session-1", LeaseToken = "lease-1", ExpectedVersion = 2 }, default);
            Assert.Equal(200, deleted.Code);
            Assert.Empty(await this._db.MapElements.ToListAsync());
            Assert.Equal(new[] { "create", "update", "delete" }, await this._db.MapAuditEvents.OrderBy(x => x.SequenceNumber).Select(x => x.Action).ToArrayAsync());
            Assert.Equal(3, await this._db.OutboxMessages.CountAsync());
        }

        [Fact]
        /// <summary>
        /// Executes catalog Changes Do Not Alter Existing Symbols And Inactive Symbols Cannot Be Placed Again.
        /// </summary>
        public async Task CatalogChangesDoNotAlterExistingSymbolsAndInactiveSymbolsCannotBePlacedAgain()
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            MapElementResponse created = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString()), default)).Value);
            SaveMapSymbolRequest update = SymbolRequest(Svg.Replace("red", "blue"), "Objekte");
            update.IsActive = false;
            _ = await this._symbols.Save(symbol.Id, update, default);
            Assert.Equal(400, (await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString()), default)).Code);
            MapElementResponse moved = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString(), created.Element.Id, 1), default)).Value);
            Assert.Equal(symbol.SvgContent, moved.Element.SymbolSvg);
            Assert.Equal("Feuerwehr", moved.Element.Category);
        }

        [Fact]
        /// <summary>
        /// Executes moves And Deletes Respect Versions And Editing Leases.
        /// </summary>
        public async Task MovesAndDeletesRespectVersionsAndEditingLeases()
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            MapElementDto original = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString()), default)).Value).Element;
            Assert.Equal(409, (await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString(), original.Id, 0), default)).Code);
            UpsertMapElementRequest wrongLease = this.Write(symbol.Id.ToString(), original.Id, 1);
            wrongLease.LeaseToken = "other";
            Assert.Equal(409, (await this._map.UpsertElement(this._incidentId, wrongLease, default)).Code);
            ServiceResult wrongLeaseDeletion = await this._map.DeleteElement(
                this._incidentId,
                original.Id,
                new DeleteMapElementRequest
                { CommandId = Guid.NewGuid().ToString(), SessionId = "session-1", LeaseToken = "other", ExpectedVersion = 1 }, default);
            Assert.Equal(409, wrongLeaseDeletion.Code);
            Assert.Equal(original.GeometryJson, (await this._db.MapElements.SingleAsync()).GeometryJson);
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());
        }

        [Theory]
        [InlineData("Admin", true, true)]
        [InlineData("SituationMapEditor", true, false)]
        [InlineData("SituationMapViewer", true, false)]
        [InlineData("Commander", false, false)]
        /// <summary>
        /// Executes catalog Http Permissions Separate Viewing And Administration.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="canView">The can View.</param>
        /// <param name="canManage">The can Manage.</param>
        public async Task CatalogHttpPermissionsSeparateViewingAndAdministration(string role, bool canView, bool canManage)
        {
            ServiceCollection services = new ServiceCollection();
            _ = services.AddLogging();
            _ = services.AddAuthorization(options => options.AddFeuerwehrPolicies());
            using ServiceProvider provider = services.BuildServiceProvider();
            IAuthorizationPolicyProvider policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
            IAuthorizationService auth = provider.GetRequiredService<IAuthorizationService>();
            ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));
            foreach (MethodInfo? method in typeof(MapSymbolController).GetMethods().Where(x => new[] { "GetSymbols", "GetCategories", "CreateCategory", "Create", "Update" }.Contains(x.Name)))
            {
                AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(policies, typeof(MapSymbolController).GetCustomAttributes<AuthorizeAttribute>().Concat(method.GetCustomAttributes<AuthorizeAttribute>()));
                Assert.NotNull(policy);
                Assert.Equal(method.Name.StartsWith("Get") ? canView : canManage, (await auth.AuthorizeAsync(user, null, policy)).Succeeded);
            }
        }

        [Fact]
        /// <summary>
        /// Executes polygon Is Saved With Audit And Idempotency Under The Npgsql Retry Strategy.
        /// </summary>
        public async Task PolygonIsSavedWithAuditAndIdempotencyUnderTheNpgsqlRetryStrategy()
        {
            UpsertMapElementRequest command = this.Write(string.Empty, geometry: "{\"type\":\"Polygon\",\"coordinates\":[[[10,51],[10.1,51],[10.1,51.1],[10,51]]]}");
            command.ElementType = MapElementType.Polygon;
            command.SymbolId = null;
            command.RadioCallName = null;
            command.Category = "Fläche";
            ServiceResult result = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.True(result.Code == 200, result.ProblemTitle);
            MapElementResponse saved = Assert.IsType<MapElementResponse>(result.Value);
            Assert.Equal(MapElementType.Polygon, saved.Element.ElementType);
            this._db.ChangeTracker.Clear();
            Assert.Equal(command.GeometryJson, (await this._db.MapElements.SingleAsync()).GeometryJson);
            Assert.Equal("create", (await this._db.MapAuditEvents.SingleAsync()).Action);
            _ = Assert.Single(await this._db.OutboxMessages.ToListAsync());
            _ = Assert.Single(await this._db.ProcessedCommands.ToListAsync());
            ServiceResult replay = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.Equal(200, replay.Code);
            _ = Assert.Single(await this._db.MapElements.ToListAsync());
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        /// <summary>
        /// Executes retrying Creation After An Uncertain Commit Does Not Duplicate Elements Or Audit.
        /// </summary>
        /// <param name="committed">The committed.</param>
        public async Task RetryingCreationAfterAnUncertainCommitDoesNotDuplicateElementsOrAudit(bool committed)
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            this._commitFailure.BeforeCommit = !committed;
            this._commitFailure.AfterCommit = committed;
            UpsertMapElementRequest command = this.Write(symbol.Id.ToString());
            ServiceResult result = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.True(result.Code == 200, result.ProblemTitle);
            Assert.Equal(1, this._commitFailure.Failures);
            this._db.ChangeTracker.Clear();
            Assert.Equal(1, (await this._db.MapElements.SingleAsync()).Version);
            Assert.Equal(2, (await this._db.Incidents.SingleAsync()).Revision);
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());
            _ = Assert.Single(await this._db.OutboxMessages.ToListAsync());
            _ = Assert.Single(await this._db.ProcessedCommands.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes retrying A Move Reloads The Original Version After Rollback.
        /// </summary>
        public async Task RetryingAMoveReloadsTheOriginalVersionAfterRollback()
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            MapElementResponse created = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString()), default)).Value);
            this._commitFailure.BeforeCommit = true;
            UpsertMapElementRequest command = this.Write(symbol.Id.ToString(), created.Element.Id, 1, "{\"type\":\"Point\",\"coordinates\":[10.26,51.51]}");
            ServiceResult result = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.True(result.Code == 200, result.ProblemTitle);
            Assert.Equal(1, this._commitFailure.Failures);
            this._db.ChangeTracker.Clear();
            MapElement element = await this._db.MapElements.SingleAsync();
            Assert.Equal(2, element.Version);
            Assert.Equal(command.GeometryJson, element.GeometryJson);
            Assert.Equal(3, (await this._db.Incidents.SingleAsync()).Revision);
            Assert.Equal(2, await this._db.MapAuditEvents.CountAsync());
            Assert.Equal(2, await this._db.OutboxMessages.CountAsync());
            Assert.Equal(2, await this._db.ProcessedCommands.CountAsync());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        /// <summary>
        /// Executes symbols Require A Call Name Without Persisting Partial Changes.
        /// </summary>
        /// <param name="callName">The call Name.</param>
        public async Task SymbolsRequireACallNameWithoutPersistingPartialChanges(string? callName)
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            UpsertMapElementRequest command = this.Write(symbol.Id.ToString());
            command.RadioCallName = callName;
            ServiceResult result = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.Equal(400, result.Code);
            Assert.Contains("Funkrufnamen", result.ProblemTitle);
            Assert.Empty(await this._db.MapElements.ToListAsync());
            Assert.Empty(await this._db.MapAuditEvents.ToListAsync());
            Assert.Empty(await this._db.OutboxMessages.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes symbol Details Are Persisted And The Call Name Cannot Be Cleared.
        /// </summary>
        public async Task SymbolDetailsArePersistedAndTheCallNameCannotBeCleared()
        {
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(), default)).Value);
            UpsertMapElementRequest command = this.Write(symbol.Id.ToString());
            command.RadioCallName = " Florian 40/1 ";
            command.Strength = "1/8/9";
            command.Note = "Wasserversorgung";
            ServiceResult result = await this._map.UpsertElement(this._incidentId, command, default);
            Assert.Equal(200, result.Code);
            MapElementDto element = Assert.IsType<MapElementResponse>(result.Value).Element;
            this._db.ChangeTracker.Clear();
            MapElement stored = await this._db.MapElements.SingleAsync();
            Assert.Equal("Florian 40/1", stored.RadioCallName);
            Assert.Equal(command.Strength, stored.Strength);
            Assert.Equal(command.Note, stored.Note);
            UpsertMapElementRequest update = this.Write(symbol.Id.ToString(), element.Id, element.Version);
            update.RadioCallName = " ";
            Assert.Equal(400, (await this._map.UpsertElement(this._incidentId, update, default)).Code);
            Assert.Equal("Florian 40/1", (await this._db.MapElements.SingleAsync()).RadioCallName);
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes custom Categories Persist And Can Be Used For Symbols.
        /// </summary>
        public async Task CustomCategoriesPersistAndCanBeUsedForSymbols()
        {
            ServiceResult created = await this._symbols.CreateCategory(new () { Name = "  Technische Hilfe  " }, default);
            Assert.Equal(201, created.Code);
            Assert.Equal("Technische Hilfe", created.Value);
            this._db.ChangeTracker.Clear();
            Assert.Contains("Technische Hilfe", Assert.IsAssignableFrom<IEnumerable<string>>((await this._symbols.GetCategories(default)).Value));
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, SymbolRequest(category: "Technische Hilfe"), default)).Value);
            Assert.Equal("Technische Hilfe", symbol.Category);
            Assert.True(symbol.IsVehicle);
            Assert.True(Assert.Single(Assert.IsAssignableFrom<IEnumerable<MapSymbolDto>>((await this._symbols.GetSymbols(default)).Value)).IsVehicle);
            Assert.Equal(409, (await this._symbols.CreateCategory(new () { Name = " technische hilfe " }, default)).Code);
            Assert.Equal(400, (await this._symbols.CreateCategory(new () { Name = " " }, default)).Code);
            Assert.Equal(400, (await this._symbols.CreateCategory(new () { Name = new string('a', 41) }, default)).Code);
            _ = this._user.SetupGet(x => x.IsAdmin).Returns(false);
            Assert.Equal(403, (await this._symbols.CreateCategory(new () { Name = "Nicht erlaubt" }, default)).Code);
            Assert.Equal(5, await this._db.MapSymbolCategories.CountAsync());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Unnötiger Funkrufname")]
        /// <summary>
        /// Executes non Vehicle Symbols Can Be Placed And Moved Without Details.
        /// </summary>
        /// <param name="callName">The call Name.</param>
        public async Task NonVehicleSymbolsCanBePlacedAndMovedWithoutDetails(string? callName)
        {
            SaveMapSymbolRequest request = SymbolRequest();
            request.IsVehicle = false;
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, request, default)).Value);
            UpsertMapElementRequest command = this.Write(symbol.Id.ToString());
            command.RadioCallName = callName;
            command.Strength = "1/8/9";
            command.Note = "Keine Eingabe";
            MapElementDto created = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, command, default)).Value).Element;
            Assert.False(created.IsVehicle);
            Assert.Null(created.RadioCallName);
            Assert.Null(created.Strength);
            Assert.Null(created.Note);
            UpsertMapElementRequest move = this.Write(symbol.Id.ToString(), created.Id, 1, "{\"type\":\"Point\",\"coordinates\":[10.26,51.51]}");
            move.RadioCallName = null;
            MapElementDto moved = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, move, default)).Value).Element;
            Assert.Equal(2, moved.Version);
            Assert.Null(moved.RadioCallName);
            this._db.ChangeTracker.Clear();
            Assert.False((await this._db.MapElements.SingleAsync()).IsVehicle);
            Assert.Equal(2, await this._db.MapAuditEvents.CountAsync());
        }

        [Fact]
        /// <summary>
        /// Executes vehicle Flag Is Editable And Placed Elements Keep Their Vehicle Rules.
        /// </summary>
        public async Task VehicleFlagIsEditableAndPlacedElementsKeepTheirVehicleRules()
        {
            SaveMapSymbolRequest request = SymbolRequest();
            MapSymbolDto symbol = Assert.IsType<MapSymbolDto>((await this._symbols.Save(null, request, default)).Value);
            MapElementDto created = Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, this.Write(symbol.Id.ToString()), default)).Value).Element;
            Assert.True(created.IsVehicle);
            request.IsVehicle = false;
            Assert.False(Assert.IsType<MapSymbolDto>((await this._symbols.Save(symbol.Id, request, default)).Value).IsVehicle);
            this._db.ChangeTracker.Clear();
            MapElementDto reloaded = Assert.Single(Assert.IsType<MapStateResponse>((await this._map.GetState(this._incidentId, default)).Value).Elements);
            Assert.True(reloaded.IsVehicle);
            UpsertMapElementRequest update = this.Write(symbol.Id.ToString(), reloaded.Id, reloaded.Version);
            update.RadioCallName = null;
            Assert.Equal(400, (await this._map.UpsertElement(this._incidentId, update, default)).Code);
            _ = Assert.Single(await this._db.MapAuditEvents.ToListAsync());
            UpsertMapElementRequest newPlacement = this.Write(symbol.Id.ToString());
            newPlacement.RadioCallName = null;
            Assert.False(Assert.IsType<MapElementResponse>((await this._map.UpsertElement(this._incidentId, newPlacement, default)).Value).Element.IsVehicle);
        }

        /// <summary>
        /// Executes dispose.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task DisposeAsync()
        {
            await this._db.DisposeAsync();
            await this._connection.DisposeAsync();
        }

        private static SaveMapSymbolRequest SymbolRequest(string svg = Svg, string category = "Feuerwehr")
            => new () { Name = "Löschfahrzeug", Category = category, SvgContent = svg, IsVehicle = true };

        private UpsertMapElementRequest Write(string symbolId, Guid? elementId = null, int? version = null, string geometry = "{\"type\":\"Point\",\"coordinates\":[10.25,51.5]}")
            => new ()
            {
                CommandId = Guid.NewGuid().ToString(),
                SessionId = "session-1",
                LeaseToken = "lease-1",
                SymbolId = symbolId,
                ElementId = elementId,
                ExpectedVersion = version,
                ElementType = MapElementType.Symbol,
                GeometryJson = geometry,
                RadioCallName = "Florian 1",
            };

        private sealed class TransientCommitFailure : DbTransactionInterceptor
        {
            public bool BeforeCommit { get; set; }

            public bool AfterCommit { get; set; }

            public int Failures { get; private set; }

            /// <summary>
            /// Executes transaction Committing.
            /// </summary>
            /// <param name="transaction">The transaction.</param>
            /// <param name="eventData">The event Data.</param>
            /// <param name="result">The result.</param>
            /// <param name="cancellationToken">The cancellation Token.</param>
            public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            {
                if (this.BeforeCommit)
                {
                    this.BeforeCommit = false;
                    this.Failures++;
                    throw new TimeoutException("Simulated connection failure before commit.");
                }

                return ValueTask.FromResult(result);
            }

            /// <summary>
            /// Executes transaction Committed.
            /// </summary>
            /// <param name="transaction">The transaction.</param>
            /// <param name="eventData">The event Data.</param>
            /// <param name="cancellationToken">The cancellation Token.</param>
            public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                if (this.AfterCommit)
                {
                    this.AfterCommit = false;
                    this.Failures++;
                    throw new TimeoutException("Simulated connection failure after commit.");
                }

                return Task.CompletedTask;
            }
        }

        // SQLite exercises transactions with the real Npgsql retry strategy (including its
        // transaction guard). This does not replace a PostgreSQL integration/sequence test.
        private sealed class SqliteTestContext(DbContextOptions<FeuerwehrDbContext> options) : FeuerwehrDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                foreach (IMutableSequence? sequence in modelBuilder.Model.GetSequences().ToList())
                {
                    _ = modelBuilder.Model.RemoveSequence(sequence.Name, sequence.Schema);
                }
            }
        }
    }
}
