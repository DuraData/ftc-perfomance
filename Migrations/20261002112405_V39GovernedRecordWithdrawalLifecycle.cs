using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedRecordWithdrawalLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReasonForWithdrawal",
                table: "OpmsTargets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "OpmsTargets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawnByUserId",
                table: "OpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "OpmsSubmissions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawnByUserId",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReasonForWithdrawal",
                table: "IpmsTargets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "IpmsTargets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawnByUserId",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "IpmsSubmissions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawnByUserId",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GovernedRecordLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernedRecordLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernedRecordLifecycleEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GovernedRecordLifecycleEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve legacy soft-disabled/withdrawn rows while bringing them under
            // the governed lifecycle invariant. The SQL is supported by both SQL
            // Server and SQLite so development and deployment use the same migration.
            migrationBuilder.Sql("""
                UPDATE [OpmsTargets]
                SET [ReasonForWithdrawal] = COALESCE(NULLIF(TRIM([ReasonForWithdrawal]), ''), 'Migrated legacy withdrawal'),
                    [WithdrawnAt] = COALESCE([WithdrawnAt], [CreatedAt])
                WHERE [IsWithdrawn] = 1;
                """);
            migrationBuilder.Sql("""
                UPDATE [IpmsTargets]
                SET [ReasonForWithdrawal] = COALESCE(NULLIF(TRIM([ReasonForWithdrawal]), ''), 'Migrated legacy withdrawal'),
                    [WithdrawnAt] = COALESCE([WithdrawnAt], [CreatedAt])
                WHERE [IsWithdrawn] = 1;
                """);
            migrationBuilder.Sql("""
                UPDATE [OpmsSubmissions]
                SET [WithdrawalReason] = COALESCE(NULLIF(TRIM([WithdrawalReason]), ''), 'Migrated legacy deactivation'),
                    [WithdrawnAt] = COALESCE([WithdrawnAt], [UpdatedOn], [CreatedOn], [CreatedAt])
                WHERE [IsDisabled] = 1;
                """);
            migrationBuilder.Sql("""
                UPDATE [IpmsSubmissions]
                SET [WithdrawalReason] = COALESCE(NULLIF(TRIM([WithdrawalReason]), ''), 'Migrated legacy deactivation'),
                    [WithdrawnAt] = COALESCE([WithdrawnAt], [UpdatedOn], [CreatedOn], [CreatedAt])
                WHERE [IsDisabled] = 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsTargets_WithdrawalMetadata",
                table: "OpmsTargets",
                sql: "[IsWithdrawn] = 0 OR ([ReasonForWithdrawal] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsSubmissions_WithdrawalMetadata",
                table: "OpmsSubmissions",
                sql: "[IsDisabled] = 0 OR ([WithdrawalReason] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsTargets_WithdrawalMetadata",
                table: "IpmsTargets",
                sql: "[IsWithdrawn] = 0 OR ([ReasonForWithdrawal] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsSubmissions_WithdrawalMetadata",
                table: "IpmsSubmissions",
                sql: "[IsDisabled] = 0 OR ([WithdrawalReason] IS NOT NULL AND [WithdrawnAt] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_GovernedRecordLifecycleEvents_ActorUserId",
                table: "GovernedRecordLifecycleEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernedRecordLifecycleEvents_MunicipalityId_AggregateType_AggregateId_OccurredAt",
                table: "GovernedRecordLifecycleEvents",
                columns: new[] { "MunicipalityId", "AggregateType", "AggregateId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GovernedRecordLifecycleEvents_PublicId",
                table: "GovernedRecordLifecycleEvents",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GovernedRecordLifecycleEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsTargets_WithdrawalMetadata",
                table: "OpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsSubmissions_WithdrawalMetadata",
                table: "OpmsSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsTargets_WithdrawalMetadata",
                table: "IpmsTargets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsSubmissions_WithdrawalMetadata",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "WithdrawnByUserId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnByUserId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "WithdrawnByUserId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "WithdrawnByUserId",
                table: "IpmsSubmissions");

            migrationBuilder.AlterColumn<string>(
                name: "ReasonForWithdrawal",
                table: "OpmsTargets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReasonForWithdrawal",
                table: "IpmsTargets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
