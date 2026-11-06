using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Controller.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller
{
    /// <summary>
    /// Tests incident controller behavior and its expected outcomes.
    /// </summary>
    public sealed class IncidentControllerTests : IDisposable
    {
        private readonly FeuerwehrDbContext _db = new (new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private readonly IncidentController _controller;
        private readonly IncidentService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentControllerTests" /> class with its required dependencies.
        /// </summary>
        public IncidentControllerTests()
        {
            Mock<IUserContextAccessor> user = new ();
            _ = user.SetupGet(x => x.UserId).Returns("user-1");
            _ = user.SetupGet(x => x.DisplayName).Returns("Ada Muster");
            _ = user.SetupGet(x => x.IsAdmin).Returns(true);
            this._service = new IncidentService(this._db, user.Object, new CommandIdempotencyService(this._db), new OutboxService(this._db));
            this._controller = new IncidentController(this._service);
        }

        /// <summary>
        /// Verifies that create returns persisted incident with alarm time and lease and active returns same incident.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Create_ReturnsPersistedIncidentWithAlarmTimeAndLease_AndActiveReturnsSameIncident()
        {
            CreateIncidentRequest request = Request();
            ObjectResult result = Assert.IsType<ObjectResult>(await this._controller.CreateIncident(request, default));
            Assert.Equal(201, result.StatusCode);
            IncidentCreatedResponse created = Assert.IsType<IncidentCreatedResponse>(result.Value);
            Assert.Equal(request.Name, created.Incident.Name);
            Assert.Equal(request.AlarmedAtUtc!.Value.UtcDateTime, created.Incident.AlarmedAtUtc);
            Assert.Equal(DateTimeKind.Utc, created.Incident.AlarmedAtUtc.Kind);
            Assert.Equal(IncidentStatus.Active, created.Incident.Status);
            Assert.Equal(request.SessionId, created.Lease.SessionId);
            Assert.False(string.IsNullOrWhiteSpace(created.Lease.LeaseToken));
            Assert.Equal(created.Incident.AlarmedAtUtc, (await this._db.Incidents.SingleAsync()).AlarmedAtUtc);
            Assert.Equal(2, await this._db.OutboxMessages.CountAsync());

            ActionResult<IncidentSummaryDto> activeResult = await this._controller.GetActiveIncident(default);
            IncidentSummaryDto active = Assert.IsType<IncidentSummaryDto>(Assert.IsType<OkObjectResult>(activeResult.Result).Value);
            Assert.Equal(created.Incident, active);

            // MVC and stored command replies both use numeric enums. The web client must accept these.
            using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(created, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            Assert.Equal(1, json.RootElement.GetProperty("incident").GetProperty("status").GetInt32());
            Assert.Equal("2026-10-26T08:15:00Z", json.RootElement.GetProperty("incident").GetProperty("alarmedAtUtc").GetString());
        }

        /// <summary>
        /// Verifies that create replay returns identical body without duplicating incident or events.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Create_ReplayReturnsIdenticalBodyWithoutDuplicatingIncidentOrEvents()
        {
            CreateIncidentRequest request = Request();
            IncidentCreatedResponse original = Assert.IsType<IncidentCreatedResponse>((await this._service.CreateIncident(request, default)).Value);
            request.AlarmedAtUtc = request.AlarmedAtUtc!.Value.ToUniversalTime();
            ServiceResult replay = await this._service.CreateIncident(request, default);
            Assert.Equal(201, replay.Code);
            Assert.Equal(JsonSerializer.Serialize(original, JsonOptionsFactory.Default), Assert.IsType<JsonElement>(replay.Value).GetRawText());
            _ = Assert.Single(await this._db.Incidents.ToListAsync());
            _ = Assert.Single(await this._db.EditorLeases.ToListAsync());
            Assert.Equal(2, await this._db.OutboxMessages.CountAsync());
        }

        /// <summary>
        /// Verifies that create command payload includes alarm time.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Create_CommandPayloadIncludesAlarmTime()
        {
            CreateIncidentRequest request = Request();
            _ = await this._service.CreateIncident(request, default);
            request.AlarmedAtUtc = request.AlarmedAtUtc!.Value.AddMinutes(-1);
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => this._service.CreateIncident(request, default));
            _ = Assert.Single(await this._db.Incidents.ToListAsync());
        }

        /// <summary>
        /// Verifies that create missing alarm time is rejected by contract and service.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Create_MissingAlarmTimeIsRejectedByContractAndService()
        {
            CreateIncidentRequest request = Request();
            request.AlarmedAtUtc = null;
            List<ValidationResult> validation = new ();
            Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), validation, validateAllProperties: true));
            Assert.Contains(validation, result => result.MemberNames.Contains(nameof(CreateIncidentRequest.AlarmedAtUtc)));
            Assert.Equal(400, (await this._service.CreateIncident(request, default)).Code);
            Assert.Empty(await this._db.Incidents.ToListAsync());
        }

        /// <summary>
        /// Verifies that close preserves alarm time in archive and removes active incident.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Close_PreservesAlarmTimeInArchiveAndRemovesActiveIncident()
        {
            IncidentCreatedResponse created = Assert.IsType<IncidentCreatedResponse>((await this._service.CreateIncident(Request(), default)).Value);
            ServiceResult closed = await this._service.CloseIncident(
                created.Incident.Id,
                new CloseIncidentRequest
                {
                    SessionId = created.Lease.SessionId,
                    LeaseToken = created.Lease.LeaseToken,
                    CommandId = Guid.NewGuid().ToString(),
                }, default);
            IncidentClosedResponse response = Assert.IsType<IncidentClosedResponse>(closed.Value);
            Assert.Equal(created.Incident.AlarmedAtUtc, response.Incident.AlarmedAtUtc);
            Assert.Equal(IncidentStatus.Closed, response.Incident.Status);
            Assert.Equal(404, (await this._service.GetActiveIncident(default)).Code);
            IEnumerable<IncidentSummaryDto> archive = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await this._service.GetArchive(cancellationToken: default)).Value);
            Assert.Equal(created.Incident.AlarmedAtUtc, Assert.Single(archive).AlarmedAtUtc);
        }

        /// <summary>
        /// Releases the resources owned by this incident controller component.
        /// </summary>
        public void Dispose() => this._db.Dispose();

        private static CreateIncidentRequest Request() => new ()
        {
            Name = "Wohnungsbrand",
            Keyword = "B3",
            IncidentNumber = "123",
            OriginLatitude = 51.5,
            OriginLongitude = 10.25,
            AlarmedAtUtc = DateTimeOffset.Parse("2026-10-26T09:15:00+01:00"),
            CommandId = Guid.NewGuid().ToString(),
            SessionId = "session-1",
        };
    }
}
