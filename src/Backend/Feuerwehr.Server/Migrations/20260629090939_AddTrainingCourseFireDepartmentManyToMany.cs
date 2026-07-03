using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingCourseFireDepartmentManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FireDepartment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireDepartment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCourse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCourse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FireDepartmentTrainingCourses",
                columns: table => new
                {
                    FireDepartmentId = table.Column<int>(type: "integer", nullable: false),
                    TrainingCourseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireDepartmentTrainingCourses", x => new { x.FireDepartmentId, x.TrainingCourseId });
                    table.ForeignKey(
                        name: "FK_FireDepartmentTrainingCourses_FireDepartment_FireDepartment~",
                        column: x => x.FireDepartmentId,
                        principalTable: "FireDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FireDepartmentTrainingCourses_TrainingCourse_TrainingCourse~",
                        column: x => x.TrainingCourseId,
                        principalTable: "TrainingCourse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FireDepartmentTrainingCourses_TrainingCourseId",
                table: "FireDepartmentTrainingCourses",
                column: "TrainingCourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FireDepartmentTrainingCourses");

            migrationBuilder.DropTable(
                name: "FireDepartment");

            migrationBuilder.DropTable(
                name: "TrainingCourse");
        }
    }
}
