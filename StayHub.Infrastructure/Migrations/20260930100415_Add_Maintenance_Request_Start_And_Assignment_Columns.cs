using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Maintenance_Request_Start_And_Assignment_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "assigned_to_on_utc",
                table: "maintenance_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_to_user_id",
                table: "maintenance_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_on_utc",
                table: "maintenance_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_assigned_to_user_id",
                table: "maintenance_requests",
                column: "assigned_to_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_maintenance_requests_user_assigned_to_user_id",
                table: "maintenance_requests",
                column: "assigned_to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_maintenance_requests_user_assigned_to_user_id",
                table: "maintenance_requests");

            migrationBuilder.DropIndex(
                name: "ix_maintenance_requests_assigned_to_user_id",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "assigned_to_on_utc",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "assigned_to_user_id",
                table: "maintenance_requests");

            migrationBuilder.DropColumn(
                name: "start_on_utc",
                table: "maintenance_requests");
        }
    }
}
