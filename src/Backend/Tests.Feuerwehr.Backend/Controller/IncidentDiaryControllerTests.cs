using System.Text;
using System.Text.Json;
using global::Feuerwehr.Server.Controller.IncidentModules;
using global::Feuerwehr.Server.Data;
using global::Feuerwehr.Server.Models.IncidentModules;
using global::Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Feuerwehr.Backend.Controller;

public class IncidentDiaryControllerTests
{
    private static FeuerwehrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new FeuerwehrDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Mock<IUserContextAccessor> CreateUserContextMock(string userId = "user-1", string displayName = "Ada Admin")
    {
        var mock = new Mock<IUserContextAccessor>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.SetupGet(x => x.DisplayName).Returns(displayName);
        return mock;
    }

    private static Mock<ICommandIdempotencyService> CreateIdempotencyMock()
    {
        var mock = new Mock<ICommandIdempotencyService>();
        mock.Setup(x => x.FindExistingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(((ProcessedCommand?)null, "payload-hash")));
        mock.Setup(x => x.BuildStoredCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CommandEnvelopeResponse>(), It.IsAny<string>()))
            .Returns<string, string, string?, string, CommandEnvelopeResponse, string>((commandId, scope, targetId, payloadHash, response, userId) => new ProcessedCommand
            {
                Id = Guid.NewGuid(),
                CommandId = commandId,
                Scope = scope,
                TargetId = targetId,
                PayloadHash = payloadHash,
                ResponseStatusCode = response.StatusCode,
                ResponseJson = response.JsonPayload,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
            });

        return mock;
    }

    private static IncidentDiaryController CreateController(
        FeuerwehrDbContext dbContext,
        Mock<IUserContextAccessor>? userContextMock = null,
        Mock<ICommandIdempotencyService>? idempotencyMock = null,
        Mock<IOutboxService>? outboxMock = null)
    {
        return new IncidentDiaryController(
            dbContext,
            (userContextMock ?? CreateUserContextMock()).Object,
            (idempotencyMock ?? CreateIdempotencyMock()).Object,
            (outboxMock ?? new Mock<IOutboxService>()).Object);
    }

    private static Incident CreateIncident(Guid incidentId, string name = "Warehouse Fire", IncidentStatus status = IncidentStatus.Active, long revision = 1)
        => new()
        {
            Id = incidentId,
            Name = name,
            Status = status,
            Revision = revision,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
        };

    private static DiaryCategory CreateCategory(string code, string name, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

    private static DiaryEntry CreateEntry(Guid incidentId, long entryNumber = 1, int currentRevision = 1, bool isCanceled = false, string? cancellationReason = null)
        => new()
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            EntryNumber = entryNumber,
            CurrentRevision = currentRevision,
            IsCanceled = isCanceled,
            CancellationReason = cancellationReason,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
        };

    private static DiaryEntryRevision CreateRevision(
        DiaryEntry entry,
        int revisionNumber,
        DiaryRevisionType revisionType,
        string categoryCode = "RADIO",
        string categoryName = "Radio",
        string text = "Initial entry",
        string? cancellationReason = null)
        => new()
        {
            Id = Guid.NewGuid(),
            DiaryEntryId = entry.Id,
            DiaryEntry = entry,
            RevisionNumber = revisionNumber,
            RevisionType = revisionType,
            EventTimestampUtc = new DateTime(2025, 1, 5, 13, 0, 0, DateTimeKind.Utc).AddMinutes(revisionNumber),
            CategoryCode = categoryCode,
            CategoryNameSnapshot = categoryName,
            Text = text,
            Sender = "Dispatch",
            Recipient = "Unit 1",
            TransmissionType = "Radio",
            CancellationReason = cancellationReason,
            CreatedAtUtc = new DateTime(2025, 1, 5, 13, 0, 0, DateTimeKind.Utc).AddMinutes(revisionNumber),
            CreatedByUserId = "user-1",
            CreatedByDisplayName = "Ada Admin",
        };

    [Fact]
    public async Task GetEntries_ReturnsOrderedEntriesWithLatestRevisions()
    {
        await using var dbContext = CreateDbContext();
        var incidentId = Guid.NewGuid();
        var firstEntry = CreateEntry(incidentId, entryNumber: 2, currentRevision: 2);
        var secondEntry = CreateEntry(incidentId, entryNumber: 1, currentRevision: 1);
        var entryWithoutRevision = CreateEntry(incidentId, entryNumber: 3, currentRevision: 1);

        dbContext.DiaryEntries.AddRange(firstEntry, secondEntry, entryWithoutRevision);
        dbContext.DiaryEntryRevisions.AddRange(
            CreateRevision(firstEntry, 1, DiaryRevisionType.Created, text: "Initial note"),
            CreateRevision(firstEntry, 2, DiaryRevisionType.Edited, text: "Updated note"),
            CreateRevision(secondEntry, 1, DiaryRevisionType.Created, text: "First chronological note"));
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.GetEntries(incidentId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var entries = Assert.IsType<List<DiaryEntryDto>>(okResult.Value);
        Assert.Collection(
            entries,
            entry =>
            {
                Assert.Equal(secondEntry.Id, entry.Id);
                Assert.Equal(1, entry.CurrentRevisionData.RevisionNumber);
                Assert.Equal("First chronological note", entry.CurrentRevisionData.Text);
                Assert.Single(entry.Revisions);
            },
            entry =>
            {
                Assert.Equal(firstEntry.Id, entry.Id);
                Assert.Equal(2, entry.CurrentRevisionData.RevisionNumber);
                Assert.Equal(DiaryRevisionType.Edited, entry.CurrentRevisionData.RevisionType);
                Assert.Equal("Updated note", entry.CurrentRevisionData.Text);
                Assert.Equal(2, entry.Revisions.Count);
            });
    }

    [Fact]
    public async Task GetCategories_ReturnsCategoriesOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        dbContext.DiaryCategories.AddRange(
            CreateCategory("STATUS", "Statusmeldung"),
            CreateCategory("ALARM", "Alarmierung"),
            CreateCategory("ZUSATZ", "Zusatzinfo", isActive: false));
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.GetCategories(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsType<List<DiaryCategoryDto>>(okResult.Value);
        Assert.Collection(
            categories,
            category => Assert.Equal("Alarmierung", category.Name),
            category => Assert.Equal("Statusmeldung", category.Name),
            category => Assert.Equal("Zusatzinfo", category.Name));
    }

    /*
    [Fact]
    public async Task CreateEntry_ReturnsValidationProblem_WhenModelStateIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        controller.ModelState.AddModelError("Text", "Required");

        var result = await controller.CreateEntry(
            Guid.NewGuid(),
            new CreateDiaryEntryRequest { CommandId = "cmd-1", CategoryCode = "RADIO", Text = "Ignored" },
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
    }
    */

    [Fact]
    public async Task CreateEntry_ReturnsStoredResponse_WhenCommandWasAlreadyProcessed()
    {
        await using var dbContext = CreateDbContext();
        var existingEntryId = Guid.NewGuid();
        var existingResponse = new ProcessedCommand
        {
            Id = Guid.NewGuid(),
            CommandId = "cmd-1",
            UserId = "user-1",
            Scope = "diary.entry.create",
            TargetId = existingEntryId.ToString(),
            PayloadHash = "hash",
            ResponseStatusCode = StatusCodes.Status201Created,
            ResponseJson = JsonSerializer.Serialize(new { entryId = existingEntryId, entryNumber = 42L }),
            CreatedAtUtc = DateTime.UtcNow,
        };

        var idempotencyMock = new Mock<ICommandIdempotencyService>();
        idempotencyMock
            .Setup(x => x.FindExistingAsync(It.IsAny<string>(), "diary.entry.create", It.IsAny<string?>(), It.IsAny<object>(), "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((existingResponse, "payload-hash")));

        var controller = CreateController(dbContext, idempotencyMock: idempotencyMock);

        var result = await controller.CreateEntry(
            Guid.NewGuid(),
            new CreateDiaryEntryRequest
            {
                CommandId = "cmd-1",
                CategoryCode = "RADIO",
                Text = "Existing",
                EventTimestampUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<JsonElement>(objectResult.Value);
        Assert.Equal(existingEntryId, payload.GetProperty("entryId").GetGuid());
        Assert.Equal(42L, payload.GetProperty("entryNumber").GetInt64());
    }

    [Fact]
    public async Task UpdateEntry_ReturnsConflict_WhenExpectedRevisionDoesNotMatch()
    {
        await using var dbContext = CreateDbContext();
        var incidentId = Guid.NewGuid();
        var incident = CreateIncident(incidentId, revision: 4);
        var entry = CreateEntry(incidentId, entryNumber: 7, currentRevision: 2);
        dbContext.Incidents.Add(incident);
        dbContext.DiaryEntries.Add(entry);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.UpdateEntry(
            incidentId,
            entry.Id,
            new UpdateDiaryEntryRequest
            {
                CommandId = "cmd-2",
                ExpectedRevision = 1,
                CategoryCode = "RADIO",
                Text = "Update",
                EventTimestampUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var details = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Konflikt: veraltete Tagebuchversion.", details.Title);
    }

    [Fact]
    public async Task UpdateEntry_PersistsNewRevision_WhenRequestIsValid()
    {
        await using var dbContext = CreateDbContext();
        var incidentId = Guid.NewGuid();
        var incident = CreateIncident(incidentId, revision: 7);
        var category = CreateCategory("STATUS", "Statusmeldung");
        var entry = CreateEntry(incidentId, entryNumber: 5, currentRevision: 1);
        var existingRevision = CreateRevision(entry, 1, DiaryRevisionType.Created, categoryCode: category.Code, categoryName: category.Name, text: "Initial note");

        dbContext.Incidents.Add(incident);
        dbContext.DiaryCategories.Add(category);
        dbContext.DiaryEntries.Add(entry);
        dbContext.DiaryEntryRevisions.Add(existingRevision);
        await dbContext.SaveChangesAsync();

        var outboxMock = new Mock<IOutboxService>();
        var controller = CreateController(dbContext, outboxMock: outboxMock);

        var result = await controller.UpdateEntry(
            incidentId,
            entry.Id,
            new UpdateDiaryEntryRequest
            {
                CommandId = "cmd-3",
                ExpectedRevision = 1,
                CategoryCode = category.Code,
                Text = "Corrected note",
                Sender = "Control",
                Recipient = "Unit 7",
                TransmissionType = "Phone",
                EventTimestampUtc = new DateTime(2025, 1, 6, 12, 0, 0, DateTimeKind.Utc),
            },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = ToJsonElement(okResult.Value!);
        Assert.Equal(entry.Id, payload.GetProperty("entryId").GetGuid());
        Assert.Equal(2, payload.GetProperty("currentRevision").GetInt32());
        Assert.Equal(8, payload.GetProperty("incidentRevision").GetInt64());

        Assert.Equal(2, entry.CurrentRevision);
        Assert.Equal(8, incident.Revision);
        var newRevision = Assert.Single(dbContext.DiaryEntryRevisions.Where(x => x.DiaryEntryId == entry.Id && x.RevisionNumber == 2));
        Assert.Equal(DiaryRevisionType.Edited, newRevision.RevisionType);
        Assert.Equal("Corrected note", newRevision.Text);
        Assert.Equal("Control", newRevision.Sender);
        Assert.Equal("Phone", newRevision.TransmissionType);
        Assert.Single(dbContext.ProcessedCommands);
        outboxMock.Verify(x => x.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CancelEntry_ReturnsBadRequest_WhenReasonIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.CancelEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CancelDiaryEntryRequest { CommandId = "cmd-4", ExpectedRevision = 1, Reason = "   " },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var details = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Stornierungsbegründung ist erforderlich.", details.Title);
    }

    [Fact]
    public async Task CancelEntry_PersistsCancellationRevision_WhenRequestIsValid()
    {
        await using var dbContext = CreateDbContext();
        var incidentId = Guid.NewGuid();
        var incident = CreateIncident(incidentId, revision: 3);
        var entry = CreateEntry(incidentId, entryNumber: 9, currentRevision: 1);
        var previousRevision = CreateRevision(entry, 1, DiaryRevisionType.Created, categoryCode: "RADIO", categoryName: "Radio", text: "Original note");

        dbContext.Incidents.Add(incident);
        dbContext.DiaryEntries.Add(entry);
        dbContext.DiaryEntryRevisions.Add(previousRevision);
        await dbContext.SaveChangesAsync();

        var outboxMock = new Mock<IOutboxService>();
        var controller = CreateController(dbContext, outboxMock: outboxMock);

        var result = await controller.CancelEntry(
            incidentId,
            entry.Id,
            new CancelDiaryEntryRequest
            {
                CommandId = "cmd-5",
                ExpectedRevision = 1,
                Reason = "  Duplicate entry  ",
            },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = ToJsonElement(okResult.Value!);
        Assert.Equal(entry.Id, payload.GetProperty("entryId").GetGuid());
        Assert.True(payload.GetProperty("canceled").GetBoolean());
        Assert.Equal(2, payload.GetProperty("currentRevision").GetInt32());
        Assert.Equal(4, payload.GetProperty("incidentRevision").GetInt64());

        Assert.True(entry.IsCanceled);
        Assert.Equal("Duplicate entry", entry.CancellationReason);
        Assert.Equal(2, entry.CurrentRevision);
        Assert.Equal(4, incident.Revision);

        var cancellationRevision = Assert.Single(dbContext.DiaryEntryRevisions.Where(x => x.DiaryEntryId == entry.Id && x.RevisionType == DiaryRevisionType.Canceled));
        Assert.Equal(2, cancellationRevision.RevisionNumber);
        Assert.Equal("Original note", cancellationRevision.Text);
        Assert.Equal("Duplicate entry", cancellationRevision.CancellationReason);
        Assert.Single(dbContext.ProcessedCommands);
        outboxMock.Verify(x => x.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ExportDiaryPdf_ReturnsNotFound_WhenIncidentDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.ExportDiaryPdf(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    /*
    [Fact]
    public async Task ExportDiaryPdf_ReturnsPdfFile_WithDiaryContent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FeuerwehrDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var incidentId = Guid.NewGuid();
        var incident = CreateIncident(incidentId, name: "Warehouse Fire", revision: 2);
        var firstEntry = CreateEntry(incidentId, entryNumber: 1, currentRevision: 1);
        var secondEntry = CreateEntry(incidentId, entryNumber: 2, currentRevision: 2, isCanceled: true, cancellationReason: "Duplicate");

        dbContext.Incidents.Add(incident);
        dbContext.DiaryEntries.AddRange(firstEntry, secondEntry);
        dbContext.DiaryEntryRevisions.AddRange(
            CreateRevision(firstEntry, 1, DiaryRevisionType.Created, categoryName: "Radio", text: "Initial radio traffic"),
            CreateRevision(secondEntry, 1, DiaryRevisionType.Created, categoryName: "Status", text: "Status before cancel"),
            CreateRevision(secondEntry, 2, DiaryRevisionType.Canceled, categoryName: "Status", text: "Status before cancel", cancellationReason: "Duplicate"));
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.ExportDiaryPdf(incidentId, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal($"einsatztagebuch-{incidentId:N}.pdf", fileResult.FileDownloadName);
        Assert.NotEmpty(fileResult.FileContents);

        var pdfText = Encoding.ASCII.GetString(fileResult.FileContents);
        Assert.Contains("%PDF-1.4", pdfText);
        Assert.Contains("Einsatztagebuch", pdfText);
        Assert.Contains("Einsatz: Warehouse Fire", pdfText);
        Assert.Contains("#1", pdfText);
        Assert.Contains("Initial radio traffic", pdfText);
        Assert.Contains("[STORNIERT]", pdfText);
        Assert.Contains("Grund: Duplicate", pdfText);
    }
    */

    private static JsonElement ToJsonElement(object value)
        => JsonSerializer.SerializeToElement(value);
}
