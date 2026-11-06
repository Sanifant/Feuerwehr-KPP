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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    // Actual Program startup, migrations, JWT/Identity and HTTP routing against disposable infrastructure.
    /// <summary>
    /// Tests inventory API behavior and its expected outcomes.
    /// </summary>
    /// <param name="fixture">The shared test fixture providing application and database infrastructure.</param>
    [Trait("Category", "Integration")]
    public sealed class InventoryApiTests(InventoryApiFixture fixture) : IClassFixture<InventoryApiFixture>
    {
        private static readonly CancellationToken Ct = CancellationToken.None;

        /// <summary>
        /// Verifies that startup migrates and seeds categories and roles.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Startup_MigratesAndSeedsCategoriesAndRoles()
        {
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            foreach (string? name in new[] { "Armaturen", "Beleuchtung", "Löschgeräte" })
            {
                Assert.True(await db.InventoryCategories.AnyAsync(x => x.Name == name));
            }

            Assert.True(await db.Roles.AnyAsync(x => x.Name == Roles.EquipmentOfficer));
            Assert.True(await db.Roles.AnyAsync(x => x.Name == Roles.InventoryViewer));
        }

        /// <summary>
        /// Verifies that authorization separates viewing maintenance and category administration.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Authorization_SeparatesViewingMaintenanceAndCategoryAdministration()
        {
            using HttpClient anonymous = fixture.Factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/inventory")).StatusCode);
            using HttpClient unrelated = await fixture.Client(Roles.Firefighter);
            Assert.Equal(HttpStatusCode.Forbidden, (await unrelated.GetAsync("/api/inventory")).StatusCode);
            using HttpClient viewer = await fixture.Client(Roles.InventoryViewer);
            Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/inventory")).StatusCode);
            Guid id = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PutAsJsonAsync($"/api/inventory/{id}/active", new SetEquipmentActiveRequest { ExpectedVersion = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"/api/inventory/{id}/inspections", new RecordEquipmentInspectionRequest { ExpectedVersion = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest())).StatusCode);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/inventory/categories", new { Name = "Test" })).StatusCode);
            using HttpClient admin = await fixture.Client(Roles.Admin);
            string name = $"Kategorie {Guid.NewGuid()}";
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/inventory/categories", new { Name = name })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/inventory/categories", new { Name = name.ToUpperInvariant() })).StatusCode);
        }

        /// <summary>
        /// Verifies that equipment uses type plan and calendar months records inspection and snapshots.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Equipment_UsesTypePlanAndCalendarMonths_RecordsInspectionAndSnapshots()
        {
            using HttpClient client = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(client, 12);
            EquipmentDto item = await fixture.CreateEquipment(client, type.Id);
            Assert.Matches("^KPP-[0-9]{3}$", item.Barcode);
            Assert.Equal(fixture.Today.AddMonths(12), item.NextInspectionOn);
            Assert.Null(item.LastInspectedOn);
            EquipmentDetailDto? found = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/barcode/{item.Barcode}");
            Assert.Equal(item.Id, found!.Equipment.Id);
            HttpResponseMessage label = await client.GetAsync($"/api/inventory/{item.Id}/barcode.svg");
            Assert.Equal("image/svg+xml", label.Content.Headers.ContentType!.MediaType);
            Assert.Contains("<svg", await label.Content.ReadAsStringAsync());
            Assert.Contains(item.Barcode, label.Content.Headers.ContentDisposition!.ToString());
            RecordEquipmentInspectionRequest request = new ()
            { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, Status = EquipmentStatus.Defective, Comment = "Dichtung undicht", ExpectedVersion = item.Version };
            HttpResponseMessage response = await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request);
            _ = response.EnsureSuccessStatusCode();
            EquipmentDetailDto result = (await response.Content.ReadFromJsonAsync<EquipmentDetailDto>())!;
            Assert.Equal(EquipmentStatus.Defective, result.Equipment.Status);
            EquipmentInspectionDto entry = Assert.Single(result.Inspections);
            Assert.Equal("Dichtung undicht", entry.Comment);
            Assert.NotEmpty(entry.InspectorName);
            Assert.Equal(type.InspectionInstructions, entry.InspectionInstructions);
            Assert.Equal(fixture.Today, result.Equipment.LastInspectedOn);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request)).StatusCode);
            request.Comment = "Andere Nutzlast";
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", request)).StatusCode);
            HttpResponseMessage changed = await client.PutAsJsonAsync($"/api/inventory/types/{type.Id}", new SaveEquipmentTypeRequest
            { CategoryId = type.CategoryId, Name = type.Name, InspectionIntervalMonths = 6, InspectionInstructions = "Neue Anweisung", ExpectedVersion = type.Version });
            _ = changed.EnsureSuccessStatusCode();
            found = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}");
            Assert.Equal(fixture.Today.AddMonths(6), found!.Equipment.NextInspectionOn);
            Assert.Equal(type.InspectionInstructions, Assert.Single(found.Inspections).InspectionInstructions);
            Assert.Equal(12, found.Inspections[0].InspectionIntervalMonths);
        }

        /// <summary>
        /// Verifies that inactive equipment remains readable but cannot be inspected and barcode is not reused.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task InactiveEquipment_RemainsReadableButCannotBeInspected_AndBarcodeIsNotReused()
        {
            using HttpClient client = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(client);
            EquipmentDto item = await fixture.CreateEquipment(client, type.Id);
            HttpResponseMessage result = await client.PutAsJsonAsync($"/api/inventory/{item.Id}/active", new SetEquipmentActiveRequest { IsActive = false, ExpectedVersion = item.Version });
            _ = result.EnsureSuccessStatusCode();
            Assert.DoesNotContain((await client.GetFromJsonAsync<EquipmentDto[]>("/api/inventory"))!, x => x.Id == item.Id);
            Assert.Contains((await client.GetFromJsonAsync<EquipmentDto[]>("/api/inventory?includeInactive=true"))!, x => x.Id == item.Id);
            EquipmentDetailDto detail = (await result.Content.ReadFromJsonAsync<EquipmentDetailDto>())!;
            HttpResponseMessage inactiveInspection = await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", new RecordEquipmentInspectionRequest
            { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, ExpectedVersion = detail.Equipment.Version });
            Assert.Equal(HttpStatusCode.Conflict, inactiveInspection.StatusCode);
            Assert.NotEqual(item.Barcode, (await fixture.CreateEquipment(client, type.Id)).Barcode);
        }

        /// <summary>
        /// Verifies that validation and concurrent edits do not lose inspection history.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ValidationAndConcurrentEdits_DoNotLoseInspectionHistory()
        {
            using HttpClient client = await fixture.Client(Roles.EquipmentOfficer);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest { InspectionIntervalMonths = 0 })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/inventory/barcode/KPP-000")).StatusCode);
            EquipmentTypeDto type = await fixture.CreateType(client);
            EquipmentDto item = await fixture.CreateEquipment(client, type.Id);
            HttpResponseMessage futureInspection = await client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", new RecordEquipmentInspectionRequest
            { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today.AddDays(1), ExpectedVersion = item.Version });
            Assert.Equal(HttpStatusCode.BadRequest, futureInspection.StatusCode);
            RecordEquipmentInspectionRequest[] requests = Enumerable.Range(0, 2).Select(_ => new RecordEquipmentInspectionRequest
            { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, ExpectedVersion = item.Version, Status = EquipmentStatus.InRepair }).ToArray();
            HttpResponseMessage[] results = await Task.WhenAll(requests.Select(x => client.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", x)));
            _ = Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            _ = Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            EquipmentDetailDto? detail = await client.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}");
            _ = Assert.Single(detail!.Inspections);
        }

        /// <summary>
        /// Verifies that barcode limit stops at999 without wrapping.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task BarcodeLimit_StopsAt999WithoutWrapping()
        {
            using HttpClient client = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(client);
            await using NpgsqlConnection connection = new (fixture.Postgres.GetConnectionString());
            await connection.OpenAsync();
            await using NpgsqlCommand read = new ("SELECT last_value, is_called FROM \"InventoryBarcodeSequence\"", connection);
            await using NpgsqlDataReader reader = await read.ExecuteReaderAsync();
            _ = await reader.ReadAsync();
            long value = reader.GetInt64(0);
            bool called = reader.GetBoolean(1);
            await reader.CloseAsync();
            try
            {
                await using NpgsqlCommand advance = new ("SELECT setval('\"InventoryBarcodeSequence\"', 998, true)", connection);
                _ = await advance.ExecuteNonQueryAsync();
                Assert.Equal("KPP-999", (await fixture.CreateEquipment(client, type.Id)).Barcode);
                HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest { EquipmentTypeId = type.Id, SerialNumber = "Limit", Manufacturer = "Test" });
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                Assert.Contains("ausgeschöpft", await response.Content.ReadAsStringAsync());
            }
            finally
            {
                await using NpgsqlCommand restore = new ("SELECT setval('\"InventoryBarcodeSequence\"', @value, @called)", connection);
                _ = restore.Parameters.AddWithValue("value", value);
                _ = restore.Parameters.AddWithValue("called", called);
                _ = await restore.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Verifies that reminders send one digest per active officer and do not repeat.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task Reminders_SendOneDigestPerActiveOfficer_AndDoNotRepeat()
        {
            using HttpClient client = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(client, 1);
            EquipmentDto due1 = await fixture.CreateEquipment(client, type.Id);
            EquipmentDto due2 = await fixture.CreateEquipment(client, type.Id);
            EquipmentDto notDue = await fixture.CreateEquipment(client, type.Id);
            EquipmentDto inactive = await fixture.CreateEquipment(client, type.Id);
            string email = $"reminder-{Guid.NewGuid():N}@integration.test";
            await fixture.CreateUser(Roles.EquipmentOfficer, email);
            string inactiveEmail = $"inactive-{Guid.NewGuid():N}@integration.test";
            await fixture.CreateUser(Roles.EquipmentOfficer, inactiveEmail, false);
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            foreach (Guid id in new[] { due1.Id, due2.Id, inactive.Id })
            {
                (await db.Equipment.FindAsync(id))!.CreatedOn = fixture.Today.AddDays(14).AddMonths(-1);
            }

            (await db.Equipment.FindAsync(inactive.Id))!.IsActive = false;
            _ = await db.SaveChangesAsync();
            IInventoryReminderService reminders = scope.ServiceProvider.GetRequiredService<IInventoryReminderService>();
            await reminders.SendDueReminders(Ct);
            await reminders.SendDueReminders(Ct);
            ApplicationUser user = await db.Users.SingleAsync(x => x.Email == email);
            Assert.Equal(2, await db.EquipmentReminderDeliveries.CountAsync(x => x.RecipientId == user.Id));
            using HttpClient mail = new ()
            { BaseAddress = fixture.MailpitUrl };
            JsonElement messages = await mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");
            JsonElement[] received = messages.GetProperty("messages").EnumerateArray().Where(x => x.GetProperty("To").EnumerateArray().Any(to => to.GetProperty("Address").GetString() == email)).ToArray();
            JsonElement message = Assert.Single(received);
            JsonElement content = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{message.GetProperty("ID").GetString()}");
            string body = content.GetProperty("Text").GetString()!;
            Assert.Contains(due1.Barcode, body);
            Assert.Contains(due2.Barcode, body);
            Assert.DoesNotContain(notDue.Barcode, body);
            Assert.DoesNotContain(inactive.Barcode, body);
            Assert.DoesNotContain(messages.GetProperty("messages").EnumerateArray(), x => x.GetProperty("To").EnumerateArray().Any(to => to.GetProperty("Address").GetString() == inactiveEmail));
        }
    }

    }
