using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace de.openelp.feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "InventoryBarcodeSequence",
                maxValue: 999L);

            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InspectionIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    InspectionInstructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentTypes_InventoryCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "InventoryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarcodeNumber = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"InventoryBarcodeSequence\"')"),
                    EquipmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LastInspectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    InspectorName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EquipmentTypeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InspectionIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    InspectionInstructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentInspections_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentReminderDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentReminderDeliveries_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "InventoryCategories",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("f3050002-0000-0000-0000-000000000001"), "Löschgeräte", "LÖSCHGERÄTE" },
                    { new Guid("f3050002-0000-0000-0000-000000000002"), "Armaturen", "ARMATUREN" },
                    { new Guid("f3050002-0000-0000-0000-000000000003"), "Beleuchtung", "BELEUCHTUNG" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_BarcodeNumber",
                table: "Equipment",
                column: "BarcodeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_EquipmentTypeId",
                table: "Equipment",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentInspections_CommandId",
                table: "EquipmentInspections",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentInspections_EquipmentId_RecordedAtUtc",
                table: "EquipmentInspections",
                columns: new[] { "EquipmentId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentReminderDeliveries_EquipmentId_RecipientId_DueOn",
                table: "EquipmentReminderDeliveries",
                columns: new[] { "EquipmentId", "RecipientId", "DueOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_CategoryId",
                table: "EquipmentTypes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_NormalizedName",
                table: "InventoryCategories",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentInspections");

            migrationBuilder.DropTable(
                name: "EquipmentReminderDeliveries");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "EquipmentTypes");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.DropSequence(
                name: "InventoryBarcodeSequence");
        }
    }
}
