using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddDelegationAndManagerHierarchySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DelegatorUserId",
                table: "UserAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserAssignments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFromUtc",
                table: "UserAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidToUtc",
                table: "UserAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignments_DelegatorUserId",
                table: "UserAssignments",
                column: "DelegatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ManagerUserId",
                table: "AspNetUsers",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ManagerUserId",
                table: "AspNetUsers",
                column: "ManagerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAssignments_AspNetUsers_DelegatorUserId",
                table: "UserAssignments",
                column: "DelegatorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ManagerUserId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAssignments_AspNetUsers_DelegatorUserId",
                table: "UserAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserAssignments_DelegatorUserId",
                table: "UserAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ManagerUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DelegatorUserId",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "ValidFromUtc",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "ValidToUtc",
                table: "UserAssignments");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "AspNetUsers");
        }
    }
}
