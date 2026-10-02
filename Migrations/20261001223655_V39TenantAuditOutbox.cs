using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39TenantAuditOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "Notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditTrails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "AuditTrails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AuditTrails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditTrails",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessEventOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEventOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEventOutbox_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                UPDATE n
                SET n.[PublicId] = NEWID(),
                    n.[MunicipalityId] = u.[MunicipalityId]
                FROM [Notifications] n
                INNER JOIN [AspNetUsers] u ON u.[Id] = n.[UserId];

                UPDATE a
                SET a.[PublicId] = NEWID(),
                    a.[MunicipalityId] = u.[MunicipalityId]
                FROM [AuditTrails] a
                INNER JOIN [AspNetUsers] u ON u.[Id] = a.[ChangedBy];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MunicipalityId",
                table: "Notifications",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PublicId",
                table: "Notifications",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_MunicipalityId_ChangedAt",
                table: "AuditTrails",
                columns: new[] { "MunicipalityId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_PublicId",
                table: "AuditTrails",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEventOutbox_MunicipalityId",
                table: "BusinessEventOutbox",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEventOutbox_ProcessedAt_AvailableAt",
                table: "BusinessEventOutbox",
                columns: new[] { "ProcessedAt", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEventOutbox_PublicId",
                table: "BusinessEventOutbox",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditTrails_Municipalities_MunicipalityId",
                table: "AuditTrails",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Municipalities_MunicipalityId",
                table: "Notifications",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditTrails_Municipalities_MunicipalityId",
                table: "AuditTrails");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Municipalities_MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "BusinessEventOutbox");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_PublicId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_MunicipalityId_ChangedAt",
                table: "AuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrails_PublicId",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditTrails");
        }
    }
}
