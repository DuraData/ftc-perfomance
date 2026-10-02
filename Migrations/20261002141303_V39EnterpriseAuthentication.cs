using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class V39EnterpriseAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthenticationConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    ProviderRegistrationCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationConfigurations", x => x.Id);
                    table.CheckConstraint("CK_AuthenticationConfigurations_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_AuthenticationConfigurations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticationConfigurations_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticationConfigurations_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProviderCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticationEvents_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthenticators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderRegistrationCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExpectedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExternalIdentityHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAuthenticatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DisabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthenticators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_AspNetUsers_DisabledByUserId",
                        column: x => x.DisabledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_AspNetUsers_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: false),
                    AuthenticationConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    MinimumPasswordLength = table.Column<int>(type: "int", nullable: false),
                    MaximumFailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutMinutes = table.Column<int>(type: "int", nullable: false),
                    RequireMfaForPrivilegedLocalUsers = table.Column<bool>(type: "bit", nullable: false),
                    RequireMfaForAllLocalUsers = table.Column<bool>(type: "bit", nullable: false),
                    RequireFirstLoginPasswordChange = table.Column<bool>(type: "bit", nullable: false),
                    SessionIdleTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionAbsoluteTimeoutHours = table.Column<int>(type: "int", nullable: false),
                    MaximumConcurrentSessions = table.Column<int>(type: "int", nullable: false),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationPolicies", x => x.Id);
                    table.CheckConstraint("CK_AuthenticationPolicies_Bounds", "[MinimumPasswordLength] BETWEEN 12 AND 128 AND [MaximumFailedAttempts] BETWEEN 1 AND 20 AND [LockoutMinutes] BETWEEN 1 AND 1440 AND [SessionIdleTimeoutMinutes] BETWEEN 5 AND 1440 AND [SessionAbsoluteTimeoutHours] BETWEEN 1 AND 720 AND [MaximumConcurrentSessions] BETWEEN 1 AND 50");
                    table.ForeignKey(
                        name: "FK_AuthenticationPolicies_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticationPolicies_AuthenticationConfigurations_AuthenticationConfigurationId",
                        column: x => x.AuthenticationConfigurationId,
                        principalTable: "AuthenticationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthenticationPolicies_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_CreatedByUserId",
                table: "AuthenticationConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_ModifiedByUserId",
                table: "AuthenticationConfigurations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_MunicipalityId",
                table: "AuthenticationConfigurations",
                column: "MunicipalityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationConfigurations_PublicId",
                table: "AuthenticationConfigurations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_MunicipalityId_OccurredAt",
                table: "AuthenticationEvents",
                columns: new[] { "MunicipalityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_PublicId",
                table: "AuthenticationEvents",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_UserId",
                table: "AuthenticationEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_AuthenticationConfigurationId",
                table: "AuthenticationPolicies",
                column: "AuthenticationConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_ModifiedByUserId",
                table: "AuthenticationPolicies",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_MunicipalityId",
                table: "AuthenticationPolicies",
                column: "MunicipalityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationPolicies_PublicId",
                table: "AuthenticationPolicies",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_CreatedByUserId",
                table: "UserAuthenticators",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_DisabledByUserId",
                table: "UserAuthenticators",
                column: "DisabledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_ExternalIdentityHash",
                table: "UserAuthenticators",
                column: "ExternalIdentityHash",
                unique: true,
                filter: "[ExternalIdentityHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_LinkedByUserId",
                table: "UserAuthenticators",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_MunicipalityId_ProviderRegistrationCode_ExpectedEmail",
                table: "UserAuthenticators",
                columns: new[] { "MunicipalityId", "ProviderRegistrationCode", "ExpectedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_MunicipalityId_UserId_ProviderRegistrationCode",
                table: "UserAuthenticators",
                columns: new[] { "MunicipalityId", "UserId", "ProviderRegistrationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_PublicId",
                table: "UserAuthenticators",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_UserId",
                table: "UserAuthenticators",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationEvents");

            migrationBuilder.DropTable(
                name: "AuthenticationPolicies");

            migrationBuilder.DropTable(
                name: "UserAuthenticators");

            migrationBuilder.DropTable(
                name: "AuthenticationConfigurations");
        }
    }
}
