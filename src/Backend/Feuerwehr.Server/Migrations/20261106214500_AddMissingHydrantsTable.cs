using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261106214500_AddMissingHydrantsTable")]
    /// <inheritdoc />
    public partial class AddMissingHydrantsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS "Hydrants_Id_seq" AS integer START WITH 1 INCREMENT BY 1;

                CREATE TABLE IF NOT EXISTS "Hydrants" (
                    "Id" integer NOT NULL DEFAULT nextval('"Hydrants_Id_seq"'),
                    "Street" text NOT NULL,
                    "HouseNumber" text NOT NULL,
                    "PostalCode" text NOT NULL,
                    "City" text NOT NULL,
                    "AdditionalInfo" text NULL,
                    "Longitude" double precision NOT NULL,
                    "Latitude" double precision NOT NULL,
                    "DN_Diameter" integer NOT NULL,
                    "Type" character varying(50) NOT NULL,
                    "WaterSource" character varying(50) NOT NULL,
                    "Status" character varying(30) NOT NULL,
                    "Notes" character varying(1000) NULL,
                    CONSTRAINT "PK_Hydrants" PRIMARY KEY ("Id")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS "Hydrants";
                DROP SEQUENCE IF EXISTS "Hydrants_Id_seq";
                """);
        }
    }
}
