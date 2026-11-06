using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using global::De.Openelp.Feuerwehr.Server.Controller.IncidentModules;
using global::De.Openelp.Feuerwehr.Server.Data;
using global::De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using global::De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Controller
{
    /// <summary>
    /// Tests incident diary controller behavior and its expected outcomes.
    /// </summary>
    public class IncidentDiaryControllerTests
    {
        /// <summary>
        /// Creates the PostgreSQL database context used by Entity Framework design-time tooling.
        /// </summary>
        /// <returns>The configured PostgreSQL database context.</returns>
        public static FeuerwehrDbContext CreateDbContext()
        {
            DbContextOptions<FeuerwehrDbContext> options = new DbContextOptionsBuilder<FeuerwehrDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            FeuerwehrDbContext context = new (options);
            _ = context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Creates the caller context mock used to exercise diary authorization.
        /// </summary>
        /// <param name="userId">The identifier of the user account.</param>
        /// <param name="displayName">The name displayed for the incident diary controller.</param>
        /// <returns>The configured mock used by the test scenario.</returns>
        public static Mock<IUserContextAccessor> CreateUserContextMock(string userId = "user-1", string displayName = "Ada Admin")
        {
            Mock<IUserContextAccessor> mock = new ();
            _ = mock.SetupGet(x => x.UserId).Returns(userId);
            _ = mock.SetupGet(x => x.DisplayName).Returns(displayName);
            return mock;
        }

        /// <summary>
        /// Creates a command-store mock with replay behavior suitable for the diary scenario.
        /// </summary>
        /// <returns>The configured mock used by the test scenario.</returns>
        public static Mock<ICommandIdempotencyService> CreateIdempotencyMock()
        {
            Mock<ICommandIdempotencyService> mock = new ();
            _ = mock.Setup(x => x.FindExistingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(((ProcessedCommand?)null, "payload-hash")));
            _ = mock.Setup(x => x.BuildStoredCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CommandEnvelopeResponse>(), It.IsAny<string>()))
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

        /// <summary>
        /// Creates a controller with the mocked dependencies and caller context required by the test.
        /// </summary>
        /// <param name="dbContext">The database context for the current unit of work.</param>
        /// <param name="userContextMock">The optional caller-context mock used to configure user permissions.</param>
        /// <param name="idempotencyMock">The optional command-store mock used to configure replay behavior.</param>
        /// <param name="outboxMock">The optional outbox mock used to verify live-event staging.</param>
        /// <returns>The resolved view, or a diagnostic control if the matching view type cannot be found.</returns>
        public static IncidentDiaryController CreateController(
            FeuerwehrDbContext dbContext,
            Mock<IUserContextAccessor>? userContextMock = null,
            Mock<ICommandIdempotencyService>? idempotencyMock = null,
            Mock<IOutboxService>? outboxMock = null)
        {
            return new IncidentDiaryController(new IncidentDiaryService(
                dbContext,
                (userContextMock ?? CreateUserContextMock()).Object,
                (idempotencyMock ?? CreateIdempotencyMock()).Object,
                (outboxMock ?? new Mock<IOutboxService>()).Object));
        }

        /// <summary>
        /// Creates an incident in the fixture for the current test scenario.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="name">The display name of the incident diary controller.</param>
        /// <param name="status">The current state of the incident diary controller.</param>
        /// <param name="revision">The revision counter for the incident diary controller.</param>
        /// <returns>The incident prepared for the current test scenario.</returns>
        public static Incident CreateIncident(Guid incidentId, string name = "Warehouse Fire", IncidentStatus status = IncidentStatus.Active, long revision = 1)
            => new ()
            {
                Id = incidentId,
                Name = name,
                Status = status,
                Revision = revision,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            };

        /// <summary>
        /// Creates a diary category in the fixture for the current test scenario.
        /// </summary>
        /// <param name="code">The HTTP status code of the application outcome.</param>
        /// <param name="name">The display name of the incident diary controller.</param>
        /// <param name="isActive">The incident diary controller is active.</param>
        /// <returns>The diary category prepared for the current test scenario.</returns>
        public static DiaryCategory CreateCategory(string code, string name, bool isActive = true)
            => new ()
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                IsActive = isActive,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            };

        /// <summary>
        /// Creates a diary entry in the fixture for the current test scenario.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="entryNumber">The sequential entry number within the incident diary.</param>
        /// <param name="currentRevision">The latest revision number of the diary entry.</param>
        /// <param name="isCanceled">The diary entry has been canceled while preserving its revision history.</param>
        /// <param name="cancellationReason">The reason recorded when canceling the diary entry.</param>
        /// <returns>The diary entry prepared for the current test scenario.</returns>
        public static DiaryEntry CreateEntry(Guid incidentId, long entryNumber = 1, int currentRevision = 1, bool isCanceled = false, string? cancellationReason = null)
            => new ()
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

        /// <summary>
        /// Creates a diary revision with the supplied content for test assertions.
        /// </summary>
        /// <param name="entry">The diary entry used as the source of revision data.</param>
        /// <param name="revisionNumber">The sequential number of this diary revision.</param>
        /// <param name="revisionType">The kind of diary revision: creation, update, or cancellation.</param>
        /// <param name="categoryCode">The category code used in equipment barcode allocation.</param>
        /// <param name="categoryName">The category's display name.</param>
        /// <param name="text">The text content of the incident diary controller.</param>
        /// <param name="cancellationReason">The reason recorded when canceling the diary entry.</param>
        /// <returns>The diary entry revision prepared for the current test scenario.</returns>
        public static DiaryEntryRevision CreateRevision(
            DiaryEntry entry,
            int revisionNumber,
            DiaryRevisionType revisionType,
            string categoryCode = "RADIO",
            string categoryName = "Radio",
            string text = "Initial entry",
            string? cancellationReason = null)
            => new ()
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

        /// <summary>
        /// Verifies that get entries returns ordered entries with latest revisions.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task GetEntries_ReturnsOrderedEntriesWithLatestRevisions()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            Guid incidentId = Guid.NewGuid();
            DiaryEntry firstEntry = CreateEntry(incidentId, entryNumber: 2, currentRevision: 2);
            _ = dbContext.Incidents.Add(CreateIncident(incidentId));
            DiaryEntry secondEntry = CreateEntry(incidentId, entryNumber: 1, currentRevision: 1);
            DiaryEntry entryWithoutRevision = CreateEntry(incidentId, entryNumber: 3, currentRevision: 1);

            dbContext.DiaryEntries.AddRange(firstEntry, secondEntry, entryWithoutRevision);
            dbContext.DiaryEntryRevisions.AddRange(
                CreateRevision(firstEntry, 1, DiaryRevisionType.Created, text: "Initial note"),
                CreateRevision(firstEntry, 2, DiaryRevisionType.Edited, text: "Updated note"),
                CreateRevision(secondEntry, 1, DiaryRevisionType.Created, text: "First chronological note"));
            _ = await dbContext.SaveChangesAsync();

            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.GetEntries(incidentId, CancellationToken.None);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<DiaryEntryDto> entries = Assert.IsType<List<DiaryEntryDto>>(okResult.Value);
            Assert.Collection(
                entries,
                entry =>
                {
                    Assert.Equal(secondEntry.Id, entry.Id);
                    Assert.Equal(1, entry.CurrentRevisionData.RevisionNumber);
                    Assert.Equal("First chronological note", entry.CurrentRevisionData.Text);
                    _ = Assert.Single(entry.Revisions);
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

        /// <summary>
        /// Verifies that get categories returns categories ordered by name.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task GetCategories_ReturnsCategoriesOrderedByName()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            dbContext.DiaryCategories.AddRange(
                CreateCategory("STATUS", "Statusmeldung"),
                CreateCategory("ALARM", "Alarmierung"),
                CreateCategory("ZUSATZ", "Zusatzinfo", isActive: false));
            _ = await dbContext.SaveChangesAsync();

            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.GetCategories(CancellationToken.None);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<DiaryCategoryDto> categories = Assert.IsType<List<DiaryCategoryDto>>(okResult.Value);
            Assert.Collection(
                categories,
                category => Assert.Equal("Alarmierung", category.Name),
                category => Assert.Equal("Statusmeldung", category.Name),
                category => Assert.Equal("Zusatzinfo", category.Name));
        }

        /*
        [Fact]
        /// <summary>
        /// Creates the entry Returns Validation Problem When Model State Is Invalid.
        /// </summary>
        public async Task CreateEntry_ReturnsValidationProblem_WhenModelStateIsInvalid()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            IncidentDiaryController controller = CreateController(dbContext);
            controller.ModelState.AddModelError("Text", "Required");

            IActionResult result = await controller.CreateEntry(
                Guid.NewGuid(),
                new CreateDiaryEntryRequest { CommandId = "cmd-1", CategoryCode = "RADIO", Text = "Ignored" },
                CancellationToken.None);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        }
        */

        /// <summary>
        /// Verifies that create entry returns stored response when command was already processed.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CreateEntry_ReturnsStoredResponse_WhenCommandWasAlreadyProcessed()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            Guid existingEntryId = Guid.NewGuid();
            ProcessedCommand existingResponse = new ()
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

            Mock<ICommandIdempotencyService> idempotencyMock = new ();
            _ = idempotencyMock
                .Setup(x => x.FindExistingAsync(It.IsAny<string>(), "diary.entry.create", It.IsAny<string?>(), It.IsAny<object>(), "user-1", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((existingResponse, "payload-hash")));

            IncidentDiaryController controller = CreateController(dbContext, idempotencyMock: idempotencyMock);

            IActionResult result = await controller.CreateEntry(
                Guid.NewGuid(),
                new CreateDiaryEntryRequest
                {
                    CommandId = "cmd-1",
                    CategoryCode = "RADIO",
                    Text = "Existing",
                    EventTimestampUtc = DateTime.UtcNow,
                },
                CancellationToken.None);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            JsonElement payload = Assert.IsType<JsonElement>(objectResult.Value);
            Assert.Equal(existingEntryId, payload.GetProperty("entryId").GetGuid());
            Assert.Equal(42L, payload.GetProperty("entryNumber").GetInt64());
        }

        /// <summary>
        /// Verifies that update entry returns conflict when expected revision does not match.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task UpdateEntry_ReturnsConflict_WhenExpectedRevisionDoesNotMatch()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            Guid incidentId = Guid.NewGuid();
            Incident incident = CreateIncident(incidentId, revision: 4);
            DiaryEntry entry = CreateEntry(incidentId, entryNumber: 7, currentRevision: 2);
            _ = dbContext.Incidents.Add(incident);
            _ = dbContext.DiaryEntries.Add(entry);
            _ = await dbContext.SaveChangesAsync();

            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.UpdateEntry(
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

            ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
            ProblemDetails details = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Konflikt: veraltete Tagebuchversion.", details.Title);
        }

        /// <summary>
        /// Verifies that update entry persists new revision when request is valid.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task UpdateEntry_PersistsNewRevision_WhenRequestIsValid()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            Guid incidentId = Guid.NewGuid();
            Incident incident = CreateIncident(incidentId, revision: 7);
            DiaryCategory category = CreateCategory("STATUS", "Statusmeldung");
            DiaryEntry entry = CreateEntry(incidentId, entryNumber: 5, currentRevision: 1);
            DiaryEntryRevision existingRevision = CreateRevision(entry, 1, DiaryRevisionType.Created, categoryCode: category.Code, categoryName: category.Name, text: "Initial note");

            _ = dbContext.Incidents.Add(incident);
            _ = dbContext.DiaryCategories.Add(category);
            _ = dbContext.DiaryEntries.Add(entry);
            _ = dbContext.DiaryEntryRevisions.Add(existingRevision);
            _ = await dbContext.SaveChangesAsync();

            Mock<IOutboxService> outboxMock = new ();
            IncidentDiaryController controller = CreateController(dbContext, outboxMock: outboxMock);

            IActionResult result = await controller.UpdateEntry(
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

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            JsonElement payload = ToJsonElement(okResult.Value!);
            Assert.Equal(entry.Id, payload.GetProperty("entryId").GetGuid());
            Assert.Equal(2, payload.GetProperty("currentRevision").GetInt32());
            Assert.Equal(8, payload.GetProperty("incidentRevision").GetInt64());

            Assert.Equal(2, entry.CurrentRevision);
            Assert.Equal(8, incident.Revision);
            DiaryEntryRevision newRevision = Assert.Single(dbContext.DiaryEntryRevisions.Where(x => x.DiaryEntryId == entry.Id && x.RevisionNumber == 2));
            Assert.Equal(DiaryRevisionType.Edited, newRevision.RevisionType);
            Assert.Equal("Corrected note", newRevision.Text);
            Assert.Equal("Control", newRevision.Sender);
            Assert.Equal("Phone", newRevision.TransmissionType);
            _ = Assert.Single(dbContext.ProcessedCommands);
            outboxMock.Verify(x => x.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), It.IsAny<object>()), Times.Once);
        }

        /// <summary>
        /// Verifies that cancel entry returns bad request when reason is missing.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CancelEntry_ReturnsBadRequest_WhenReasonIsMissing()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.CancelEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new CancelDiaryEntryRequest { CommandId = "cmd-4", ExpectedRevision = 1, Reason = "   " },
                CancellationToken.None);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            ProblemDetails details = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Stornierungsbegründung ist erforderlich.", details.Title);
        }

        /// <summary>
        /// Verifies that cancel entry persists cancellation revision when request is valid.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CancelEntry_PersistsCancellationRevision_WhenRequestIsValid()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            Guid incidentId = Guid.NewGuid();
            Incident incident = CreateIncident(incidentId, revision: 3);
            DiaryEntry entry = CreateEntry(incidentId, entryNumber: 9, currentRevision: 1);
            DiaryEntryRevision previousRevision = CreateRevision(entry, 1, DiaryRevisionType.Created, categoryCode: "RADIO", categoryName: "Radio", text: "Original note");

            _ = dbContext.Incidents.Add(incident);
            _ = dbContext.DiaryEntries.Add(entry);
            _ = dbContext.DiaryEntryRevisions.Add(previousRevision);
            _ = await dbContext.SaveChangesAsync();

            Mock<IOutboxService> outboxMock = new ();
            IncidentDiaryController controller = CreateController(dbContext, outboxMock: outboxMock);

            IActionResult result = await controller.CancelEntry(
                incidentId,
                entry.Id,
                new CancelDiaryEntryRequest
                {
                    CommandId = "cmd-5",
                    ExpectedRevision = 1,
                    Reason = "  Duplicate entry  ",
                },
                CancellationToken.None);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            JsonElement payload = ToJsonElement(okResult.Value!);
            Assert.Equal(entry.Id, payload.GetProperty("entryId").GetGuid());
            Assert.True(payload.GetProperty("canceled").GetBoolean());
            Assert.Equal(2, payload.GetProperty("currentRevision").GetInt32());
            Assert.Equal(4, payload.GetProperty("incidentRevision").GetInt64());

            Assert.True(entry.IsCanceled);
            Assert.Equal("Duplicate entry", entry.CancellationReason);
            Assert.Equal(2, entry.CurrentRevision);
            Assert.Equal(4, incident.Revision);

            DiaryEntryRevision cancellationRevision = Assert.Single(dbContext.DiaryEntryRevisions.Where(x => x.DiaryEntryId == entry.Id && x.RevisionType == DiaryRevisionType.Canceled));
            Assert.Equal(2, cancellationRevision.RevisionNumber);
            Assert.Equal("Original note", cancellationRevision.Text);
            Assert.Equal("Duplicate entry", cancellationRevision.CancellationReason);
            _ = Assert.Single(dbContext.ProcessedCommands);
            outboxMock.Verify(x => x.AddMessage("diary", "diary.entry.changed", incidentId.ToString(), It.IsAny<object>()), Times.Once);
        }

        /// <summary>
        /// Verifies that export diary pdf returns not found when incident does not exist.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ExportDiaryPdf_ReturnsNotFound_WhenIncidentDoesNotExist()
        {
            await using FeuerwehrDbContext dbContext = CreateDbContext();
            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.ExportDiaryPdf(Guid.NewGuid(), CancellationToken.None);

            _ = Assert.IsType<NotFoundResult>(result);
        }

        /*
        [Fact]
        /// <summary>
        /// Executes export Diary Pdf Returns Pdf File With Diary Content.
        /// </summary>
        public async Task ExportDiaryPdf_ReturnsPdfFile_WithDiaryContent()
        {
            await using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            DbContextOptions<FeuerwehrDbContext> options = new DbContextOptionsBuilder<FeuerwehrDbContext>()
                .UseSqlite(connection)
                .Options;

            await using FeuerwehrDbContext dbContext = new FeuerwehrDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            Guid incidentId = Guid.NewGuid();
            Incident incident = CreateIncident(incidentId, name: "Warehouse Fire", revision: 2);
            DiaryEntry firstEntry = CreateEntry(incidentId, entryNumber: 1, currentRevision: 1);
            DiaryEntry secondEntry = CreateEntry(incidentId, entryNumber: 2, currentRevision: 2, isCanceled: true, cancellationReason: "Duplicate");

            dbContext.Incidents.Add(incident);
            dbContext.DiaryEntries.AddRange(firstEntry, secondEntry);
            dbContext.DiaryEntryRevisions.AddRange(
                CreateRevision(firstEntry, 1, DiaryRevisionType.Created, categoryName: "Radio", text: "Initial radio traffic"),
                CreateRevision(secondEntry, 1, DiaryRevisionType.Created, categoryName: "Status", text: "Status before cancel"),
                CreateRevision(secondEntry, 2, DiaryRevisionType.Canceled, categoryName: "Status", text: "Status before cancel", cancellationReason: "Duplicate"));
            await dbContext.SaveChangesAsync();

            IncidentDiaryController controller = CreateController(dbContext);

            IActionResult result = await controller.ExportDiaryPdf(incidentId, CancellationToken.None);

            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal($"einsatztagebuch-{incidentId:N}.pdf", fileResult.FileDownloadName);
            Assert.NotEmpty(fileResult.FileContents);

            string pdfText = Encoding.ASCII.GetString(fileResult.FileContents);
            Assert.Contains("%PDF-1.4", pdfText);
            Assert.Contains("Einsatztagebuch", pdfText);
            Assert.Contains("Einsatz: Warehouse Fire", pdfText);
            Assert.Contains("#1", pdfText);
            Assert.Contains("Initial radio traffic", pdfText);
            Assert.Contains("[STORNIERT]", pdfText);
            Assert.Contains("Grund: Duplicate", pdfText);
        }
        */

        /// <summary>
        /// Serializes the supplied test data as a JSON element.
        /// </summary>
        /// <param name="value">The object value to process.</param>
        /// <returns>The serialized JSON value for the supplied test data.</returns>
        public static JsonElement ToJsonElement(object value)
            => JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
