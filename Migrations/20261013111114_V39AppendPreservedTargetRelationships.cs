using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39AppendPreservedTargetRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetWards_OpmsTargetId_WardId",
                table: "OpmsTargetWards");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetVoteNumbers_OpmsTargetId_VoteNumberId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetAdditionalAssignees_OpmsTargetId_UserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OpmsTargetWards",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedByUserId",
                table: "OpmsTargetWards",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargetWards",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkReason",
                table: "OpmsTargetWards",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlinkedAt",
                table: "OpmsTargetWards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkedByUserId",
                table: "OpmsTargetWards",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OpmsTargetVoteNumbers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargetVoteNumbers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkReason",
                table: "OpmsTargetVoteNumbers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlinkedAt",
                table: "OpmsTargetVoteNumbers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OpmsTargets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OpmsTargetAdditionalAssignees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargetAdditionalAssignees",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkReason",
                table: "OpmsTargetAdditionalAssignees",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlinkedAt",
                table: "OpmsTargetAdditionalAssignees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_LinkedByUserId",
                table: "OpmsTargetWards",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_OpmsTargetId_WardId",
                table: "OpmsTargetWards",
                columns: new[] { "OpmsTargetId", "WardId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_UnlinkedByUserId",
                table: "OpmsTargetWards",
                column: "UnlinkedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargetWards_UnlinkLifecycle",
                table: "OpmsTargetWards",
                sql: "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_LinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_OpmsTargetId_VoteNumberId",
                table: "OpmsTargetVoteNumbers",
                columns: new[] { "OpmsTargetId", "VoteNumberId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                column: "UnlinkedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargetVoteNumbers_UnlinkLifecycle",
                table: "OpmsTargetVoteNumbers",
                sql: "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_OpmsTargetId_UserId",
                table: "OpmsTargetAdditionalAssignees",
                columns: new[] { "OpmsTargetId", "UserId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                column: "UnlinkedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargetAdditionalAssignees_UnlinkLifecycle",
                table: "OpmsTargetAdditionalAssignees",
                sql: "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkedByUserId] IS NOT NULL AND [UnlinkReason] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetAdditionalAssignees_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                column: "LinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetAdditionalAssignees_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees",
                column: "UnlinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetVoteNumbers_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                column: "LinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetVoteNumbers_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers",
                column: "UnlinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetWards_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetWards",
                column: "LinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargetWards_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetWards",
                column: "UnlinkedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetAdditionalAssignees_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetAdditionalAssignees_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetVoteNumbers_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetVoteNumbers_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetWards_AspNetUsers_LinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargetWards_AspNetUsers_UnlinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetWards_LinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetWards_OpmsTargetId_WardId",
                table: "OpmsTargetWards");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetWards_UnlinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargetWards_UnlinkLifecycle",
                table: "OpmsTargetWards");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetVoteNumbers_LinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetVoteNumbers_OpmsTargetId_VoteNumberId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetVoteNumbers_UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargetVoteNumbers_UnlinkLifecycle",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetAdditionalAssignees_LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetAdditionalAssignees_OpmsTargetId_UserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargetAdditionalAssignees_UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargetAdditionalAssignees_UnlinkLifecycle",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "LinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "UnlinkReason",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "UnlinkedAt",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "UnlinkedByUserId",
                table: "OpmsTargetWards");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "LinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "UnlinkReason",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "UnlinkedAt",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "UnlinkedByUserId",
                table: "OpmsTargetVoteNumbers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "LinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "UnlinkReason",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "UnlinkedAt",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.DropColumn(
                name: "UnlinkedByUserId",
                table: "OpmsTargetAdditionalAssignees");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetWards_OpmsTargetId_WardId",
                table: "OpmsTargetWards",
                columns: new[] { "OpmsTargetId", "WardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetVoteNumbers_OpmsTargetId_VoteNumberId",
                table: "OpmsTargetVoteNumbers",
                columns: new[] { "OpmsTargetId", "VoteNumberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargetAdditionalAssignees_OpmsTargetId_UserId",
                table: "OpmsTargetAdditionalAssignees",
                columns: new[] { "OpmsTargetId", "UserId" },
                unique: true);
        }
    }
}
