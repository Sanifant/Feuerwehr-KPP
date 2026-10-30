using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMapSymbolVehicleFlagAndCategories : Migration
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

            migrationBuilder.AddColumn<bool>(
                name: "IsVehicle",
                table: "MapElements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MapSymbolCategories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapSymbolCategories", x => x.Name);
                });

            migrationBuilder.InsertData(
                table: "MapSymbolCategories",
                column: "Name",
                values: new object[]
                {
                    "Feuerwehr",
                    "Objekte",
                    "Personen",
                    "Rettungsdienst"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapSymbolCategories");

            migrationBuilder.DropColumn(
                name: "IsVehicle",
                table: "MapSymbols");

            migrationBuilder.DropColumn(
                name: "IsVehicle",
                table: "MapElements");
        }
    }
}

