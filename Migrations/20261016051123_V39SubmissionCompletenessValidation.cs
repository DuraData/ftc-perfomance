using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39SubmissionCompletenessValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresPoe",
                table: "RatingSchemeValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredPoeDescription",
                table: "PerformancePeriodTargets",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPoe",
                table: "PerformancePeriodTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "OpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectiveMeasure",
                table: "OpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredPoeDescription",
                table: "OpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPoe",
                table: "OpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "IpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectiveMeasure",
                table: "IpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredPoeDescription",
                table: "IpmsSubmissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPoe",
                table: "IpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE OpmsSubmissions
                SET RequiredPoeDescription = (
                        SELECT RequiredPoeDescription FROM PerformancePeriodTargets
                        WHERE OpmsTargetId = OpmsSubmissions.OpmsTargetId
                          AND ReportingPeriodId = OpmsSubmissions.ReportingPeriodId),
                    RequiresPoe = COALESCE((
                        SELECT RequiresPoe FROM PerformancePeriodTargets
                        WHERE OpmsTargetId = OpmsSubmissions.OpmsTargetId
                          AND ReportingPeriodId = OpmsSubmissions.ReportingPeriodId), 0)
                WHERE ReportingPeriodId IS NOT NULL;

                UPDATE IpmsSubmissions
                SET RequiredPoeDescription = (
                        SELECT RequiredPoeDescription FROM PerformancePeriodTargets
                        WHERE IpmsTargetId = IpmsSubmissions.IpmsTargetId
                          AND ReportingPeriodId = IpmsSubmissions.ReportingPeriodId),
                    RequiresPoe = COALESCE((
                        SELECT RequiresPoe FROM PerformancePeriodTargets
                        WHERE IpmsTargetId = IpmsSubmissions.IpmsTargetId
                          AND ReportingPeriodId = IpmsSubmissions.ReportingPeriodId), 0)
                WHERE ReportingPeriodId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresPoe",
                table: "RatingSchemeValues");

            migrationBuilder.DropColumn(
                name: "RequiredPoeDescription",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RequiresPoe",
                table: "PerformancePeriodTargets");

            migrationBuilder.DropColumn(
                name: "RequiredPoeDescription",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "RequiresPoe",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "RequiredPoeDescription",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "RequiresPoe",
                table: "IpmsSubmissions");

            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectiveMeasure",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrectiveMeasure",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
