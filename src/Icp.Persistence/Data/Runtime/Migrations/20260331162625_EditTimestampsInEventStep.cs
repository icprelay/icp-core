using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icp.Persistence.Data.Runtime.Migrations
{
    /// <inheritdoc />
    public partial class EditTimestampsInEventStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "EventSteps");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "EventSteps",
                newName: "TimestampUtc");

            migrationBuilder.RenameIndex(
                name: "IX_EventSteps_EventId_StartedAtUtc",
                table: "EventSteps",
                newName: "IX_EventSteps_EventId_TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "EventSteps",
                newName: "StartedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_EventSteps_EventId_TimestampUtc",
                table: "EventSteps",
                newName: "IX_EventSteps_EventId_StartedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "EventSteps",
                type: "datetime2",
                nullable: true);
        }
    }
}
