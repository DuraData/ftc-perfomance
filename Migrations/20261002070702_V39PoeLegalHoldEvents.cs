using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39PoeLegalHoldEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoeLegalHoldEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    HoldReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoeLegalHoldEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoeLegalHoldEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeLegalHoldEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeLegalHoldEvents_PoeFiles_PoeFileId",
                        column: x => x.PoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoeLegalHoldEvents_ActorUserId",
                table: "PoeLegalHoldEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeLegalHoldEvents_MunicipalityId",
                table: "PoeLegalHoldEvents",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeLegalHoldEvents_PoeFileId_HoldId_Action",
                table: "PoeLegalHoldEvents",
                columns: new[] { "PoeFileId", "HoldId", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoeLegalHoldEvents_PublicId",
                table: "PoeLegalHoldEvents",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoeLegalHoldEvents");
        }
    }
}
