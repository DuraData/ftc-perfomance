using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39SecurityTenantFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_DepartmentId_Code",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code",
                table: "Departments");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "UserScopes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "UserScopes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserScopes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "UserScopes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "Units",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "Units",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "Units",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Units",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Units",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "RolePermissions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "RolePermissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RolePermissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ScopeType",
                table: "RolePermissions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ActionCode",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Permissions",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "MemberCode",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NavigationCode",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Operation",
                table: "Permissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceCode",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "OpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "OpmsTargets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsTargets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "OpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "OpmsSubmissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OpmsSubmissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "IpmsTargets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IpmsTargets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IpmsTargets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "IpmsSubmissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "IpmsSubmissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IpmsSubmissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "Departments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "Departments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "Departments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Departments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Departments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AspNetUsers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipalityId",
                table: "AspNetRoles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RoleCode",
                table: "AspNetRoles",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AspNetRoles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "FinancialYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialYears", x => x.Id);
                    table.CheckConstraint("CK_FinancialYears_DateRange", "[EndDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityActionDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResourceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityActionDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityMemberDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MemberCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemManaged = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityMemberDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityNavigationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RequiredPermissionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityNavigationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityNavigationItems_SecurityNavigationItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SecurityNavigationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiResourceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportsCreate = table.Column<bool>(type: "bit", nullable: false),
                    SupportsRead = table.Column<bool>(type: "bit", nullable: false),
                    SupportsUpdate = table.Column<bool>(type: "bit", nullable: false),
                    SupportsDelete = table.Column<bool>(type: "bit", nullable: false),
                    SupportsExport = table.Column<bool>(type: "bit", nullable: false),
                    SupportsImport = table.Column<bool>(type: "bit", nullable: false),
                    SupportsFieldSecurity = table.Column<bool>(type: "bit", nullable: false),
                    SupportsRecordCriteria = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalEmployees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicipalEmployees_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalEmployees_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalityFinancialYears",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    FinancialYearId = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityFinancialYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicipalityFinancialYears_FinancialYears_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "FinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityFinancialYears_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityUserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityUserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityUserRoleAssignments_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityUserRoleAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityUserRoleAssignments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    MunicipalEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    PositionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_MunicipalEmployees_MunicipalEmployeeId",
                        column: x => x.MunicipalEmployeeId,
                        principalTable: "MunicipalEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportingPeriods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityFinancialYearId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingPeriods", x => x.Id);
                    table.CheckConstraint("CK_ReportingPeriods_DateRange", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_ReportingPeriods_MunicipalityFinancialYears_MunicipalityFinancialYearId",
                        column: x => x.MunicipalityFinancialYearId,
                        principalTable: "MunicipalityFinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE [Units] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [Departments] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [AspNetUsers] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [AspNetRoles] SET [PublicId] = NEWID(), [RoleCode] = COALESCE(NULLIF([NormalizedName], ''), [Id]) WHERE [PublicId] = '00000000-0000-0000-0000-000000000000' OR [RoleCode] = '';
                UPDATE [OpmsTargets] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [IpmsTargets] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [OpmsSubmissions] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                UPDATE [IpmsSubmissions] SET [PublicId] = NEWID() WHERE [PublicId] = '00000000-0000-0000-0000-000000000000';
                INSERT INTO [SecurityUserRoleAssignments] ([UserId], [RoleId], [MunicipalityId], [DepartmentId], [UnitId], [EffectiveFrom], [EffectiveTo], [IsActive], [AssignedBy], [AssignedAt], [RevokedBy], [RevokedAt])
                SELECT ur.[UserId], ur.[RoleId], r.[MunicipalityId], NULL, NULL, SYSUTCDATETIME(), NULL, 1, 'MIGRATION', SYSUTCDATETIME(), NULL, NULL
                FROM [AspNetUserRoles] ur
                INNER JOIN [AspNetRoles] r ON r.[Id] = ur.[RoleId]
                WHERE NOT EXISTS (SELECT 1 FROM [SecurityUserRoleAssignments] existing WHERE existing.[UserId] = ur.[UserId] AND existing.[RoleId] = ur.[RoleId] AND existing.[IsActive] = 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_MunicipalityId",
                table: "UserScopes",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_DepartmentId",
                table: "Units",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_MunicipalityId_DepartmentId_Code",
                table: "Units",
                columns: new[] { "MunicipalityId", "DepartmentId", "Code" },
                unique: true,
                filter: "[MunicipalityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Units_PublicId",
                table: "Units",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_MunicipalityId",
                table: "OpmsTargets",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsTargets_PublicId",
                table: "OpmsTargets",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_MunicipalityId",
                table: "OpmsSubmissions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpmsSubmissions_PublicId",
                table: "OpmsSubmissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_MunicipalityId",
                table: "IpmsTargets",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsTargets_PublicId",
                table: "IpmsTargets",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_MunicipalityId",
                table: "IpmsSubmissions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_IpmsSubmissions_PublicId",
                table: "IpmsSubmissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_MunicipalityId_Code",
                table: "Departments",
                columns: new[] { "MunicipalityId", "Code" },
                unique: true,
                filter: "[MunicipalityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_PublicId",
                table: "Departments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MunicipalityId",
                table: "AspNetUsers",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_MunicipalityId_RoleCode",
                table: "AspNetRoles",
                columns: new[] { "MunicipalityId", "RoleCode" },
                unique: true,
                filter: "[MunicipalityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_PublicId",
                table: "AspNetRoles",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_DepartmentId",
                table: "EmployeeAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_MunicipalEmployeeId",
                table: "EmployeeAssignments",
                column: "MunicipalEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_MunicipalityId_MunicipalEmployeeId_EffectiveFrom",
                table: "EmployeeAssignments",
                columns: new[] { "MunicipalityId", "MunicipalEmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_PublicId",
                table: "EmployeeAssignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_UnitId",
                table: "EmployeeAssignments",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialYears_Code",
                table: "FinancialYears",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialYears_PublicId",
                table: "FinancialYears",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalEmployees_IdentityUserId",
                table: "MunicipalEmployees",
                column: "IdentityUserId",
                unique: true,
                filter: "[IdentityUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalEmployees_MunicipalityId_EmployeeNumber",
                table: "MunicipalEmployees",
                columns: new[] { "MunicipalityId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalEmployees_PublicId",
                table: "MunicipalEmployees",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_Code",
                table: "Municipalities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_PublicId",
                table: "Municipalities",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityFinancialYears_FinancialYearId",
                table: "MunicipalityFinancialYears",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityFinancialYears_MunicipalityId_FinancialYearId",
                table: "MunicipalityFinancialYears",
                columns: new[] { "MunicipalityId", "FinancialYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityFinancialYears_PublicId",
                table: "MunicipalityFinancialYears",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingPeriods_MunicipalityFinancialYearId_Code",
                table: "ReportingPeriods",
                columns: new[] { "MunicipalityFinancialYearId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingPeriods_PublicId",
                table: "ReportingPeriods",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityActionDefinitions_Code",
                table: "SecurityActionDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityMemberDefinitions_ResourceCode_MemberCode",
                table: "SecurityMemberDefinitions",
                columns: new[] { "ResourceCode", "MemberCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNavigationItems_Code",
                table: "SecurityNavigationItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNavigationItems_ParentId",
                table: "SecurityNavigationItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityResources_Code",
                table: "SecurityResources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUserRoleAssignments_MunicipalityId",
                table: "SecurityUserRoleAssignments",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUserRoleAssignments_RoleId",
                table: "SecurityUserRoleAssignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUserRoleAssignments_UserId_RoleId_MunicipalityId_EffectiveFrom",
                table: "SecurityUserRoleAssignments",
                columns: new[] { "UserId", "RoleId", "MunicipalityId", "EffectiveFrom" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_Municipalities_MunicipalityId",
                table: "AspNetRoles",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Municipalities_MunicipalityId",
                table: "AspNetUsers",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Municipalities_MunicipalityId",
                table: "Departments",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsSubmissions_Municipalities_MunicipalityId",
                table: "IpmsSubmissions",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IpmsTargets_Municipalities_MunicipalityId",
                table: "IpmsTargets",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsSubmissions_Municipalities_MunicipalityId",
                table: "OpmsSubmissions",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OpmsTargets_Municipalities_MunicipalityId",
                table: "OpmsTargets",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Municipalities_MunicipalityId",
                table: "Units",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserScopes_Municipalities_MunicipalityId",
                table: "UserScopes",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Municipalities_MunicipalityId",
                table: "AspNetRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Municipalities_MunicipalityId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Municipalities_MunicipalityId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsSubmissions_Municipalities_MunicipalityId",
                table: "IpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_IpmsTargets_Municipalities_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsSubmissions_Municipalities_MunicipalityId",
                table: "OpmsSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_OpmsTargets_Municipalities_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Municipalities_MunicipalityId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_UserScopes_Municipalities_MunicipalityId",
                table: "UserScopes");

            migrationBuilder.DropTable(
                name: "EmployeeAssignments");

            migrationBuilder.DropTable(
                name: "ReportingPeriods");

            migrationBuilder.DropTable(
                name: "SecurityActionDefinitions");

            migrationBuilder.DropTable(
                name: "SecurityMemberDefinitions");

            migrationBuilder.DropTable(
                name: "SecurityNavigationItems");

            migrationBuilder.DropTable(
                name: "SecurityResources");

            migrationBuilder.DropTable(
                name: "SecurityUserRoleAssignments");

            migrationBuilder.DropTable(
                name: "MunicipalEmployees");

            migrationBuilder.DropTable(
                name: "MunicipalityFinancialYears");

            migrationBuilder.DropTable(
                name: "FinancialYears");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropIndex(
                name: "IX_UserScopes_MunicipalityId",
                table: "UserScopes");

            migrationBuilder.DropIndex(
                name: "IX_Units_DepartmentId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_MunicipalityId_DepartmentId_Code",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_PublicId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Code",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsTargets_PublicId",
                table: "OpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_MunicipalityId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_OpmsSubmissions_PublicId",
                table: "OpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsTargets_PublicId",
                table: "IpmsTargets");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_MunicipalityId",
                table: "IpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_IpmsSubmissions_PublicId",
                table: "IpmsSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Departments_MunicipalityId_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_PublicId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_MunicipalityId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_MunicipalityId_RoleCode",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_PublicId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "UserScopes");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "ActionCode",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "MemberCode",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "NavigationCode",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ResourceCode",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsTargets");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IpmsTargets");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IpmsSubmissions");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "RoleCode",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AspNetRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Units_DepartmentId_Code",
                table: "Units",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);
        }
    }
}
