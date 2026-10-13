using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39AppendPreservedIpmsOpmsTargetLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlServer = ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
            migrationBuilder.CreateTable(
                name: "IPMS_TargetOpmsLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: sqlServer ? "bigint" : "INTEGER", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    IpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpmsTargetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UnlinkedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UnlinkReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPMS_TargetOpmsLinks", x => x.Id);
                    table.CheckConstraint("CK_IPMS_TargetOpmsLinks_Lifecycle", "([IsActive] = 1 AND [UnlinkedAt] IS NULL AND [UnlinkedByUserId] IS NULL AND [UnlinkReason] IS NULL) OR ([IsActive] = 0 AND [UnlinkedAt] IS NOT NULL AND [UnlinkReason] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_IPMS_TargetOpmsLinks_AspNetUsers_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_TargetOpmsLinks_AspNetUsers_UnlinkedByUserId",
                        column: x => x.UnlinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_TargetOpmsLinks_IpmsTargets_IpmsTargetId",
                        column: x => x.IpmsTargetId,
                        principalTable: "IpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_TargetOpmsLinks_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IPMS_TargetOpmsLinks_OpmsTargets_OpmsTargetId",
                        column: x => x.OpmsTargetId,
                        principalTable: "OpmsTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            if (sqlServer)
            {
                migrationBuilder.Sql("""
                    INSERT INTO [IPMS_TargetOpmsLinks]
                        ([PublicId], [MunicipalityId], [IpmsTargetId], [OpmsTargetId], [LinkedAt], [LinkedByUserId],
                         [IsActive], [UnlinkedAt], [UnlinkedByUserId], [UnlinkReason])
                    SELECT NEWID(), i.[MunicipalityId], i.[Id], i.[RelatedOpmsTargetId],
                           COALESCE(i.[UpdatedAt], i.[CreatedAt], SYSUTCDATETIME()), NULL,
                           1, NULL, NULL, NULL
                    FROM [IpmsTargets] i
                    INNER JOIN [OpmsTargets] o
                        ON o.[Id] = i.[RelatedOpmsTargetId]
                       AND o.[MunicipalityId] = i.[MunicipalityId]
                    WHERE i.[MunicipalityId] IS NOT NULL
                      AND i.[RelatedOpmsTargetId] IS NOT NULL;
                    """);
            }
            else if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                const string sqliteGuid = "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random()) % 4 + 1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6)))";
                migrationBuilder.Sql($"""
                    INSERT INTO "IPMS_TargetOpmsLinks"
                        ("PublicId", "MunicipalityId", "IpmsTargetId", "OpmsTargetId", "LinkedAt", "LinkedByUserId",
                         "IsActive", "UnlinkedAt", "UnlinkedByUserId", "UnlinkReason", "RowVersion")
                    SELECT {sqliteGuid}, i."MunicipalityId", i."Id", i."RelatedOpmsTargetId",
                           COALESCE(i."UpdatedAt", i."CreatedAt", CURRENT_TIMESTAMP), NULL,
                           1, NULL, NULL, NULL, randomblob(16)
                    FROM "IpmsTargets" i
                    INNER JOIN "OpmsTargets" o
                        ON o."Id" = i."RelatedOpmsTargetId"
                       AND o."MunicipalityId" = i."MunicipalityId"
                    WHERE i."MunicipalityId" IS NOT NULL
                      AND i."RelatedOpmsTargetId" IS NOT NULL;
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_IpmsTargetId",
                table: "IPMS_TargetOpmsLinks",
                column: "IpmsTargetId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_LinkedByUserId",
                table: "IPMS_TargetOpmsLinks",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_MunicipalityId",
                table: "IPMS_TargetOpmsLinks",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_OpmsTargetId_IsActive",
                table: "IPMS_TargetOpmsLinks",
                columns: new[] { "OpmsTargetId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_PublicId",
                table: "IPMS_TargetOpmsLinks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IPMS_TargetOpmsLinks_UnlinkedByUserId",
                table: "IPMS_TargetOpmsLinks",
                column: "UnlinkedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPMS_TargetOpmsLinks");
        }
    }
}
