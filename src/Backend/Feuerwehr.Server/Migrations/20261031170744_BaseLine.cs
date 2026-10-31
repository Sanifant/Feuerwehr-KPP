using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace De.Openelp.Feuerwehr.Server.Migrations
{
    /// <inheritdoc />
    public partial class BaseLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "DiaryEntryNumberSeq");

            migrationBuilder.CreateSequence<int>(
                name: "Hydrants_Id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "InventoryBarcodeSequence",
                maxValue: 999L);

            migrationBuilder.CreateSequence(
                name: "MapAuditSequence");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

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
                name: "Hydrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"Hydrants_Id_seq\"')"),
                    Street = table.Column<string>(type: "text", nullable: false),
                    HouseNumber = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    DN_Diameter = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WaterSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hydrants", x => x.Id);
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
                    AlarmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "InventoryCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
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
                name: "MapSymbolCategories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapSymbolCategories", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "MapSymbols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SvgContent = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVehicle = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapSymbols", x => x.Id);
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
                name: "VehicleStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllowsBooking = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    FireDepartmentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordResetRequired = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_FireDepartment_FireDepartmentId",
                        column: x => x.FireDepartmentId,
                        principalTable: "FireDepartment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                name: "MapElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SymbolId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SymbolSvg = table.Column<string>(type: "text", nullable: true),
                    IsVehicle = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "EquipmentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InspectionIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    InspectionInstructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentTypes_InventoryCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "InventoryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FireDepartmentTrainingCourses",
                columns: table => new
                {
                    FireDepartmentId = table.Column<int>(type: "integer", nullable: false),
                    TrainingCourseId = table.Column<int>(type: "integer", nullable: false),
                    SeatsAssigned = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    VehicleTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleStatusId = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("5032f99d-4352-4000-8000-000000000001")),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleStatuses_VehicleStatusId",
                        column: x => x.VehicleStatusId,
                        principalTable: "VehicleStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarcodeNumber = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"InventoryBarcodeSequence\"')"),
                    EquipmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LastInspectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentTypes",
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
                name: "EquipmentInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    InspectorName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EquipmentTypeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InspectionIntervalMonths = table.Column<int>(type: "integer", nullable: false),
                    InspectionInstructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentInspections_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentReminderDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentReminderDeliveries_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
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

            migrationBuilder.InsertData(
                table: "InventoryCategories",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("f3050002-0000-0000-0000-000000000001"), "Löschgeräte", "LÖSCHGERÄTE" },
                    { new Guid("f3050002-0000-0000-0000-000000000002"), "Armaturen", "ARMATUREN" },
                    { new Guid("f3050002-0000-0000-0000-000000000003"), "Beleuchtung", "BELEUCHTUNG" }
                });

            migrationBuilder.InsertData(
                table: "MapSymbolCategories",
                column: "Name",
                values: new object[]
                {
                    "Feuerwehr",
                    "Objekte",
                    "Personen",
                    "Rettungsdienst"
                });

            migrationBuilder.InsertData(
                table: "VehicleStatuses",
                columns: new[] { "Id", "AllowsBooking", "IsActive", "Name", "NormalizedName", "Version" },
                values: new object[,]
                {
                    { new Guid("5032f99d-4352-4000-8000-000000000001"), true, true, "Betriebsbereit", "BETRIEBSBEREIT", 1 },
                    { new Guid("5032f99d-4352-4000-8000-000000000002"), false, true, "Wartung", "WARTUNG", 1 },
                    { new Guid("5032f99d-4352-4000-8000-000000000003"), false, true, "Abgemeldet", "ABGEMELDET", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FireDepartmentId",
                table: "AspNetUsers",
                column: "FireDepartmentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

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
                name: "IX_Equipment_BarcodeNumber",
                table: "Equipment",
                column: "BarcodeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_EquipmentTypeId",
                table: "Equipment",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentInspections_CommandId",
                table: "EquipmentInspections",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentInspections_EquipmentId_RecordedAtUtc",
                table: "EquipmentInspections",
                columns: new[] { "EquipmentId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentReminderDeliveries_EquipmentId_RecipientId_DueOn",
                table: "EquipmentReminderDeliveries",
                columns: new[] { "EquipmentId", "RecipientId", "DueOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_CategoryId",
                table: "EquipmentTypes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FireDepartmentTrainingCourses_TrainingCourseId",
                table: "FireDepartmentTrainingCourses",
                column: "TrainingCourseId");

            migrationBuilder.CreateIndex(
                name: "UX_Incidents_OnlyOneActive",
                table: "Incidents",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_NormalizedName",
                table: "InventoryCategories",
                column: "NormalizedName",
                unique: true);

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
                name: "IX_MapSymbols_Category_IsActive",
                table: "MapSymbols",
                columns: new[] { "Category", "IsActive" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleStatusId",
                table: "Vehicles",
                column: "VehicleStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleTypeId",
                table: "Vehicles",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleStatuses_NormalizedName",
                table: "VehicleStatuses",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_NormalizedName",
                table: "VehicleTypes",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DiaryCategories");

            migrationBuilder.DropTable(
                name: "DiaryEntryRevisions");

            migrationBuilder.DropTable(
                name: "EditorLeases");

            migrationBuilder.DropTable(
                name: "EquipmentInspections");

            migrationBuilder.DropTable(
                name: "EquipmentReminderDeliveries");

            migrationBuilder.DropTable(
                name: "FireDepartmentTrainingCourses");

            migrationBuilder.DropTable(
                name: "Hydrants");

            migrationBuilder.DropTable(
                name: "MapAuditEvents");

            migrationBuilder.DropTable(
                name: "MapElements");

            migrationBuilder.DropTable(
                name: "MapSymbolCategories");

            migrationBuilder.DropTable(
                name: "MapSymbols");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProcessedCommands");

            migrationBuilder.DropTable(
                name: "VehicleBookingDays");

            migrationBuilder.DropTable(
                name: "VehicleEvents");

            migrationBuilder.DropTable(
                name: "VehicleMailMessages");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "DiaryEntries");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "TrainingCourse");

            migrationBuilder.DropTable(
                name: "VehicleReservations");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "EquipmentTypes");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "VehicleStatuses");

            migrationBuilder.DropTable(
                name: "VehicleTypes");

            migrationBuilder.DropTable(
                name: "FireDepartment");

            migrationBuilder.DropSequence(
                name: "DiaryEntryNumberSeq");

            migrationBuilder.DropSequence(
                name: "Hydrants_Id_seq");

            migrationBuilder.DropSequence(
                name: "InventoryBarcodeSequence");

            migrationBuilder.DropSequence(
                name: "MapAuditSequence");
        }
    }
}
