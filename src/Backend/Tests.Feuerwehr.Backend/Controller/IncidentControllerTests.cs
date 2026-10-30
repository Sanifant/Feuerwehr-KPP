using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Controller.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller;

public sealed class IncidentControllerTests : IDisposable
{
    private readonly FeuerwehrDbContext _db = new(new DbContextOptionsBuilder<FeuerwehrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly IncidentController _controller;
    private readonly IncidentService _service;

    public IncidentControllerTests()
    {
        var user = new Mock<IUserContextAccessor>();
        user.SetupGet(x => x.UserId).Returns("user-1");
        user.SetupGet(x => x.DisplayName).Returns("Ada Muster");
        user.SetupGet(x => x.IsAdmin).Returns(true);
        _service = new IncidentService(_db, user.Object, new CommandIdempotencyService(_db), new OutboxService(_db));
        _controller = new IncidentController(_service);
    }

    private static CreateIncidentRequest Request() => new()
    {
        Name = "Wohnungsbrand", Keyword = "B3", IncidentNumber = "123",
        OriginLatitude = 51.5, OriginLongitude = 10.25,
        AlarmedAtUtc = DateTimeOffset.Parse("2026-10-26T09:15:00+01:00"),
        CommandId = Guid.NewGuid().ToString(), SessionId = "session-1",
    };

    [Fact]
    /// <summary>
    /// Creates the returns Persisted Incident With Alarm Time And Lease And Active Returns Same Incident.
    /// </summary>
    public async Task Create_ReturnsPersistedIncidentWithAlarmTimeAndLease_AndActiveReturnsSameIncident()
    {
        var request = Request();
        var result = Assert.IsType<ObjectResult>(await _controller.CreateIncident(request, default));
        Assert.Equal(201, result.StatusCode);
        var created = Assert.IsType<IncidentCreatedResponse>(result.Value);
        Assert.Equal(request.Name, created.Incident.Name);
        Assert.Equal(request.AlarmedAtUtc!.Value.UtcDateTime, created.Incident.AlarmedAtUtc);
        Assert.Equal(DateTimeKind.Utc, created.Incident.AlarmedAtUtc.Kind);
        Assert.Equal(IncidentStatus.Active, created.Incident.Status);
        Assert.Equal(request.SessionId, created.Lease.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(created.Lease.LeaseToken));
        Assert.Equal(created.Incident.AlarmedAtUtc, (await _db.Incidents.SingleAsync()).AlarmedAtUtc);
        Assert.Equal(2, await _db.OutboxMessages.CountAsync());

        var activeResult = await _controller.GetActiveIncident(default);
        var active = Assert.IsType<IncidentSummaryDto>(Assert.IsType<OkObjectResult>(activeResult.Result).Value);
        Assert.Equal(created.Incident, active);

        // MVC and stored command replies both use numeric enums. The web client must accept these.
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(created, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(1, json.RootElement.GetProperty("incident").GetProperty("status").GetInt32());
        Assert.Equal("2026-10-26T08:15:00Z", json.RootElement.GetProperty("incident").GetProperty("alarmedAtUtc").GetString());
    }

    [Fact]
    /// <summary>
    /// Creates the replay Returns Identical Body Without Duplicating Incident Or Events.
    /// </summary>
    public async Task Create_ReplayReturnsIdenticalBodyWithoutDuplicatingIncidentOrEvents()
    {
        var request = Request();
        var original = Assert.IsType<IncidentCreatedResponse>((await _service.CreateIncident(request, default)).Value);
        // The same point in time with a different offset has the same command payload hash.
        request.AlarmedAtUtc = request.AlarmedAtUtc!.Value.ToUniversalTime();
        var replay = await _service.CreateIncident(request, default);
        Assert.Equal(201, replay.Code);
        Assert.Equal(JsonSerializer.Serialize(original, JsonOptionsFactory.Default), Assert.IsType<JsonElement>(replay.Value).GetRawText());
        Assert.Single(await _db.Incidents.ToListAsync());
        Assert.Single(await _db.EditorLeases.ToListAsync());
        Assert.Equal(2, await _db.OutboxMessages.CountAsync());
    }

    [Fact]
    /// <summary>
    /// Creates the command Payload Includes Alarm Time.
    /// </summary>
    public async Task Create_CommandPayloadIncludesAlarmTime()
    {
        var request = Request();
        await _service.CreateIncident(request, default);
        request.AlarmedAtUtc = request.AlarmedAtUtc!.Value.AddMinutes(-1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateIncident(request, default));
        Assert.Single(await _db.Incidents.ToListAsync());
    }

    [Fact]
    /// <summary>
    /// Creates the missing Alarm Time Is Rejected By Contract And Service.
    /// </summary>
    public async Task Create_MissingAlarmTimeIsRejectedByContractAndService()
    {
        var request = Request();
        request.AlarmedAtUtc = null;
        var validation = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), validation, validateAllProperties: true));
        Assert.Contains(validation, result => result.MemberNames.Contains(nameof(CreateIncidentRequest.AlarmedAtUtc)));
        Assert.Equal(400, (await _service.CreateIncident(request, default)).Code);
        Assert.Empty(await _db.Incidents.ToListAsync());
    }

    [Fact]
    /// <summary>
    /// Executes close Preserves Alarm Time In Archive And Removes Active Incident.
    /// </summary>
    public async Task Close_PreservesAlarmTimeInArchiveAndRemovesActiveIncident()
    {
        var created = Assert.IsType<IncidentCreatedResponse>((await _service.CreateIncident(Request(), default)).Value);
        var closed = await _service.CloseIncident(created.Incident.Id, new CloseIncidentRequest
        {
            SessionId = created.Lease.SessionId, LeaseToken = created.Lease.LeaseToken, CommandId = Guid.NewGuid().ToString(),
        }, default);
        var response = Assert.IsType<IncidentClosedResponse>(closed.Value);
        Assert.Equal(created.Incident.AlarmedAtUtc, response.Incident.AlarmedAtUtc);
        Assert.Equal(IncidentStatus.Closed, response.Incident.Status);
        Assert.Equal(404, (await _service.GetActiveIncident(default)).Code);
        var archive = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await _service.GetArchive(cancellationToken: default)).Value);
        Assert.Equal(created.Incident.AlarmedAtUtc, Assert.Single(archive).AlarmedAtUtc);
    }

    /// <summary>
    /// Executes dispose.
    /// </summary>
    public void Dispose() => _db.Dispose();
}

