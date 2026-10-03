using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39CanonicalSubmissionBaseState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseState",
                table: "OpmsSubmissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "IN_PROGRESS");

            migrationBuilder.AddColumn<string>(
                name: "BaseState",
                table: "IpmsSubmissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "IN_PROGRESS");

            migrationBuilder.Sql(
                """
                UPDATE [OpmsSubmissions]
                SET [BaseState] = CASE
                    WHEN UPPER(LTRIM(RTRIM(COALESCE([Status], '')))) IN ('', 'DRAFT', 'VERIFY_REJECTED', 'REJECTED') THEN 'IN_PROGRESS'
                    ELSE 'SUBMITTED'
                END;

                UPDATE [IpmsSubmissions]
                SET [BaseState] = CASE
                    WHEN UPPER(LTRIM(RTRIM(COALESCE([Status], '')))) IN ('', 'DRAFT', 'VERIFY_REJECTED', 'REJECTED') THEN 'IN_PROGRESS'
                    ELSE 'SUBMITTED'
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OpmsSubmissions_BaseState",
                table: "OpmsSubmissions",
                sql: "[BaseState] IN ('IN_PROGRESS','SUBMITTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IpmsSubmissions_BaseState",
                table: "IpmsSubmissions",
                sql: "[BaseState] IN ('IN_PROGRESS','SUBMITTED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OpmsSubmissions_BaseState",
                table: "OpmsSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IpmsSubmissions_BaseState",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "BaseState",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "BaseState",
                table: "IpmsSubmissions");
        }
    }
}
