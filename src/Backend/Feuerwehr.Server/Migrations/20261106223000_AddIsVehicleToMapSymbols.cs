using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261106223000_AddIsVehicleToMapSymbols")]
    /// <inheritdoc />
    public partial class AddIsVehicleToMapSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVehicle",
                table: "MapSymbols",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVehicle",
                table: "MapSymbols");
        }
    }
}
