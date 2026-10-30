using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentAlarmTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AlarmedAtUtc",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Incidents\" SET \"AlarmedAtUtc\" = \"CreatedAtUtc\";");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AlarmedAtUtc",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlarmedAtUtc",
                table: "Incidents");
        }
    }
}

