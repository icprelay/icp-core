using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icp.Persistence.Data.Runtime.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AllowedTriggerTypes = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Event"),
                    ParametersTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationTargets",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParametersTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretsTemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedTriggerTypes = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Event"),
                    Availability = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationTargets", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTimeZones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTimeZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationInstances",
                columns: table => new
                {
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    IntegrationTargetParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretRefsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubscribedEventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Event"),
                    ScheduleCron = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduleTimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NextDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduleVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationInstances", x => x.InstanceId);
                    table.ForeignKey(
                        name: "FK_IntegrationInstances_EventTypes_SubscribedEventType",
                        column: x => x.SubscribedEventType,
                        principalTable: "EventTypes",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationInstances_IntegrationTargets_IntegrationTarget",
                        column: x => x.IntegrationTarget,
                        principalTable: "IntegrationTargets",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputFullBlobPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_Runs_IntegrationInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "IntegrationInstances",
                        principalColumn: "InstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationInstances_CustomerId_IntegrationTarget",
                table: "IntegrationInstances",
                columns: new[] { "CustomerId", "IntegrationTarget" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationInstances_DeletedAt",
                table: "IntegrationInstances",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationInstances_IntegrationTarget",
                table: "IntegrationInstances",
                column: "IntegrationTarget");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationInstances_SubscribedEventType",
                table: "IntegrationInstances",
                column: "SubscribedEventType");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_CorrelationId",
                table: "Runs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_InstanceId_CreatedAt",
                table: "Runs",
                columns: new[] { "InstanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTimeZones_Enabled",
                table: "ScheduleTimeZones",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTimeZones_SortOrder",
                table: "ScheduleTimeZones",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "ScheduleTimeZones");

            migrationBuilder.DropTable(
                name: "IntegrationInstances");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropTable(
                name: "IntegrationTargets");
        }
    }
}
