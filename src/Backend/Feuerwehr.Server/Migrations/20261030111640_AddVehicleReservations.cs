using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FireDepartmentId = table.Column<int>(type: "integer", nullable: false),
                    RadioCallSign = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LicensePlate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EquipmentOfficerId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_AspNetUsers_EquipmentOfficerId",
                        column: x => x.EquipmentOfficerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_FireDepartment_FireDepartmentId",
                        column: x => x.FireDepartmentId,
                        principalTable: "FireDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequesterId = table.Column<string>(type: "text", nullable: false),
                    RequesterName = table.Column<string>(type: "text", nullable: false),
                    RequesterEmail = table.Column<string>(type: "text", nullable: false),
                    RadioCallSign = table.Column<string>(type: "text", nullable: false),
                    FireDepartmentName = table.Column<string>(type: "text", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsDirectBooking = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedById = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleReservations", x => x.Id);
                    table.CheckConstraint("CK_VehicleReservation_Dates", "\"From\" <= \"To\"");
                    table.ForeignKey(
                        name: "FK_VehicleReservations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleBookingDays",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleBookingDays", x => new { x.VehicleId, x.Date });
                    table.ForeignKey(
                        name: "FK_VehicleBookingDays_VehicleReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "VehicleReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleBookingDays_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookingDays_ReservationId",
                table: "VehicleBookingDays",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEvents_VehicleId_OccurredAtUtc",
                table: "VehicleEvents",
                columns: new[] { "VehicleId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMailMessages_ReservationId_Kind",
                table: "VehicleMailMessages",
                columns: new[] { "ReservationId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMailMessages_SentAtUtc_NextAttemptAtUtc",
                table: "VehicleMailMessages",
                columns: new[] { "SentAtUtc", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_CommandId",
                table: "VehicleReservations",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_RequesterId",
                table: "VehicleReservations",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_VehicleId_Status_From_To",
                table: "VehicleReservations",
                columns: new[] { "VehicleId", "Status", "From", "To" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_EquipmentOfficerId",
                table: "Vehicles",
                column: "EquipmentOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_FireDepartmentId_IsActive",
                table: "Vehicles",
                columns: new[] { "FireDepartmentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleBookingDays");

            migrationBuilder.DropTable(
                name: "VehicleEvents");

            migrationBuilder.DropTable(
                name: "VehicleMailMessages");

            migrationBuilder.DropTable(
                name: "VehicleReservations");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}

