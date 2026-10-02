using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39PoeRetentionDisposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoeDisposalEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    PoeFileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoeDisposalEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoeDisposalEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeDisposalEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoeDisposalEvents_PoeFiles_PoeFileId",
                        column: x => x.PoeFileId,
                        principalTable: "PoeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoeDisposalEvents_ActorUserId",
                table: "PoeDisposalEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeDisposalEvents_MunicipalityId",
                table: "PoeDisposalEvents",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PoeDisposalEvents_PoeFileId_DisposalId_Action",
                table: "PoeDisposalEvents",
                columns: new[] { "PoeFileId", "DisposalId", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoeDisposalEvents_PublicId",
                table: "PoeDisposalEvents",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoeDisposalEvents");
        }
    }
}
