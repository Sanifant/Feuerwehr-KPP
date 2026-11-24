namespace de.openelp.feuerwehr.Tests.Backend.Services.HydrantModules
{
    using de.openelp.feuerwehr.Common.Models;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Hydrant;
    using de.openelp.feuerwehr.Server.Services;
    using de.openelp.feuerwehr.Server.Services.HydrantModules;
    using de.openelp.feuerwehr.Server.Services.Settings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;

    public sealed class HydrantMailServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("hydrant@example.test")]
        public async Task QueuesConfiguredNotificationsInTheCallersUnitOfWorkAsync(string? recipient)
        {
            DbContextOptions<FeuerwehrDbContext> options = new DbContextOptionsBuilder<FeuerwehrDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            await using FeuerwehrDbContext db = new FeuerwehrDbContext(options);
            Mock<ISettingsRuntime> settings = new Mock<ISettingsRuntime>();
            settings.Setup(x => x.GetValueAsync("HYDRANT_MAIL_SERVICE_ENABLED", default)).ReturnsAsync(recipient);
            Mock<TimeProvider> clock = new Mock<TimeProvider>();
            DateTimeOffset now = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
            clock.Setup(x => x.GetUtcNow()).Returns(now);
            HydrantMailService service = new HydrantMailService(new MailQueue(db, clock.Object),
                NullLogger<HydrantMailService>.Instance, settings.Object);
            HydrantInspection inspection = new HydrantInspection
            {
                Id = Guid.NewGuid(), HydrantId = 42, InspectorId = "inspector", NewStatus = HydrantStatus.Defective,
            };
            await service.QueueMailAsync(inspection, default);
            await using FeuerwehrDbContext beforeCommit = new FeuerwehrDbContext(options);
            Assert.Empty(await beforeCommit.MailMessages.ToListAsync());
            await db.SaveChangesAsync();
            if (recipient is null)
            {
                Assert.Empty(await beforeCommit.MailMessages.ToListAsync());
                return;
            }

            MailMessage message = await beforeCommit.MailMessages.SingleAsync();
            Assert.Equal("hydrant", message.Source);
            Assert.Equal(inspection.Id.ToString(), message.DeduplicationKey);
            Assert.Equal(recipient, message.Recipient);
            Assert.Contains("42", message.TextBody);
            Assert.NotNull(message.HtmlBody);
            Assert.Equal("inspector", message.ModifiedBy);
            Assert.Equal(now.UtcDateTime, message.CreatedAtUtc);
            Assert.Equal(now.UtcDateTime, message.ModifiedOn);
            Assert.Equal(now.UtcDateTime, message.NextAttemptAtUtc);
            Assert.Null(message.LeaseUntilUtc);
        }
    }
}
