using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39GovernedWardAndVoteMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wards_Code",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_Code",
                table: "VoteNumbers");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "Wards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "Wards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "Wards",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                migrationBuilder.AddColumn<Guid>(name: "PublicId", table: "Wards", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            else
                migrationBuilder.AddColumn<Guid>(name: "PublicId", table: "Wards", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Wards",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "VoteNumbers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "VoteNumbers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "VoteNumbers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                migrationBuilder.AddColumn<Guid>(name: "PublicId", table: "VoteNumbers", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
            else
                migrationBuilder.AddColumn<Guid>(name: "PublicId", table: "VoteNumbers", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VoteNumbers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE w
                    SET w.[PublicId] = NEWID(),
                        w.[EffectiveFrom] = CASE WHEN w.[CreatedAt] < '1900-01-01' THEN SYSUTCDATETIME() ELSE w.[CreatedAt] END,
                        w.[MunicipalityId] = resolved.[Id]
                    FROM [Wards] w
                    CROSS APPLY (
                        SELECT MIN(m.[Id]) AS [Id], COUNT_BIG(*) AS [Matches]
                        FROM [Municipalities] m
                        WHERE UPPER(LTRIM(RTRIM(w.[Municipality]))) IN (UPPER(m.[Code]), UPPER(m.[Name]))
                    ) resolved
                    WHERE resolved.[Matches] = 1;

                    UPDATE v
                    SET v.[PublicId] = NEWID(),
                        v.[EffectiveFrom] = CASE WHEN v.[CreatedAt] < '1900-01-01' THEN SYSUTCDATETIME() ELSE v.[CreatedAt] END,
                        v.[MunicipalityId] = d.[MunicipalityId]
                    FROM [VoteNumbers] v
                    INNER JOIN [Departments] d ON d.[Id] = v.[DepartmentId]
                    WHERE d.[MunicipalityId] IS NOT NULL;

                    IF EXISTS (SELECT 1 FROM [Wards] WHERE [MunicipalityId] = 0)
                        THROW 51000, 'Ward tenant reconciliation is required before this migration can continue.', 1;
                    IF EXISTS (SELECT 1 FROM [VoteNumbers] WHERE [MunicipalityId] = 0 OR [DepartmentId] IS NULL)
                        THROW 51000, 'Vote-number department and tenant reconciliation is required before this migration can continue.', 1;
                    """);
            }
            else
            {
                migrationBuilder.Sql(
                    """
                    UPDATE Wards
                    SET PublicId = lower(hex(randomblob(16))),
                        EffectiveFrom = CreatedAt,
                        MunicipalityId = COALESCE((SELECT Id FROM Municipalities WHERE upper(trim(Code)) = upper(trim(Wards.Municipality)) OR upper(trim(Name)) = upper(trim(Wards.Municipality)) LIMIT 1), 0);
                    UPDATE VoteNumbers
                    SET PublicId = lower(hex(randomblob(16))),
                        EffectiveFrom = CreatedAt,
                        MunicipalityId = COALESCE((SELECT MunicipalityId FROM Departments WHERE Id = VoteNumbers.DepartmentId LIMIT 1), 0);
                    """);
            }

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "VoteNumbers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_MunicipalityId_Code",
                table: "Wards",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_PublicId",
                table: "Wards",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_MunicipalityId_Code",
                table: "VoteNumbers",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_PublicId",
                table: "VoteNumbers",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VoteNumbers_Municipalities_MunicipalityId",
                table: "VoteNumbers",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_Municipalities_MunicipalityId",
                table: "Wards",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoteNumbers_Municipalities_MunicipalityId",
                table: "VoteNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_Municipalities_MunicipalityId",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_Wards_MunicipalityId_Code",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_Wards_PublicId",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_MunicipalityId_Code",
                table: "VoteNumbers");

            migrationBuilder.DropIndex(
                name: "IX_VoteNumbers_PublicId",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "VoteNumbers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VoteNumbers");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "VoteNumbers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Code",
                table: "Wards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteNumbers_Code",
                table: "VoteNumbers",
                column: "Code",
                unique: true);
        }
    }
}
