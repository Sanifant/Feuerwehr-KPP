using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration;

// Actual Program startup, migrations, JWT/Identity and HTTP routing against disposable infrastructure.
[Trait("Category", "Integration")]
/// <summary>
/// Executes inventory Api Tests.
/// </summary>
/// <param name="fixture">The fixture.</param>
public sealed class InventoryApiTests(InventoryApiFixture fixture) : IClassFixture<InventoryApiFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    /// <summary>
    /// Executes startup Migrates And Seeds Categories And Roles.
    /// </summary>
    public async Task Startup_MigratesAndSeedsCategoriesAndRoles()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), x => x.EndsWith("AddEquipmentInventory"));
        foreach (var name in new[] { "Armaturen", "Beleuchtung", "LÃ¶schgerÃ¤te" }) Assert.True(await db.InventoryCategories.AnyAsync(x => x.Name == name));
        Assert.True(await db.Roles.AnyAsync(x => x.Name == Roles.EquipmentOfficer));
        Assert.True(await db.Roles.AnyAsync(x => x.Name == Roles.InventoryViewer));
    }

    [Fact]
    /// <summary>
    /// Executes authorization Separates Viewing Maintenance And Category Administration.
    /// </summary>
    public async Task Authorization_SeparatesViewingMaintenanceAndCategoryAdministration()
    {
        using var anonymous = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/inventory")).StatusCode);
        using var unrelated = await fixture.Client(Roles.Firefighter);
        Assert.Equal(HttpStatusCode.Forbidden, (await unrelated.GetAsync("/api/inventory")).StatusCode);
        using var viewer = await fixture.Client(Roles.InventoryViewer);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/inventory")).StatusCode);
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PutAsJsonAsync($"/api/inventory/{id}/active", new SetEquipmentActiveRequest { ExpectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"/api/inventory/{id}/inspections", new RecordEquipmentInspectionRequest { ExpectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest())).StatusCode);
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/inventory/categories", new { Name = "Test" })).StatusCode);
        using var admin = await fixture.Client(Roles.Admin);
        var name = $"Kategorie {Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/inventory/categories", new { Name = name })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/inventory/categories", new { Name = name.ToUpperInvariant() })).StatusCode);
    }

    [Fact]
    /// <summary>
    /// Executes equipment Uses Type Plan And Calendar Months Records Inspection And Snapshots.
    /// </summary>
    public async Task Equipment_UsesTypePlanAndCalendarMonths_RecordsInspectionAndSnapshots()
    {
        using var client = await fixture.Client(Roles.EquipmentOfficer);
        var type = await fixture.CreateType(client, 12);
        var item = await fixture.CreateEquipment(client, type.Id);
        Assert.Matches("^KPP-[0-9]{3}$", item.Barcode);
        Assert.Equal(fixture.Today.AddMonths(12), item.NextInspectionOn);
        Assert.Null(item.LastInspectedOn);
        var found = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/barcode/{item.Barcode}");
        Assert.Equal(item.Id, found!.Equipment.Id);
        var label = await client.GetAsync($"/api/inventory/{item.Id}/barcode.svg");
        Assert.Equal("image/svg+xml", label.Content.Headers.ContentType!.MediaType);
        Assert.Contains("<svg", await label.Content.ReadAsStringAsync());
        Assert.Contains(item.Barcode, label.Content.Headers.ContentDisposition!.ToString());
        var request = new RecordEquipmentInspectionRequest { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, Status = EquipmentStatus.Defective, Comment = "Dichtung undicht", ExpectedVersion = item.Version };
        var response = await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<EquipmentDetailDto>())!;
        Assert.Equal(EquipmentStatus.Defective, result.Equipment.Status);
        var entry = Assert.Single(result.Inspections);
        Assert.Equal("Dichtung undicht", entry.Comment);
        Assert.NotEmpty(entry.InspectorName);
        Assert.Equal(type.InspectionInstructions, entry.InspectionInstructions);
        Assert.Equal(fixture.Today, result.Equipment.LastInspectedOn);
        // Safe retry does not create another inspection; altered payload is rejected.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request)).StatusCode);
        request.Comment = "Andere Nutzlast";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request)).StatusCode);
        var changed = await client.PutAsJsonAsync($"/api/inventory/types/{type.Id}", new SaveEquipmentTypeRequest
        { CategoryId = type.CategoryId, Name = type.Name, InspectionIntervalMonths = 6, InspectionInstructions = "Neue Anweisung", ExpectedVersion = type.Version });
        changed.EnsureSuccessStatusCode();
        found = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}");
        Assert.Equal(fixture.Today.AddMonths(6), found!.Equipment.NextInspectionOn);
        Assert.Equal(type.InspectionInstructions, Assert.Single(found.Inspections).InspectionInstructions);
        Assert.Equal(12, found.Inspections[0].InspectionIntervalMonths);
    }

    [Fact]
    /// <summary>
    /// Executes inactive Equipment Remains Readable But Cannot Be Inspected And Barcode Is Not Reused.
    /// </summary>
    public async Task InactiveEquipment_RemainsReadableButCannotBeInspected_AndBarcodeIsNotReused()
    {
        using var client = await fixture.Client(Roles.EquipmentOfficer);
        var type = await fixture.CreateType(client);
        var item = await fixture.CreateEquipment(client, type.Id);
        var result = await client.PutAsJsonAsync($"/api/inventory/{item.Id}/active", new SetEquipmentActiveRequest { IsActive = false, ExpectedVersion = item.Version });
        result.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await client.GetFromJsonAsync<EquipmentDto[]>("/api/inventory"))!, x => x.Id == item.Id);
        Assert.Contains((await client.GetFromJsonAsync<EquipmentDto[]>("/api/inventory?includeInactive=true"))!, x => x.Id == item.Id);
        var detail = (await result.Content.ReadFromJsonAsync<EquipmentDetailDto>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", new RecordEquipmentInspectionRequest
        { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, ExpectedVersion = detail.Equipment.Version })).StatusCode);
        Assert.NotEqual(item.Barcode, (await fixture.CreateEquipment(client, type.Id)).Barcode);
    }

    [Fact]
    /// <summary>
    /// Executes validation And Concurrent Edits Do Not Lose Inspection History.
    /// </summary>
    public async Task ValidationAndConcurrentEdits_DoNotLoseInspectionHistory()
    {
        using var client = await fixture.Client(Roles.EquipmentOfficer);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest { InspectionIntervalMonths = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/inventory/barcode/KPP-000")).StatusCode);
        var type = await fixture.CreateType(client);
        var item = await fixture.CreateEquipment(client, type.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", new RecordEquipmentInspectionRequest
        { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today.AddDays(1), ExpectedVersion = item.Version })).StatusCode);
        var requests = Enumerable.Range(0, 2).Select(_ => new RecordEquipmentInspectionRequest
        { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, ExpectedVersion = item.Version, Status = EquipmentStatus.InRepair }).ToArray();
        var results = await Task.WhenAll(requests.Select(x => client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", x)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        var detail = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}");
        Assert.Single(detail!.Inspections);
    }

    [Fact]
    /// <summary>
    /// Executes barcode Limit Stops At999 Without Wrapping.
    /// </summary>
    public async Task BarcodeLimit_StopsAt999WithoutWrapping()
    {
        using var client = await fixture.Client(Roles.EquipmentOfficer);
        var type = await fixture.CreateType(client);
        await using var connection = new NpgsqlConnection(fixture.Postgres.GetConnectionString()); await connection.OpenAsync();
        await using var read = new NpgsqlCommand("SELECT last_value, is_called FROM \"InventoryBarcodeSequence\"", connection);
        await using var reader = await read.ExecuteReaderAsync(); await reader.ReadAsync();
        var value = reader.GetInt64(0); var called = reader.GetBoolean(1); await reader.CloseAsync();
        try
        {
            await using var advance = new NpgsqlCommand("SELECT setval('\"InventoryBarcodeSequence\"', 998, true)", connection); await advance.ExecuteNonQueryAsync();
            Assert.Equal("KPP-999", (await fixture.CreateEquipment(client, type.Id)).Barcode);
            var response = await client.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest { EquipmentTypeId = type.Id, SerialNumber = "Limit", Manufacturer = "Test" });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("ausgeschÃ¶pft", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await using var restore = new NpgsqlCommand("SELECT setval('\"InventoryBarcodeSequence\"', @value, @called)", connection);
            restore.Parameters.AddWithValue("value", value); restore.Parameters.AddWithValue("called", called); await restore.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    /// <summary>
    /// Executes reminders Send One Digest Per Active Officer And Do Not Repeat.
    /// </summary>
    public async Task Reminders_SendOneDigestPerActiveOfficer_AndDoNotRepeat()
    {
        using var client = await fixture.Client(Roles.EquipmentOfficer);
        var type = await fixture.CreateType(client, 1);
        var due1 = await fixture.CreateEquipment(client, type.Id);
        var due2 = await fixture.CreateEquipment(client, type.Id);
        var notDue = await fixture.CreateEquipment(client, type.Id);
        var inactive = await fixture.CreateEquipment(client, type.Id);
        var email = $"reminder-{Guid.NewGuid():N}@integration.test";
        await fixture.CreateUser(Roles.EquipmentOfficer, email);
        var inactiveEmail = $"inactive-{Guid.NewGuid():N}@integration.test";
        await fixture.CreateUser(Roles.EquipmentOfficer, inactiveEmail, false);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        foreach (var id in new[] { due1.Id, due2.Id, inactive.Id })
            (await db.Equipment.FindAsync(id))!.CreatedOn = fixture.Today.AddDays(14).AddMonths(-1);
        (await db.Equipment.FindAsync(inactive.Id))!.IsActive = false;
        await db.SaveChangesAsync();
        var reminders = scope.ServiceProvider.GetRequiredService<IInventoryReminderService>();
        await reminders.SendDueReminders(Ct);
        await reminders.SendDueReminders(Ct);
        var user = await db.Users.SingleAsync(x => x.Email == email);
        Assert.Equal(2, await db.EquipmentReminderDeliveries.CountAsync(x => x.RecipientId == user.Id));
        using var mail = new HttpClient { BaseAddress = fixture.MailpitUrl };
        var messages = await mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        var received = messages.GetProperty("messages").EnumerateArray().Where(x => x.GetProperty("To").EnumerateArray().Any(to => to.GetProperty("Address").GetString() == email)).ToArray();
        var message = Assert.Single(received);
        var content = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{message.GetProperty("ID").GetString()}");
        var body = content.GetProperty("Text").GetString()!;
        Assert.Contains(due1.Barcode, body); Assert.Contains(due2.Barcode, body); Assert.DoesNotContain(notDue.Barcode, body); Assert.DoesNotContain(inactive.Barcode, body);
        Assert.DoesNotContain(messages.GetProperty("messages").EnumerateArray(), x => x.GetProperty("To").EnumerateArray().Any(to => to.GetProperty("Address").GetString() == inactiveEmail));
    }
}

public sealed class InventoryApiFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
    private readonly IContainer mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
        .WithPortBinding(1025, true).WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025))).Build();
    public WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program> Factory { get; private set; } = null!;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public Uri MailpitUrl => new($"http://{mailpit.Hostname}:{mailpit.GetMappedPublicPort(8025)}");
    private const string Password = "InventoryTest123456";

    /// <summary>
    /// Initializes the operation.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await Task.WhenAll(Postgres.StartAsync(), redis.StartAsync(), mailpit.StartAsync());
            Factory = new InventoryFactory(new Dictionary<string, string>
            {
                ["ConnectionStrings:postgresdb"] = Postgres.GetConnectionString(), ["ConnectionStrings:cache"] = redis.GetConnectionString(),
                ["MAIL_HOST"] = mailpit.Hostname, ["MAIL_PORT"] = mailpit.GetMappedPublicPort(1025).ToString(), ["MAIL_SECURITY"] = "None",
                ["MAIL_USERNAME"] = "", ["MAIL_PASSWORD"] = "", ["MAIL_FROM"] = "noreply@integration.test",
                ["Frontend:BaseUrl"] = "https://frontend.integration.test", ["SeedAdmin:Email"] = "admin@integration.test", ["SeedAdmin:Password"] = Password,
                ["JwtSettings:SecretKey"] = "inventory-integration-tests-only-signing-key-1234567890", ["JwtSettings:Issuer"] = "inventory-tests", ["JwtSettings:Audience"] = "inventory-tests",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "",
            });
            using var startup = Factory.CreateClient();
            foreach (var role in new[] { Roles.EquipmentOfficer, Roles.InventoryViewer, Roles.Firefighter }) await CreateUser(role, $"{role}@integration.test");
        }
        catch { await DisposeAsync(); throw; }
    }
    /// <summary>
    /// Creates the user.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="email">The email.</param>
    /// <param name="active">The active.</param>
    public async Task CreateUser(string role, string email, bool active = true)
    {
        using var scope = Factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = role, IsActive = active };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }
    /// <summary>
    /// Executes client.
    /// </summary>
    /// <param name="role">The role.</param>
    public async Task<HttpClient> Client(string role)
    {
        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Email = role == Roles.Admin ? "admin@integration.test" : $"{role}@integration.test", Password = Password });
        login.EnsureSuccessStatusCode(); var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken); return client;
    }
    /// <summary>
    /// Creates the type.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="interval">The interval.</param>
    public async Task<EquipmentTypeDto> CreateType(HttpClient client, int interval = 12)
    {
        var categories = await client.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories");
        var response = await client.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest
        { CategoryId = categories![0].Id, Name = $"Typ {Guid.NewGuid()}", InspectionInstructions = "Sicht- und FunktionsprÃ¼fung nach Herstellerangaben", InspectionIntervalMonths = interval });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<EquipmentTypeDto>())!;
    }
    /// <summary>
    /// Creates the equipment.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="type">The type.</param>
    public async Task<EquipmentDto> CreateEquipment(HttpClient client, Guid type)
    {
        var response = await client.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest { EquipmentTypeId = type, SerialNumber = Guid.NewGuid().ToString(), Manufacturer = "Testhersteller" });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
    }
    /// <summary>
    /// Executes dispose.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        await Task.WhenAll(Postgres.DisposeAsync().AsTask(), redis.DisposeAsync().AsTask(), mailpit.DisposeAsync().AsTask());
    }
    private sealed class InventoryFactory(Dictionary<string, string> settings) : WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        { builder.UseEnvironment("Testing"); foreach (var pair in settings) builder.UseSetting(pair.Key, pair.Value); }
    }
}

