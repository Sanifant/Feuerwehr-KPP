using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItemRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "InventoryItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemRelationshipLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRelationshipLabels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipLabelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemRelationships_InventoryItems_ChildItemId",
                        column: x => x.ChildItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemRelationships_InventoryItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItemRelationships_ItemRelationshipLabels_Relations~",
                        column: x => x.RelationshipLabelId,
                        principalTable: "ItemRelationshipLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_Name",
                table: "InventoryCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemRelationships_ChildItemId",
                table: "InventoryItemRelationships",
                column: "ChildItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemRelationships_ParentItemId_ChildItemId_Relatio~",
                table: "InventoryItemRelationships",
                columns: new[] { "ParentItemId", "ChildItemId", "RelationshipLabelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemRelationships_RelationshipLabelId",
                table: "InventoryItemRelationships",
                column: "RelationshipLabelId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_InventoryCategories_CategoryId",
                table: "InventoryItems",
                column: "CategoryId",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_InventoryCategories_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.DropTable(
                name: "InventoryItemRelationships");

            migrationBuilder.DropTable(
                name: "ItemRelationshipLabels");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InventoryItems");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
