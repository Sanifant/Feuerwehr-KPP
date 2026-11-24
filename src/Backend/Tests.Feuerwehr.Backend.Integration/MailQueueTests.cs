namespace de.openelp.feuerwehr.Tests.Backend.Integration
{
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Services;
    using de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.Extensions.Logging.Abstractions;
    using Npgsql;
    using Testcontainers.PostgreSql;

    public sealed class MailQueueTests : IAsyncLifetime
    {
        private const string PreviousMigration = "20261121102453_HydrantMessages";
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

        public Task InitializeAsync() => this._postgres.StartAsync();

        public async Task DisposeAsync() => await this._postgres.DisposeAsync();

        [Fact]
        public async Task MigrationPreservesMessagesAndRetryStateAndCanRestoreThePreviousSchemaAsync()
        {
            await using FeuerwehrDbContext db = this.CreateDb();
            IMigrator migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            Guid vehicleId = Guid.NewGuid();
            Guid hydrantId = Guid.NewGuid();
            Guid reservationId = Guid.NewGuid();
            Guid oldHydrantId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;
            DateTime next = now.AddHours(1);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "VehicleMailMessages" ("Id", "ReservationId", "Kind", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError")
                VALUES ({vehicleId}, {reservationId}, 'Requested', 'vehicle@example.test', 'Vehicle', 'Text', '<p>HTML</p>',
                    {now}, {next}, {next}, NULL, 3, 'IOException');
                INSERT INTO "HydrantMailMessages" ("Id", "HydrantId", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "SentAtUtc", "Attempts")
                VALUES ({hydrantId}, {oldHydrantId}, 'hydrant@example.test', 'Hydrant', 'Text', '<p>HTML</p>', {now}, {now}, {now}, 1);
                """);

            await migrator.MigrateAsync();
            List<MailMessage> messages = await db.MailMessages.AsNoTracking().ToListAsync();
            Assert.Equal(2, messages.Count);
            MailMessage vehicle = Assert.Single(messages.Where(x => x.Source == "vehicle"));
            Assert.Equal(reservationId + ":Requested", vehicle.DeduplicationKey);
            Assert.Null(vehicle.SentAtUtc);
            Assert.Equal(3, vehicle.Attempts);
            Assert.Equal("IOException", vehicle.LastError);
            Assert.InRange((vehicle.LeaseUntilUtc!.Value - next).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
            Assert.NotNull(Assert.Single(messages.Where(x => x.Source == "hydrant")).SentAtUtc);
            await using NpgsqlConnection connection = new NpgsqlConnection(this._postgres.GetConnectionString());
            await connection.OpenAsync();
            await using NpgsqlCommand tables = new NpgsqlCommand("SELECT to_regclass('\"VehicleMailMessages\"') IS NULL AND to_regclass('\"HydrantMailMessages\"') IS NULL", connection);
            Assert.Equal(true, await tables.ExecuteScalarAsync());

            await migrator.MigrateAsync(PreviousMigration);
            await using NpgsqlCommand original = new NpgsqlCommand("SELECT \"HydrantId\" FROM \"HydrantMailMessages\"", connection);
            Assert.Equal(oldHydrantId, await original.ExecuteScalarAsync());
            await migrator.MigrateAsync();
            Assert.Equal(2, await db.MailMessages.CountAsync());
        }

        [Fact]
        public async Task ConcurrentDispatchersClaimMessagesOnceAcrossModulesAndFormatsAsync()
        {
            await using FeuerwehrDbContext first = this.CreateDb();
            await first.Database.MigrateAsync();
            MailQueue queue = new MailQueue(first, TimeProvider.System);
            queue.Enqueue(new MailMessage { Source = "vehicle", Recipient = "first@example.test", HtmlBody = "<p>Vehicle</p>" }, null);
            queue.Enqueue(new MailMessage { Source = "hydrant", Recipient = "second@example.test", TextBody = "Hydrant" }, null);
            await first.SaveChangesAsync();
            TaskCompletionSource started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TestMailTransport transport = new TestMailTransport
            {
                Send = recipient =>
                {
                    if (recipient == "first@example.test")
                    {
                        started.TrySetResult();
                        return release.Task;
                    }

                    return Task.CompletedTask;
                },
            };
            Task firstRun = Dispatcher(first, transport).DispatchAsync(default);
            try
            {
                await started.Task.WaitAsync(TimeSpan.FromSeconds(20));
                await using FeuerwehrDbContext second = this.CreateDb();
                await Dispatcher(second, transport).DispatchAsync(default);
                Assert.Single(transport.HtmlRecipients);
                Assert.Single(transport.TextRecipients);
            }
            finally
            {
                release.TrySetResult();
                await firstRun;
            }

            first.ChangeTracker.Clear();
            Assert.All(await first.MailMessages.ToListAsync(), message =>
            {
                Assert.NotNull(message.SentAtUtc);
                Assert.Null(message.LeaseId);
                Assert.Null(message.LeaseUntilUtc);
                Assert.Equal(1, message.Attempts);
            });
            await Dispatcher(first, transport).DispatchAsync(default);
            Assert.Single(transport.HtmlRecipients);
            Assert.Single(transport.TextRecipients);
        }

        [Fact]
        public async Task FailureIsRetriedAfterBackoffAndExpiredLeasesRecoverAsync()
        {
            await using FeuerwehrDbContext db = this.CreateDb();
            await db.Database.MigrateAsync();
            MailQueue queue = new MailQueue(db, TimeProvider.System);
            MailMessage message = new MailMessage { Source = "hydrant", Recipient = "retry@example.test" };
            queue.Enqueue(message, null);
            message.LeaseId = Guid.NewGuid();
            message.LeaseUntilUtc = DateTime.UtcNow.AddMinutes(-1);
            message.Attempts = 1;
            await db.SaveChangesAsync();
            TestMailTransport transport = new TestMailTransport { Send = _ => throw new IOException("private SMTP details") };
            await Dispatcher(db, transport).DispatchAsync(default);
            db.ChangeTracker.Clear();
            message = await db.MailMessages.SingleAsync();
            Assert.Equal(2, message.Attempts);
            Assert.Null(message.SentAtUtc);
            Assert.Null(message.LeaseId);
            Assert.Equal("IOException", message.LastError);
            Assert.InRange(message.NextAttemptAtUtc - DateTime.UtcNow, TimeSpan.FromMinutes(3.5), TimeSpan.FromMinutes(4.5));
            await Dispatcher(db, transport).DispatchAsync(default);
            Assert.Single(transport.TextRecipients);
            message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
            transport.Send = _ => Task.CompletedTask;
            await Dispatcher(db, transport).DispatchAsync(default);
            db.ChangeTracker.Clear();
            message = await db.MailMessages.SingleAsync();
            Assert.NotNull(message.SentAtUtc);
            Assert.Null(message.LastError);
            Assert.Equal(3, message.Attempts);
            Assert.Equal(2, transport.TextRecipients.Count);
        }

        [Fact]
        public async Task MessageKeysAreScopedToTheirModuleAndDowngradesRefuseUnsupportedMessagesAsync()
        {
            await using FeuerwehrDbContext db = this.CreateDb();
            await db.Database.MigrateAsync();
            MailQueue queue = new MailQueue(db, TimeProvider.System);
            queue.Enqueue(new MailMessage { Source = "vehicle", DeduplicationKey = "shared" }, null);
            queue.Enqueue(new MailMessage { Source = "hydrant", DeduplicationKey = "shared" }, null);
            queue.Enqueue(new MailMessage { Source = "inventory" }, null);
            await db.SaveChangesAsync();
            queue.Enqueue(new MailMessage { Source = "vehicle", DeduplicationKey = "shared" }, null);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            Assert.Equal(3, await db.MailMessages.CountAsync());

            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(PreviousMigration));
            Assert.Equal(3, await db.MailMessages.CountAsync());
        }

        private static MailDispatcher Dispatcher(FeuerwehrDbContext db, TestMailTransport transport)
        {
            return new MailDispatcher(db, transport, transport, TimeProvider.System, NullLogger<MailDispatcher>.Instance);
        }

        private FeuerwehrDbContext CreateDb()
        {
            return new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
                .UseNpgsql(this._postgres.GetConnectionString()).Options);
        }
    }
}
