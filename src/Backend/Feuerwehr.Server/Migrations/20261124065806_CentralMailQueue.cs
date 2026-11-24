using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace de.openelp.feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class CentralMailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentAtUtc",
                table: "EquipmentReminderDeliveries",
                newName: "QueuedAtUtc");

            migrationBuilder.CreateTable(
                name: "MailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeduplicationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailMessages_AspNetUsers_modified_by",
                        column: x => x.modified_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Preserve queued and delivered messages, including their retry state.
            migrationBuilder.Sql("""
                INSERT INTO "MailMessages" ("Id", "Source", "DeduplicationKey", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError", "modified_by", "modified_on")
                SELECT m."Id", 'vehicle', m."ReservationId"::text || ':' || m."Kind", m."Recipient", m."Subject", m."TextBody", m."HtmlBody",
                    m."CreatedAtUtc", m."NextAttemptAtUtc", m."LeaseUntilUtc", m."SentAtUtc", m."Attempts", m."LastError", u."Id", m."CreatedAtUtc"
                FROM "VehicleMailMessages" m
                LEFT JOIN "VehicleReservations" r ON r."Id" = m."ReservationId"
                LEFT JOIN "AspNetUsers" u ON u."Id" = r."RequesterId";

                INSERT INTO "MailMessages" ("Id", "Source", "DeduplicationKey", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError", "modified_on")
                SELECT "Id", 'hydrant', 'legacy:' || "HydrantId"::text || ':' || "Id"::text, "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError", "CreatedAtUtc"
                FROM "HydrantMailMessages";
                """);

            migrationBuilder.DropTable(name: "HydrantMailMessages");
            migrationBuilder.DropTable(name: "VehicleMailMessages");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_modified_by",
                table: "MailMessages",
                column: "modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_SentAtUtc_NextAttemptAtUtc",
                table: "MailMessages",
                columns: new[] { "SentAtUtc", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_Source_DeduplicationKey",
                table: "MailMessages",
                columns: new[] { "Source", "DeduplicationKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Older versions cannot represent other modules' messages. Refuse a lossy downgrade.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "MailMessages" WHERE "Source" NOT IN ('vehicle', 'hydrant')) THEN
                        RAISE EXCEPTION 'MailMessages contains messages from modules unsupported by the previous schema.';
                    END IF;
                END $$;
                """);

            migrationBuilder.RenameColumn(
                name: "QueuedAtUtc",
                table: "EquipmentReminderDeliveries",
                newName: "SentAtUtc");

            migrationBuilder.CreateTable(
                name: "HydrantMailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    HydrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydrantMailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMailMessages", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO "VehicleMailMessages" ("Id", "ReservationId", "Kind", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError")
                SELECT "Id", split_part("DeduplicationKey", ':', 1)::uuid, split_part("DeduplicationKey", ':', 2),
                    "Recipient", "Subject", "TextBody", COALESCE("HtmlBody", ''), "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError"
                FROM "MailMessages" WHERE "Source" = 'vehicle';

                INSERT INTO "HydrantMailMessages" ("Id", "HydrantId", "Recipient", "Subject", "TextBody", "HtmlBody",
                    "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError")
                SELECT "Id", CASE WHEN "DeduplicationKey" LIKE 'legacy:%' THEN split_part("DeduplicationKey", ':', 2)::uuid
                    ELSE '00000000-0000-0000-0000-000000000000'::uuid END,
                    "Recipient", "Subject", "TextBody", COALESCE("HtmlBody", ''), "CreatedAtUtc", "NextAttemptAtUtc", "LeaseUntilUtc", "SentAtUtc", "Attempts", "LastError"
                FROM "MailMessages" WHERE "Source" = 'hydrant';
                """);

            migrationBuilder.DropTable(name: "MailMessages");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMailMessages_ReservationId_Kind",
                table: "VehicleMailMessages",
                columns: new[] { "ReservationId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMailMessages_SentAtUtc_NextAttemptAtUtc",
                table: "VehicleMailMessages",
                columns: new[] { "SentAtUtc", "NextAttemptAtUtc" });
        }
    }
}
