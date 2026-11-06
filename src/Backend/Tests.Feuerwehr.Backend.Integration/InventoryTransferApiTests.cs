using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    /// <summary>
    /// Tests inventory transfer API behavior and its expected outcomes.
    /// </summary>
    /// <param name="fixture">The shared test fixture providing application and database infrastructure.</param>
    [Trait("Category", "Integration")]
    public sealed class InventoryTransferApiTests(InventoryApiFixture fixture) : IClassFixture<InventoryApiFixture>
    {
        /// <summary>
        /// Verifies that CSV upsert uses barcode preserves identity state and history and advances automatic numbers.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CsvUpsertUsesBarcode_PreservesIdentityStateAndHistory_AndAdvancesAutomaticNumbers()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(officer, 12);
            EquipmentTypeDto replacementType = await fixture.CreateType(officer, 6);
            EquipmentDto item = await fixture.CreateEquipment(officer, type.Id);
            using HttpResponseMessage inspection = await officer.PostAsJsonAsync($"/api/inventory/{item.Id}/inspections", new RecordEquipmentInspectionRequest
            { CommandId = Guid.NewGuid(), InspectedOn = fixture.Today, Status = EquipmentStatus.Defective, Comment = "Historie bleibt erhalten", ExpectedVersion = item.Version });
            _ = inspection.EnsureSuccessStatusCode();
            EquipmentDetailDto inspected = (await inspection.Content.ReadFromJsonAsync<EquipmentDetailDto>())!;
            using HttpResponseMessage deactivate = await officer.PutAsJsonAsync($"/api/inventory/{item.Id}/active", new SetEquipmentActiveRequest { IsActive = false, ExpectedVersion = inspected.Equipment.Version });
            _ = deactivate.EnsureSuccessStatusCode();
            string csv = $"barcode;equipmentTypeId;serialNumber;manufacturer;status;isActive;createdOn;lastInspectedOn\r\n{item.Barcode};{replacementType.Id};NEU-123;Neuer Hersteller;Einsatzklar;true;2000-01-01;2000-01-02\r\nKPP-700;{type.Id};IMPORT-700;Importhersteller;In Reparatur;false;{fixture.Today:yyyy-MM-dd};\r\n";
            using HttpResponseMessage response = await Upload(officer, "/api/inventory/import", csv, "inventar.csv");
            _ = response.EnsureSuccessStatusCode();
            InventoryImportResponse result = (await response.Content.ReadFromJsonAsync<InventoryImportResponse>())!;
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(1, result.UpdatedCount);
            EquipmentDetailDto updated = (await officer.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}"))!;
            Assert.Equal(item.Id, updated.Equipment.Id);
            Assert.Equal("NEU-123", updated.Equipment.SerialNumber);
            Assert.Equal("Neuer Hersteller", updated.Equipment.Manufacturer);
            Assert.Equal(replacementType.Id, updated.Equipment.EquipmentTypeId);
            Assert.Equal(EquipmentStatus.Defective, updated.Equipment.Status);
            Assert.False(updated.Equipment.IsActive);
            Assert.Equal(item.CreatedOn, updated.Equipment.CreatedOn);
            Assert.Equal(fixture.Today, updated.Equipment.LastInspectedOn);
            Assert.Equal(fixture.Today.AddMonths(6), updated.Equipment.NextInspectionOn);
            Assert.Equal("Historie bleibt erhalten", Assert.Single(updated.Inspections).Comment);
            EquipmentDetailDto added = (await officer.GetFromJsonAsync<EquipmentDetailDto>("/api/inventory/barcode/KPP-700"))!;
            Assert.Equal(EquipmentStatus.InRepair, added.Equipment.Status);
            Assert.False(added.Equipment.IsActive);
            EquipmentDto automatic = await fixture.CreateEquipment(officer, type.Id);
            Assert.True(int.Parse(automatic.Barcode[4..]) > 700);
        }

        /// <summary>
        /// Verifies that CSV export is excel compatible and round trips quotes unicode newlines and formula prefixes.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CsvExportIsExcelCompatibleAndRoundTripsQuotesUnicodeNewlinesAndFormulaPrefixes()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(officer);
            SaveEquipmentRequest request = new ()
            { EquipmentTypeId = type.Id, Manufacturer = "=SUM(1;2)", SerialNumber = "GröÃŸe; \"Zitat\"\nzweite Zeile" };
            using HttpResponseMessage create = await officer.PostAsJsonAsync("/api/inventory", request);
            _ = create.EnsureSuccessStatusCode();
            EquipmentDto item = (await create.Content.ReadFromJsonAsync<EquipmentDto>())!;
            using HttpResponseMessage export = await officer.GetAsync("/api/inventory/export");
            _ = export.EnsureSuccessStatusCode();
            Assert.Equal("text/csv", export.Content.Headers.ContentType!.MediaType);
            byte[] bytes = await export.Content.ReadAsByteArrayAsync();
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
            string csv = Encoding.UTF8.GetString(bytes);
            Assert.Contains("'=SUM", csv);
            Assert.Contains("GröÃŸe", csv);
            using HttpResponseMessage import = await Upload(officer, "/api/inventory/import", csv, "inventar.csv");
            _ = import.EnsureSuccessStatusCode();
            Assert.Equal(0, (await import.Content.ReadFromJsonAsync<InventoryImportResponse>())!.CreatedCount);
            EquipmentDetailDto detail = (await officer.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}"))!;
            Assert.Equal(request.SerialNumber, detail.Equipment.SerialNumber);
            Assert.Equal(request.Manufacturer, detail.Equipment.Manufacturer);
        }

        /// <summary>
        /// Verifies that invalid inventory rows and duplicate barcodes reject the whole file.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task InvalidInventoryRowsAndDuplicateBarcodesRejectTheWholeFile()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(officer);
            EquipmentDto item = await fixture.CreateEquipment(officer, type.Id);
            string csv = $"barcode;equipmentTypeId;serialNumber;manufacturer\n{item.Barcode};{type.Id};Changed;Changed\nKPP-900;{Guid.NewGuid()};Missing;Type\n";
            using HttpResponseMessage response = await Upload(officer, "/api/inventory/import", csv, "bad.csv");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            InventoryImportError error = Assert.Single((await response.Content.ReadFromJsonAsync<InventoryImportResponse>())!.Errors);
            Assert.Equal(3, error.LineNumber);
            EquipmentDetailDto detail = (await officer.GetFromJsonAsync<EquipmentDetailDto>($"/api/inventory/{item.Id}"))!;
            Assert.Equal(item.SerialNumber, detail.Equipment.SerialNumber);
            Assert.Equal(HttpStatusCode.NotFound, (await officer.GetAsync("/api/inventory/barcode/KPP-900")).StatusCode);
            using HttpResponseMessage duplicate = await Upload(officer, "/api/inventory/import", $"barcode;equipmentTypeId;serialNumber;manufacturer\nKPP-899;{type.Id};A;B\nkpp-899;{type.Id};C;D\n", "duplicate.csv");
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await officer.GetAsync("/api/inventory/barcode/KPP-899")).StatusCode);
        }

        /// <summary>
        /// Verifies that import and ordinary create do not collide and sequence never rewinds.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ImportAndOrdinaryCreateDoNotCollide_AndSequenceNeverRewinds()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            EquipmentTypeDto type = await fixture.CreateType(officer);
            Task<HttpResponseMessage> upload = Upload(officer, "/api/inventory/import", $"barcode;equipmentTypeId;serialNumber;manufacturer\nKPP-800;{type.Id};Concurrent;Test\n", "parallel.csv");
            Task<EquipmentDto> ordinary = fixture.CreateEquipment(officer, type.Id);
            using HttpResponseMessage response = await upload;
            _ = response.EnsureSuccessStatusCode();
            EquipmentDto item = await ordinary;
            Assert.NotEqual("KPP-800", item.Barcode);
            EquipmentDto next = await fixture.CreateEquipment(officer, type.Id);
            Assert.True(int.Parse(next.Barcode[4..]) > 800);
        }

        /// <summary>
        /// Verifies that categories and types use JSON and update existing records.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CategoriesAndTypesUseJsonAndUpdateExistingRecords()
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            string name = $"Kategorie {Guid.NewGuid()}";
            using HttpResponseMessage first = await Upload(admin, "/api/inventory/categories/import", JsonSerializer.Serialize(new[] { new { name } }), "categories.json");
            _ = first.EnsureSuccessStatusCode();
            Assert.Equal(1, (await first.Content.ReadFromJsonAsync<InventoryImportResponse>())!.CreatedCount);
            using HttpResponseMessage second = await Upload(admin, "/api/inventory/categories/import", JsonSerializer.Serialize(new[] { new { name } }), "categories.json");
            _ = second.EnsureSuccessStatusCode();
            Assert.Equal(1, (await second.Content.ReadFromJsonAsync<InventoryImportResponse>())!.UpdatedCount);
            InventoryCategoryDto category = Assert.Single((await officer.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories"))!, x => x.Name == name);
            EquipmentTypeDto type = await fixture.CreateType(officer, 12);
            string json = JsonSerializer.Serialize(new[] {
                new { id = type.Id, name = "Aktualisierter Typ", categoryId = category.Id, inspectionIntervalMonths = 6, inspectionInstructions = "Neue Anweisung" },
                new { id = Guid.NewGuid(), name = "Importierter Typ", categoryId = category.Id, inspectionIntervalMonths = 24, inspectionInstructions = "Weitere Anweisung" },
            });
            using HttpResponseMessage imported = await Upload(officer, "/api/inventory/types/import", json, "types.json");
            _ = imported.EnsureSuccessStatusCode();
            InventoryImportResponse result = (await imported.Content.ReadFromJsonAsync<InventoryImportResponse>())!;
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(1, result.UpdatedCount);
            EquipmentTypeDto updated = Assert.Single((await officer.GetFromJsonAsync<EquipmentTypeDto[]>("/api/inventory/types"))!, x => x.Id == type.Id);
            Assert.Equal(6, updated.InspectionIntervalMonths);
            Assert.Equal(category.Id, updated.CategoryId);
            foreach (string? path in new[] { "categories", "types" })
            {
                using HttpResponseMessage export = await officer.GetAsync($"/api/inventory/{path}/export");
                _ = export.EnsureSuccessStatusCode();
                Assert.Equal("application/json", export.Content.Headers.ContentType!.MediaType);
                string text = await export.Content.ReadAsStringAsync();
                Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(text).RootElement.ValueKind);
                using HttpResponseMessage roundTrip = await Upload(admin, $"/api/inventory/{path}/import", text, "roundtrip.json");
                _ = roundTrip.EnsureSuccessStatusCode();
                Assert.Equal(0, (await roundTrip.Content.ReadFromJsonAsync<InventoryImportResponse>())!.CreatedCount);
            }
        }

        /// <summary>
        /// Verifies that invalid JSON reports errors without partial writes.
        /// </summary>
        /// <param name="kind">The category of records to transfer and its corresponding file format.</param>
        /// <param name="json">The JSON import content submitted by the scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("categories", "[null]")]
        [InlineData("categories", "[{\"name\":\"Doppelt\"},{\"name\":\"doppelt\"}]")]
        [InlineData("types", "[{\"name\":\"Unvollständig\",\"inspectionIntervalMonths\":0}]")]
        [InlineData("types", "not json")]
        public async Task InvalidJsonReportsErrorsWithoutPartialWrites(string kind, string json)
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpResponseMessage response = await Upload(admin, $"/api/inventory/{kind}/import", json, "bad.json");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotEmpty((await response.Content.ReadFromJsonAsync<InventoryImportResponse>())!.Errors);
            InventoryCategoryDto[]? categories = await admin.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories");
            Assert.DoesNotContain(categories!, x => x.Name == "Doppelt");
        }

        /// <summary>
        /// Verifies that JSON export can reimport distinct existing types with the same display name.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task JsonExportCanReimportDistinctExistingTypesWithTheSameDisplayName()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            InventoryCategoryDto[] categories = (await officer.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories"))!;
            SaveEquipmentTypeRequest request = new ()
            { CategoryId = categories[0].Id, Name = $"Doppelname {Guid.NewGuid()}", InspectionIntervalMonths = 12, InspectionInstructions = "Prüfen" };
            for (int i = 0; i < 2; i++)
            {
                using HttpResponseMessage create = await officer.PostAsJsonAsync("/api/inventory/types", request);
                _ = create.EnsureSuccessStatusCode();
            }

            using HttpResponseMessage export = await officer.GetAsync("/api/inventory/types/export");
            _ = export.EnsureSuccessStatusCode();
            using HttpResponseMessage import = await Upload(officer, "/api/inventory/types/import", await export.Content.ReadAsStringAsync(), "types.json");
            _ = import.EnsureSuccessStatusCode();
            Assert.Equal(0, (await import.Content.ReadFromJsonAsync<InventoryImportResponse>())!.CreatedCount);
        }

        /// <summary>
        /// Verifies that readers can export but only officers and admins can import and categories require admin.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ReadersCanExportButOnlyOfficersAndAdminsCanImportAndCategoriesRequireAdmin()
        {
            using HttpClient viewer = await fixture.Client(Roles.InventoryViewer);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            foreach (string? suffix in new[] { string.Empty, "/types", "/categories" })
            {
                using HttpResponseMessage export = await viewer.GetAsync($"/api/inventory{suffix}/export");
                Assert.Equal(HttpStatusCode.OK, export.StatusCode);
                using HttpResponseMessage import = await Upload(viewer, $"/api/inventory{suffix}/import", "[]", "file.json");
                Assert.Equal(HttpStatusCode.Forbidden, import.StatusCode);
            }

            using HttpResponseMessage forbidden = await Upload(officer, "/api/inventory/categories/import", "[]", "file.json");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using HttpResponseMessage missing = await officer.PostAsync("/api/inventory/import", new MultipartFormDataContent());
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        }

        private static async Task<HttpResponseMessage> Upload(HttpClient client, string url, string content, string filename)
        {
            using MultipartFormDataContent form = new ();
            using ByteArrayContent file = new (Encoding.UTF8.GetBytes(content));
            file.Headers.ContentType = new MediaTypeHeaderValue(filename.EndsWith(".csv") ? "text/csv" : "application/json");
            form.Add(file, "file", filename);
            return await client.PostAsync(url, form);
        }
    }
}
