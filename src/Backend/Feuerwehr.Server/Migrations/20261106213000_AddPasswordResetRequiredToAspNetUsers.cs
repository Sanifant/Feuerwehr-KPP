using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261106213000_AddPasswordResetRequiredToAspNetUsers")]
    /// <inheritdoc />
    public partial class AddPasswordResetRequiredToAspNetUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PasswordResetRequired",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetRequired",
                table: "AspNetUsers");
        }
    }
}
