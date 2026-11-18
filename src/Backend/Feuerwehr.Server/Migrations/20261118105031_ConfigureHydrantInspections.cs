using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureHydrantInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HydrantInspections_Hydrants_HydrantId1",
                table: "HydrantInspections");

            migrationBuilder.DropIndex(
                name: "IX_HydrantInspections_HydrantId1",
                table: "HydrantInspections");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "HydrantInspections",
                newName: "NewStatus");

            // Keep the actual integer foreign key, replacing the unused UUID property.
            migrationBuilder.DropColumn(
                name: "HydrantId",
                table: "HydrantInspections");

            migrationBuilder.RenameColumn(
                name: "HydrantId1",
                table: "HydrantInspections",
                newName: "HydrantId");

            // There are no existing inspections. Do not manufacture a historical status.
            migrationBuilder.AddColumn<int>(
                name: "PreviousStatus",
                table: "HydrantInspections",
                type: "integer",
                nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "HydrantInspections",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "HydrantInspections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoStorageKey",
                table: "HydrantInspections",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HydrantInspections_HydrantId",
                table: "HydrantInspections",
                column: "HydrantId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HydrantInspections_CommentLength",
                table: "HydrantInspections",
                sql: "\"Comment\" IS NULL OR length(\"Comment\") <= 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HydrantInspections_NewStatus",
                table: "HydrantInspections",
                sql: "\"NewStatus\" IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HydrantInspections_Photo",
                table: "HydrantInspections",
                sql: "(\"PhotoStorageKey\" IS NULL AND \"PhotoContentType\" IS NULL) OR (\"PhotoStorageKey\" IS NOT NULL AND length(\"PhotoStorageKey\") > 0 AND \"PhotoContentType\" IS NOT NULL AND \"PhotoContentType\" = 'image/jpeg')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HydrantInspections_PreviousStatus",
                table: "HydrantInspections",
                sql: "\"PreviousStatus\" IN (1, 2, 3, 4)");

            migrationBuilder.AddForeignKey(
                name: "FK_HydrantInspections_Hydrants_HydrantId",
                table: "HydrantInspections",
                column: "HydrantId",
                principalTable: "Hydrants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HydrantInspections_Hydrants_HydrantId",
                table: "HydrantInspections");

            migrationBuilder.DropIndex(
                name: "IX_HydrantInspections_HydrantId",
                table: "HydrantInspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HydrantInspections_CommentLength",
                table: "HydrantInspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HydrantInspections_NewStatus",
                table: "HydrantInspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HydrantInspections_Photo",
                table: "HydrantInspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HydrantInspections_PreviousStatus",
                table: "HydrantInspections");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "HydrantInspections");

            migrationBuilder.DropColumn(
                name: "PhotoStorageKey",
                table: "HydrantInspections");

            migrationBuilder.DropColumn(
                name: "PreviousStatus",
                table: "HydrantInspections");

            migrationBuilder.RenameColumn(
                name: "NewStatus",
                table: "HydrantInspections",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "HydrantId",
                table: "HydrantInspections",
                newName: "HydrantId1");

            migrationBuilder.AddColumn<Guid>(
                name: "HydrantId",
                table: "HydrantInspections",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AlterColumn<Guid>(
                name: "HydrantId",
                table: "HydrantInspections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: Guid.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "HydrantInspections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HydrantInspections_HydrantId1",
                table: "HydrantInspections",
                column: "HydrantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_HydrantInspections_Hydrants_HydrantId1",
                table: "HydrantInspections",
                column: "HydrantId1",
                principalTable: "Hydrants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
