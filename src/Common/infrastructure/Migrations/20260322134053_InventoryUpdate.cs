using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextInspectionDate",
                table: "InventoryItems",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "NextInspectionDate",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InventoryItems");
        }
    }
}
