using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icp.Persistence.Data.Runtime.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "IntegrationInstances",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "IntegrationAccounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    InboundKeyHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationAccounts", x => x.AccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationInstances_AccountId",
                table: "IntegrationInstances",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAccounts_ExternalCustomerId",
                table: "IntegrationAccounts",
                column: "ExternalCustomerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationInstances_IntegrationAccounts_AccountId",
                table: "IntegrationInstances",
                column: "AccountId",
                principalTable: "IntegrationAccounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationInstances_IntegrationAccounts_AccountId",
                table: "IntegrationInstances");

            migrationBuilder.DropTable(
                name: "IntegrationAccounts");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationInstances_AccountId",
                table: "IntegrationInstances");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "IntegrationInstances");
        }
    }
}
