using de.openelp.feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    [DbContext(typeof(FeuerwehrDbContext))]
    [Migration("20261106220000_AddSeatsAssignedToFireDepartmentTrainingCourses")]
    /// <inheritdoc />
    public partial class AddSeatsAssignedToFireDepartmentTrainingCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatsAssigned",
                table: "FireDepartmentTrainingCourses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatsAssigned",
                table: "FireDepartmentTrainingCourses");
        }
    }
}
