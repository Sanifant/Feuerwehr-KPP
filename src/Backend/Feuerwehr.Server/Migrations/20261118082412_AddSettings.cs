using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HydrantInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HydrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HydrantId1 = table.Column<int>(type: "integer", nullable: false),
                    InspectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    InspectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectorId = table.Column<string>(type: "text", nullable: false),
                    InspectorName = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydrantInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HydrantInspections_Hydrants_HydrantId1",
                        column: x => x.HydrantId1,
                        principalTable: "Hydrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_AspNetUsers_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HydrantInspections_HydrantId1",
                table: "HydrantInspections",
                column: "HydrantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ModifiedBy",
                table: "Settings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Name",
                table: "Settings",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HydrantInspections");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
