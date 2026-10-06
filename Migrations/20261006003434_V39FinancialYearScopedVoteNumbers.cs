using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39FinancialYearScopedVoteNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_MunicipalityId_Code",
                table: "VoteNumbers");

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityFinancialYearId",
                table: "VoteNumbers",
                type: "bigint",
                nullable: true);

            if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE voteNumber
                    SET MunicipalityFinancialYearId = inferred.MunicipalityFinancialYearId
                    FROM VoteNumbers AS voteNumber
                    INNER JOIN (
                        SELECT targetVote.VoteNumberId, MIN(layer.MunicipalityFinancialYearId) AS MunicipalityFinancialYearId
                        FROM OpmsTargetVoteNumbers AS targetVote
                        INNER JOIN OpmsTargets AS target ON target.Id = targetVote.OpmsTargetId
                        INNER JOIN SdbipLayers AS layer ON layer.Id = target.SdbipLayerId
                        GROUP BY targetVote.VoteNumberId
                        HAVING COUNT(DISTINCT layer.MunicipalityFinancialYearId) = 1
                    ) AS inferred ON inferred.VoteNumberId = voteNumber.Id
                    WHERE voteNumber.MunicipalityFinancialYearId IS NULL;

                    UPDATE voteNumber
                    SET MunicipalityFinancialYearId = singleYear.MunicipalityFinancialYearId
                    FROM VoteNumbers AS voteNumber
                    INNER JOIN (
                        SELECT MunicipalityId, MIN(Id) AS MunicipalityFinancialYearId
                        FROM MunicipalityFinancialYears
                        GROUP BY MunicipalityId
                        HAVING COUNT(*) = 1
                    ) AS singleYear ON singleYear.MunicipalityId = voteNumber.MunicipalityId
                    WHERE voteNumber.MunicipalityFinancialYearId IS NULL;
                    """);
            }
            else if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE VoteNumbers
                    SET MunicipalityFinancialYearId = (
                        SELECT MIN(layer.MunicipalityFinancialYearId)
                        FROM OpmsTargetVoteNumbers AS targetVote
                        INNER JOIN OpmsTargets AS target ON target.Id = targetVote.OpmsTargetId
                        INNER JOIN SdbipLayers AS layer ON layer.Id = target.SdbipLayerId
                        WHERE targetVote.VoteNumberId = VoteNumbers.Id
                    )
                    WHERE MunicipalityFinancialYearId IS NULL
                      AND 1 = (
                        SELECT COUNT(DISTINCT layer.MunicipalityFinancialYearId)
                        FROM OpmsTargetVoteNumbers AS targetVote
                        INNER JOIN OpmsTargets AS target ON target.Id = targetVote.OpmsTargetId
                        INNER JOIN SdbipLayers AS layer ON layer.Id = target.SdbipLayerId
                        WHERE targetVote.VoteNumberId = VoteNumbers.Id
                      );

                    UPDATE VoteNumbers
                    SET MunicipalityFinancialYearId = (
                        SELECT MIN(year.Id)
                        FROM MunicipalityFinancialYears AS year
                        WHERE year.MunicipalityId = VoteNumbers.MunicipalityId
                    )
                    WHERE MunicipalityFinancialYearId IS NULL
                      AND 1 = (
                        SELECT COUNT(*)
                        FROM MunicipalityFinancialYears AS year
                        WHERE year.MunicipalityId = VoteNumbers.MunicipalityId
                      );
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_MunicipalityFinancialYearId_MunicipalityId",
                table: "VoteNumbers",
                columns: new[] { "MunicipalityFinancialYearId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_MunicipalityId_MunicipalityFinancialYearId_Code",
                table: "VoteNumbers",
                columns: new[] { "MunicipalityId", "MunicipalityFinancialYearId", "Code" },
                unique: true,
                filter: "[MunicipalityFinancialYearId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_VoteNumbers_MunicipalityFinancialYears_MunicipalityFinancialYearId_MunicipalityId",
                table: "VoteNumbers",
                columns: new[] { "MunicipalityFinancialYearId", "MunicipalityId" },
                principalTable: "MunicipalityFinancialYears",
                principalColumns: new[] { "Id", "MunicipalityId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoteNumbers_MunicipalityFinancialYears_MunicipalityFinancialYearId_MunicipalityId",
                table: "VoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_MunicipalityFinancialYearId_MunicipalityId",
                table: "VoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_MunicipalityId_MunicipalityFinancialYearId_Code",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "MunicipalityFinancialYearId",
                table: "VoteNumbers");

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_MunicipalityId_Code",
                table: "VoteNumbers",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);
        }
    }
}
