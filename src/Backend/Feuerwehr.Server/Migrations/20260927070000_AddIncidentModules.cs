using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feuerwehr.Server.Migrations
{
    public partial class AddIncidentModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(
                name: "DiaryEntryNumberSeq");

            migrationBuilder.CreateSequence<long>(
                name: "MapAuditSequence");

            migrationBuilder.CreateTable(
                name: "DiaryCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IncidentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Keyword = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OriginLongitude = table.Column<double>(type: "double precision", nullable: false),
                    OriginLatitude = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ClosedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PayloadHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiaryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryNumber = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"DiaryEntryNumberSeq\"')"),
                    CurrentRevision = table.Column<int>(type: "integer", nullable: false),
                    IsCanceled = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaryEntries_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EditorLeases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LeaseToken = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorLeases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditorLeases_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"MapAuditSequence\"')"),
                    CommandId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ElementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BeforeStateJson = table.Column<string>(type: "text", nullable: false),
                    AfterStateJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SymbolId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RadioCallName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Strength = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ColorHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeometryJson = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UpdatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapElements_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiaryEntryRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CategoryNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Sender = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TransmissionType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryEntryRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaryEntryRevisions_DiaryEntries_DiaryEntryId",
                        column: x => x.DiaryEntryId,
                        principalTable: "DiaryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryCategories_Code",
                table: "DiaryCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_IncidentId_EntryNumber",
                table: "DiaryEntries",
                columns: new[] { "IncidentId", "EntryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntryRevisions_DiaryEntryId_RevisionNumber",
                table: "DiaryEntryRevisions",
                columns: new[] { "DiaryEntryId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EditorLeases_IncidentId",
                table: "EditorLeases",
                column: "IncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Incidents_OnlyOneActive",
                table: "Incidents",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_MapAuditEvents_IncidentId_SequenceNumber",
                table: "MapAuditEvents",
                columns: new[] { "IncidentId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapElements_IncidentId_UpdatedAtUtc",
                table: "MapElements",
                columns: new[] { "IncidentId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedCommands_CommandId",
                table: "ProcessedCommands",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedCommands_UserId_Scope_TargetId",
                table: "ProcessedCommands",
                columns: new[] { "UserId", "Scope", "TargetId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DiaryCategories");
            migrationBuilder.DropTable(name: "DiaryEntryRevisions");
            migrationBuilder.DropTable(name: "EditorLeases");
            migrationBuilder.DropTable(name: "MapAuditEvents");
            migrationBuilder.DropTable(name: "MapElements");
            migrationBuilder.DropTable(name: "OutboxMessages");
            migrationBuilder.DropTable(name: "ProcessedCommands");
            migrationBuilder.DropTable(name: "DiaryEntries");
            migrationBuilder.DropTable(name: "Incidents");

            migrationBuilder.DropSequence(name: "DiaryEntryNumberSeq");
            migrationBuilder.DropSequence(name: "MapAuditSequence");
        }
    }
}
