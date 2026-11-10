using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261110090000_AddMissingIsVehicleToMapElements")]
    /// <inheritdoc />
    public partial class AddMissingIsVehicleToMapElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"MapElements\" ADD COLUMN IF NOT EXISTS \"IsVehicle\" boolean NOT NULL DEFAULT FALSE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"MapElements\" DROP COLUMN IF EXISTS \"IsVehicle\";");
        }
    }
}
