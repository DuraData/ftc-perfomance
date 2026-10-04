using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39PeriodBasedKpiOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.AddColumn<int>(
                name: "OriginalOrderNumber",
                table: "OpmsTargets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RevisedOrderNumber",
                table: "OpmsTargets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OriginalOrderNumber",
                table: "IpmsTargets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RevisedOrderNumber",
                table: "IpmsTargets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                WITH OrderedTargets AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [MunicipalityId] ORDER BY [CreatedAt], [PublicId]) AS [Sequence]
                    FROM [OpmsTargets]
                )
                UPDATE target
                SET [OriginalOrderNumber] = ordered.[Sequence], [RevisedOrderNumber] = ordered.[Sequence]
                FROM [OpmsTargets] target
                INNER JOIN OrderedTargets ordered ON ordered.[Id] = target.[Id];

                WITH OrderedTargets AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [MunicipalityId] ORDER BY [CreatedAt], [PublicId]) AS [Sequence]
                    FROM [IpmsTargets]
                )
                UPDATE target
                SET [OriginalOrderNumber] = ordered.[Sequence], [RevisedOrderNumber] = ordered.[Sequence]
                FROM [IpmsTargets] target
                INNER JOIN OrderedTargets ordered ON ordered.[Id] = target.[Id];
                """);

            migrationBuilder.CreateTable(
                name: "KpiFieldRevisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RevisedValue = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiFieldRevisions", x => x.Id);
                    table.CheckConstraint("CK_KpiFieldRevisions_OneKpi", "CASE WHEN [OpmsTargetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IpmsTargetId] IS NULL THEN 0 ELSE 1 END = 1");
                    table.ForeignKey(
                        name: "FK_KpiFieldRevisions_AspNetUsers_RevisedByUserId",
                        column: x => x.RevisedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiFieldRevisions_IpmsTargets_IpmsTargetId",
                        column: x => x.IpmsTargetId,
                        principalTable: "IpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiFieldRevisions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiFieldRevisions_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_MunicipalityId_OriginalOrderNumber",
                table: "OpmsTargets",
                columns: new[] { "MunicipalityId", "OriginalOrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_MunicipalityId_RevisedOrderNumber",
                table: "OpmsTargets",
                columns: new[] { "MunicipalityId", "RevisedOrderNumber" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargets_OriginalOrderNumber",
                table: "OpmsTargets",
                sql: "[OriginalOrderNumber] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargets_RevisedOrderNumber",
                table: "OpmsTargets",
                sql: "[RevisedOrderNumber] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_MunicipalityId_OriginalOrderNumber",
                table: "IpmsTargets",
                columns: new[] { "MunicipalityId", "OriginalOrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_MunicipalityId_RevisedOrderNumber",
                table: "IpmsTargets",
                columns: new[] { "MunicipalityId", "RevisedOrderNumber" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsTargets_OriginalOrderNumber",
                table: "IpmsTargets",
                sql: "[OriginalOrderNumber] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsTargets_RevisedOrderNumber",
                table: "IpmsTargets",
                sql: "[RevisedOrderNumber] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_KpiFieldRevisions_IpmsTargetId_RecordedAt",
                table: "KpiFieldRevisions",
                columns: new[] { "IpmsTargetId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KpiFieldRevisions_MunicipalityId",
                table: "KpiFieldRevisions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiFieldRevisions_OpmsTargetId_RecordedAt",
                table: "KpiFieldRevisions",
                columns: new[] { "OpmsTargetId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KpiFieldRevisions_PublicId",
                table: "KpiFieldRevisions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiFieldRevisions_RevisedByUserId",
                table: "KpiFieldRevisions",
                column: "RevisedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KpiFieldRevisions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_MunicipalityId_OriginalOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_MunicipalityId_RevisedOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargets_OriginalOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargets_RevisedOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_MunicipalityId_OriginalOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_MunicipalityId_RevisedOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsTargets_OriginalOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsTargets_RevisedOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "OriginalOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedOrderNumber",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "OriginalOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RevisedOrderNumber",
                table: "IpmsTargets");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_MunicipalityId",
                table: "OpmsTargets",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_MunicipalityId",
                table: "IpmsTargets",
                column: "MunicipalityId");
        }
    }
}
