using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icp.Persistence.Data.Runtime.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStepForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EventSteps_ExecutionId",
                table: "EventSteps",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSteps_InstanceId",
                table: "EventSteps",
                column: "InstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventSteps_EventTraces_EventId",
                table: "EventSteps",
                column: "EventId",
                principalTable: "EventTraces",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventSteps_IntegrationInstances_InstanceId",
                table: "EventSteps",
                column: "InstanceId",
                principalTable: "IntegrationInstances",
                principalColumn: "InstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventSteps_Runs_ExecutionId",
                table: "EventSteps",
                column: "ExecutionId",
                principalTable: "Runs",
                principalColumn: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventSteps_EventTraces_EventId",
                table: "EventSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_EventSteps_IntegrationInstances_InstanceId",
                table: "EventSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_EventSteps_Runs_ExecutionId",
                table: "EventSteps");

            migrationBuilder.DropIndex(
                name: "IX_EventSteps_ExecutionId",
                table: "EventSteps");

            migrationBuilder.DropIndex(
                name: "IX_EventSteps_InstanceId",
                table: "EventSteps");
        }
    }
}
