using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Controller.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
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
            CommandIdempotencyService idempotency = new CommandIdempotencyService(_db);
            OutboxService outbox = new OutboxService(_db);
            _diary = new IncidentDiaryService(_db, _user.Object, idempotency, outbox);
            _incidents = new IncidentService(_db, _user.Object, idempotency, outbox);
            _archive = new IncidentArchiveService(_db, _diary, _user.Object);
        }

        private Incident AddIncident(IncidentStatus status = IncidentStatus.Closed, int closedHoursAgo = 1)
        {
            Incident incident = new Incident
            {
                Id = Guid.NewGuid(), Name = "Brand Gerätehaus", Keyword = "B3", IncidentNumber = "2026-123",
                Description = "Löschzug StraÃŸe", Status = status, CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                AlarmedAtUtc = DateTime.UtcNow.AddHours(-7), ClosedAtUtc = status == IncidentStatus.Closed ? DateTime.UtcNow.AddHours(-closedHoursAgo) : null,
                ClosedByDisplayName = "Ada Muster", Revision = 3,
            };
            _db.Incidents.Add(incident);
            return incident;
        }

        private void AddEntry(Incident incident, long number, bool canceled = false)
        {
            DiaryEntry entry = new DiaryEntry
            {
                Id = Guid.NewGuid(), IncidentId = incident.Id, EntryNumber = number, CurrentRevision = canceled ? 2 : 1,
                IsCanceled = canceled, CancellationReason = canceled ? "Doppelte Meldung" : null,
                CreatedAtUtc = incident.CreatedAtUtc, UpdatedAtUtc = incident.CreatedAtUtc,
            };
            _db.DiaryEntries.Add(entry);
            DiaryEntryRevision revision = new DiaryEntryRevision
            {
                Id = Guid.NewGuid(), DiaryEntryId = entry.Id, RevisionNumber = 1, RevisionType = DiaryRevisionType.Created,
                EventTimestampUtc = incident.CreatedAtUtc, CategoryCode = "meldung", CategoryNameSnapshot = "Meldung",
                Text = number == 70 ? "LetzteMeldung" : "Löschzug StraÃŸe: Wasser am Rohr. " + new string('X', 180) + "\nWeitere Informationen zur Einsatzlage.",
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
        /// <summary>
        /// Executes archive Lists Only Closed Incidents In Newest First Pages.
        /// </summary>
        public async Task ArchiveListsOnlyClosedIncidentsInNewestFirstPages()
        {
            Incident older = AddIncident(closedHoursAgo: 3);
            Incident newer = AddIncident(closedHoursAgo: 1);
            AddIncident(IncidentStatus.Active);
            await _db.SaveChangesAsync();
            IEnumerable<IncidentSummaryDto> first = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await _incidents.GetArchive(1, 1)).Value);
            IEnumerable<IncidentSummaryDto> second = Assert.IsAssignableFrom<IEnumerable<IncidentSummaryDto>>((await _incidents.GetArchive(2, 1)).Value);
            Assert.Equal(newer.Id, Assert.Single(first).Id);
            Assert.Equal(older.Id, Assert.Single(second).Id);
        }

        [Fact]
        /// <summary>
        /// Executes archive Diary Includes Current State And All Revisions Without Changing Data.
        /// </summary>
        public async Task ArchiveDiaryIncludesCurrentStateAndAllRevisionsWithoutChangingData()
        {
            Incident incident = AddIncident();
            AddEntry(incident, 1, canceled: true);
            await _db.SaveChangesAsync();
            ServiceResult result = await _archive.GetDiary(incident.Id, default);
            Assert.Equal(200, result.Code);
            DiaryEntryDto entry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<DiaryEntryDto>>(result.Value));
            Assert.True(entry.IsCanceled);
            Assert.Equal("Doppelte Meldung", entry.CurrentRevisionData.CancellationReason);
            Assert.Equal(2, entry.Revisions.Count);
            Assert.Equal(IncidentStatus.Closed, (await _db.Incidents.SingleAsync()).Status);
            Assert.Empty(await _db.OutboxMessages.ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes archive Rejects Active Or Unknown Incidents.
        /// </summary>
        public async Task ArchiveRejectsActiveOrUnknownIncidents()
        {
            Incident active = AddIncident(IncidentStatus.Active);
            await _db.SaveChangesAsync();
            foreach (Guid id in new[] { active.Id, Guid.NewGuid() })
            {
                Assert.Equal(404, (await _archive.GetDiary(id, default)).Code);
                Assert.Equal(404, (await _archive.ExportPdf(id, default)).Code);
            }
        }

        [Fact]
        /// <summary>
        /// Executes non Admins Cannot Read Archives Or Use Legacy Diary Endpoints For Closed Incidents.
        /// </summary>
        public async Task NonAdminsCannotReadArchivesOrUseLegacyDiaryEndpointsForClosedIncidents()
        {
            Incident closed = AddIncident();
            Incident active = AddIncident(IncidentStatus.Active);
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
        /// <summary>
        /// Executes all Archive Http Endpoints Require Admin Role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="allowed">The allowed.</param>
        public async Task AllArchiveHttpEndpointsRequireAdminRole(string? role, bool allowed)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(options => options.AddFeuerwehrPolicies());
            using ServiceProvider provider = services.BuildServiceProvider();
            IAuthorizationPolicyProvider policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
            IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();
            ClaimsPrincipal principal = new ClaimsPrincipal(role is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));
            MethodInfo[] endpoints = new[]
            {
                typeof(IncidentController).GetMethod(nameof(IncidentController.GetArchive))!,
                typeof(IncidentArchiveController).GetMethod(nameof(IncidentArchiveController.GetDiary))!,
                typeof(IncidentArchiveController).GetMethod(nameof(IncidentArchiveController.ExportPdf))!,
            };
            foreach (MethodInfo? endpoint in endpoints)
            {
                IEnumerable<AuthorizeAttribute> attributes = endpoint.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>().Concat(endpoint.GetCustomAttributes<AuthorizeAttribute>());
                AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(policies, attributes);
                Assert.NotNull(policy);
                Assert.Equal(allowed, (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded);
            }
        }

        /// <summary>
        /// Executes dispose.
        /// </summary>
        public void Dispose() => _db.Dispose();
    }

}
