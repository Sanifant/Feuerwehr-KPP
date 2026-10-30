using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VehicleStatusId",
                table: "Vehicles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("5032f99d-4352-4000-8000-000000000001"));

            migrationBuilder.CreateTable(
                name: "VehicleStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllowsBooking = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleStatuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "VehicleStatuses",
                columns: new[] { "Id", "AllowsBooking", "IsActive", "Name", "NormalizedName", "Version" },
                values: new object[,]
                {
                    { new Guid("5032f99d-4352-4000-8000-000000000001"), true, true, "Betriebsbereit", "BETRIEBSBEREIT", 1 },
                    { new Guid("5032f99d-4352-4000-8000-000000000002"), false, true, "Wartung", "WARTUNG", 1 },
                    { new Guid("5032f99d-4352-4000-8000-000000000003"), false, true, "Abgemeldet", "ABGEMELDET", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleStatusId",
                table: "Vehicles",
                column: "VehicleStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleStatuses_NormalizedName",
                table: "VehicleStatuses",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleStatuses_VehicleStatusId",
                table: "Vehicles",
                column: "VehicleStatusId",
                principalTable: "VehicleStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleStatuses_VehicleStatusId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VehicleStatusId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleStatusId",
                table: "Vehicles");
        }
    }
}

