using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261106221500_AddMissingMapSymbolCategoriesTable")]
    /// <inheritdoc />
    public partial class AddMissingMapSymbolCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "MapSymbolCategories" (
                    "Name" character varying(40) NOT NULL,
                    CONSTRAINT "PK_MapSymbolCategories" PRIMARY KEY ("Name")
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "MapSymbolCategories" ("Name") VALUES ('Feuerwehr') ON CONFLICT ("Name") DO NOTHING;
                INSERT INTO "MapSymbolCategories" ("Name") VALUES ('Rettungsdienst') ON CONFLICT ("Name") DO NOTHING;
                INSERT INTO "MapSymbolCategories" ("Name") VALUES ('Personen') ON CONFLICT ("Name") DO NOTHING;
                INSERT INTO "MapSymbolCategories" ("Name") VALUES ('Objekte') ON CONFLICT ("Name") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS "MapSymbolCategories";
                """);
        }
    }
}
