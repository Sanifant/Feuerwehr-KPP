using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Controller.IncidentModules;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace de.openelp.feuerwehr.Tests.Backend.Services;

public sealed class IncidentArchiveTests : IDisposable
{
    private readonly FeuerwehrDbContext _db = new(new DbContextOptionsBuilder<FeuerwehrDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Mock<IUserContextAccessor> _user = new();
    private readonly IncidentDiaryService _diary;
    private readonly IncidentService _incidents;
    private readonly IncidentArchiveService _archive;

    public IncidentArchiveTests()
    {
        _user.SetupGet(x => x.IsAdmin).Returns(true);
        _user.SetupGet(x => x.UserId).Returns("admin-1");
        _user.SetupGet(x => x.DisplayName).Returns("Ada Muster");
        var idempotency = new CommandIdempotencyService(_db);
        var outbox = new OutboxService(_db);
        _diary = new IncidentDiaryService(_db, _user.Object, idempotency, outbox);
        _incidents = new IncidentService(_db, _user.Object, idempotency, outbox);
        _archive = new IncidentArchiveService(_db, _diary, _user.Object);
    }

    private Incident AddIncident(IncidentStatus status = IncidentStatus.Closed, int closedHoursAgo = 1)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(), Name = "Brand Gerätehaus", Keyword = "B3", IncidentNumber = "2026-123",
            Description = "Löschzug Straße", Status = status, CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
            AlarmedAtUtc = DateTime.UtcNow.AddHours(-7), ClosedAtUtc = status == IncidentStatus.Closed ? DateTime.UtcNow.AddHours(-closedHoursAgo) : null,
            ClosedByDisplayName = "Ada Muster", Revision = 3,
        };
        _db.Incidents.Add(incident);
        return incident;
    }

    private void AddEntry(Incident incident, long number, bool canceled = false)
    {
        var entry = new DiaryEntry
        {
            Id = Guid.NewGuid(), IncidentId = incident.Id, EntryNumber = number, CurrentRevision = canceled ? 2 : 1,
            IsCanceled = canceled, CancellationReason = canceled ? "Doppelte Meldung" : null,
            CreatedAtUtc = incident.CreatedAtUtc, UpdatedAtUtc = incident.CreatedAtUtc,
        };
        _db.DiaryEntries.Add(entry);
        var revision = new DiaryEntryRevision
        {
            Id = Guid.NewGuid(), DiaryEntryId = entry.Id, RevisionNumber = 1, RevisionType = DiaryRevisionType.Created,
            EventTimestampUtc = incident.CreatedAtUtc, CategoryCode = "meldung", CategoryNameSnapshot = "Meldung",
            Text = number == 70 ? "LetzteMeldung" : "Löschzug Straße: Wasser am Rohr. " + new string('X', 180) + "\nWeitere Informationen zur Einsatzlage.",
            Sender = "Florian 1", Recipient = "Einsatzleitung", TransmissionType = "Funk",
            CreatedAtUtc = incident.CreatedAtUtc, CreatedByUserId = "admin-1", CreatedByDisplayName = "Ada Muster",
        };
        _db.DiaryEntryRevisions.Add(revision);
        if (canceled) _db.DiaryEntryRevisions.Add(new DiaryEntryRevision
        {
            Id = Guid.NewGuid(), DiaryEntryId = entry.Id, RevisionNumber = 2, RevisionType = DiaryRevisionType.Canceled,
            EventTimestampUtc = revision.EventTimestampUtc, CategoryCode = revision.CategoryCode, CategoryNameSnapshot = revision.CategoryNameSnapshot,
            Text = "Stornierte Meldung", CancellationReason = "Doppelte Meldung", CreatedAtUtc = revision.CreatedAtUtc,
            CreatedByUserId = "admin-1", CreatedByDisplayName = "Ada Muster",
        });
    }

    [Fact]
    public async Task ArchiveListsOnlyClosedIncidentsInNewestFirstPages()
    {
        var older = AddIncident(closedHoursAgo: 3);
        var newer = AddIncident(closedHoursAgo: 1);
        AddIncident(IncidentStatus.Active);
        await _db.SaveChangesAsync();
        var first = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await _incidents.GetArchive(1, 1)).Value);
        var second = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await _incidents.GetArchive(2, 1)).Value);
        Assert.Equal(newer.Id, Assert.Single(first).Id);
        Assert.Equal(older.Id, Assert.Single(second).Id);
    }

    [Fact]
    public async Task ArchiveDiaryIncludesCurrentStateAndAllRevisionsWithoutChangingData()
    {
        var incident = AddIncident();
        AddEntry(incident, 1, canceled: true);
        await _db.SaveChangesAsync();
        var result = await _archive.GetDiary(incident.Id, default);
        Assert.Equal(200, result.Code);
        var entry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<DiaryEntryDto>>(result.Value));
        Assert.True(entry.IsCanceled);
        Assert.Equal("Doppelte Meldung", entry.CurrentRevisionData.CancellationReason);
        Assert.Equal(2, entry.Revisions.Count);
        Assert.Equal(IncidentStatus.Closed, (await _db.Incidents.SingleAsync()).Status);
        Assert.Empty(await _db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task ArchiveRejectsActiveOrUnknownIncidents()
    {
        var active = AddIncident(IncidentStatus.Active);
        await _db.SaveChangesAsync();
        foreach (var id in new[] { active.Id, Guid.NewGuid() })
        {
            Assert.Equal(404, (await _archive.GetDiary(id, default)).Code);
            Assert.Equal(404, (await _archive.ExportPdf(id, default)).Code);
        }
    }

    [Fact]
    public async Task NonAdminsCannotReadArchivesOrUseLegacyDiaryEndpointsForClosedIncidents()
    {
        var closed = AddIncident();
        var active = AddIncident(IncidentStatus.Active);
        await _db.SaveChangesAsync();
        _user.SetupGet(x => x.IsAdmin).Returns(false);
        Assert.Equal(403, (await _incidents.GetArchive()).Code);
        Assert.Equal(403, (await _archive.GetDiary(closed.Id, default)).Code);
        Assert.Equal(403, (await _archive.ExportPdf(closed.Id, default)).Code);
        Assert.Equal(403, (await _diary.GetEntries(closed.Id, default)).Code);
        Assert.Equal(403, (await _diary.ExportDiaryPdf(closed.Id, default)).Code);
        Assert.Equal(200, (await _diary.GetEntries(active.Id, default)).Code);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Commander", false)]
    [InlineData("IncidentDiaryViewer", false)]
    [InlineData("IncidentDiaryEditor", false)]
    [InlineData("SituationMapEditor", false)]
    [InlineData(null, false)]
    public async Task AllArchiveHttpEndpointsRequireAdminRole(string? role, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddFeuerwehrPolicies());
        using var provider = services.BuildServiceProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(role is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));
        var endpoints = new[]
        {
            typeof(IncidentController).GetMethod(nameof(IncidentController.GetArchive))!,
            typeof(IncidentArchiveController).GetMethod(nameof(IncidentArchiveController.GetDiary))!,
            typeof(IncidentArchiveController).GetMethod(nameof(IncidentArchiveController.ExportPdf))!,
        };
        foreach (var endpoint in endpoints)
        {
            var attributes = endpoint.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>().Concat(endpoint.GetCustomAttributes<AuthorizeAttribute>());
            var policy = await AuthorizationPolicy.CombineAsync(policies, attributes);
            Assert.NotNull(policy);
            Assert.Equal(allowed, (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded);
        }
    }

    [Fact]
    public async Task PdfIncludesAllEntriesWithPageBreaksUmlautsAndRevisionHistory()
    {
        var incident = AddIncident();
        for (var number = 1; number <= 70; number++) AddEntry(incident, number, canceled: number == 1);
        await _db.SaveChangesAsync();
        var result = await _archive.ExportPdf(incident.Id, default);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal($"einsatztagebuch-{incident.Id:N}.pdf", result.DownloadName);
        var bytes = Assert.IsType<byte[]>(result.Content);
        var pdf = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("LetzteMeldung", pdf);
        Assert.Contains("Versionshistorie", pdf);
        Assert.Contains("Doppelte Meldung", pdf);
        Assert.Contains("L\\366schzug Stra\\337e", pdf);
        Assert.True(int.Parse(Regex.Match(pdf, @"/Count (\d+)").Groups[1].Value) > 1);
        var preview = Environment.GetEnvironmentVariable("FEUERWEHR_PDF_PREVIEW");
        if (!string.IsNullOrWhiteSpace(preview)) await File.WriteAllBytesAsync(preview, bytes);
    }

    public void Dispose() => _db.Dispose();
}
