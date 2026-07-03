using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatsAssignedToFireDepartmentTrainingCourse : Migration
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
