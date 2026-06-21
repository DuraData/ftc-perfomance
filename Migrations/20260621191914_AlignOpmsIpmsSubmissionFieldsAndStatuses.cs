using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class AlignOpmsIpmsSubmissionFieldsAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActualPerformanceDescription",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverComment",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApproverScore",
                table: "OpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverStatus",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuditorComment",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorRecommendation",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditorResponseDueDate",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AuditorScore",
                table: "OpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorStatus",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DueDateExtendedDays",
                table: "OpmsSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "OpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrganisationId",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsComment",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsRecommendation",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PmsResponseDueDate",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsRfiComment",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PmsScore",
                table: "OpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsStatus",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PoeType",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitterStatus",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "OpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifierComment",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifierScore",
                table: "OpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifierStatus",
                table: "OpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActualPerformanceDescription",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverComment",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApproverScore",
                table: "IpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverStatus",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuditorComment",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorRecommendation",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditorResponseDueDate",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AuditorScore",
                table: "IpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorStatus",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DueDateExtendedDays",
                table: "IpmsSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "IpmsSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrganisationId",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsComment",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsRecommendation",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PmsResponseDueDate",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsRfiComment",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PmsScore",
                table: "IpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmsStatus",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PoeType",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitterStatus",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "IpmsSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifierComment",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifierScore",
                table: "IpmsSubmissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifierStatus",
                table: "IpmsSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE OpmsSubmissions
SET
    SubmitterStatus = CASE
        WHEN LOWER(ISNULL([Status], 'draft')) = 'draft' THEN 'Draft'
        WHEN LOWER([Status]) = 'submitted' THEN 'Submitted'
        WHEN LOWER([Status]) = 'verify_rejected' THEN 'Needs Rework'
        WHEN LOWER([Status]) = 'rejected' THEN 'Needs Rework'
        WHEN LOWER([Status]) = 'verified' THEN 'Verified'
        WHEN LOWER([Status]) = 'approved' THEN 'Approved'
        WHEN LOWER([Status]) = 'reviewed' THEN 'Respond To PMS'
        WHEN LOWER([Status]) = 'audited' THEN 'Respond To Audit'
        ELSE 'Submitted'
    END,
    VerifierStatus = CASE
        WHEN LOWER([Status]) = 'verified' THEN 'Verified'
        WHEN LOWER([Status]) = 'verify_rejected' THEN 'Rejected'
        ELSE 'Pending'
    END,
    ApproverStatus = CASE
        WHEN LOWER([Status]) = 'approved' THEN 'Approved'
        WHEN LOWER([Status]) = 'rejected' THEN 'Rejected'
        ELSE 'Pending'
    END,
    PmsStatus = CASE
        WHEN LOWER([Status]) = 'reviewed' THEN 'Reviewed'
        ELSE 'Pending'
    END,
    AuditorStatus = CASE
        WHEN LOWER([Status]) = 'audited' THEN 'Audited'
        ELSE 'Pending'
    END,
    CreatedOn = COALESCE(CreatedAt, SYSUTCDATETIME()),
    UpdatedOn = COALESCE(UpdatedOn, CreatedAt)
WHERE SubmitterStatus = '' OR VerifierStatus = '' OR ApproverStatus = '' OR PmsStatus = '' OR AuditorStatus = '';

UPDATE IpmsSubmissions
SET
    SubmitterStatus = CASE
        WHEN LOWER(ISNULL([Status], 'draft')) = 'draft' THEN 'Draft'
        WHEN LOWER([Status]) = 'submitted' THEN 'Submitted'
        WHEN LOWER([Status]) = 'verify_rejected' THEN 'Needs Rework'
        WHEN LOWER([Status]) = 'rejected' THEN 'Needs Rework'
        WHEN LOWER([Status]) = 'verified' THEN 'Verified'
        WHEN LOWER([Status]) = 'approved' THEN 'Approved'
        WHEN LOWER([Status]) = 'reviewed' THEN 'Respond To PMS'
        WHEN LOWER([Status]) = 'audited' THEN 'Respond To Audit'
        ELSE 'Submitted'
    END,
    VerifierStatus = CASE
        WHEN LOWER([Status]) = 'verified' THEN 'Verified'
        WHEN LOWER([Status]) = 'verify_rejected' THEN 'Rejected'
        ELSE 'Pending'
    END,
    ApproverStatus = CASE
        WHEN LOWER([Status]) = 'approved' THEN 'Approved'
        WHEN LOWER([Status]) = 'rejected' THEN 'Rejected'
        ELSE 'Pending'
    END,
    PmsStatus = CASE
        WHEN LOWER([Status]) = 'reviewed' THEN 'Reviewed'
        ELSE 'Pending'
    END,
    AuditorStatus = CASE
        WHEN LOWER([Status]) = 'audited' THEN 'Audited'
        ELSE 'Pending'
    END,
    CreatedOn = COALESCE(CreatedAt, SYSUTCDATETIME()),
    UpdatedOn = COALESCE(UpdatedOn, CreatedAt)
WHERE SubmitterStatus = '' OR VerifierStatus = '' OR ApproverStatus = '' OR PmsStatus = '' OR AuditorStatus = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualPerformanceDescription",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverComment",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverScore",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverStatus",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorComment",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorRecommendation",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorResponseDueDate",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorScore",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorStatus",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "DueDateExtendedDays",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsComment",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsRecommendation",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsResponseDueDate",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsRfiComment",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsScore",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsStatus",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PoeType",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmitterStatus",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierComment",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierScore",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierStatus",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ActualPerformanceDescription",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverComment",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverScore",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "ApproverStatus",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorComment",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorRecommendation",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorResponseDueDate",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorScore",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "AuditorStatus",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "DueDateExtendedDays",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsComment",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsRecommendation",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsResponseDueDate",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsRfiComment",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsScore",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PmsStatus",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PoeType",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmitterStatus",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierComment",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierScore",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "VerifierStatus",
                table: "IpmsSubmissions");
        }
    }
}
