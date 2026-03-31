using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icp.Persistence.Data.Runtime.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTracing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogicAppRunId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTraces",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchedInstanceCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTraces", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventSteps_EventId",
                table: "EventSteps",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSteps_EventId_StartedAtUtc",
                table: "EventSteps",
                columns: new[] { "EventId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTraces_AccountKey",
                table: "EventTraces",
                column: "AccountKey");

            migrationBuilder.CreateIndex(
                name: "IX_EventTraces_CorrelationId",
                table: "EventTraces",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTraces_ReceivedAtUtc",
                table: "EventTraces",
                column: "ReceivedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSteps");

            migrationBuilder.DropTable(
                name: "EventTraces");
        }
    }
}
