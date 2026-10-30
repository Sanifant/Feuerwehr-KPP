using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class RequireInvitationPasswordReset : Migration
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

