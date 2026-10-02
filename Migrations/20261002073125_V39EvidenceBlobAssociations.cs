using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTCERP.Host.Migrations
{
    public partial class V39EvidenceBlobAssociations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_PoeFiles_MunicipalityId_Sha256", table: "PoeFiles");
            migrationBuilder.DropIndex(name: "IX_IdpDocuments_IdpPlanId_Sha256", table: "IdpDocuments");

            migrationBuilder.CreateTable(
                name: "EvidenceBlobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipalityId = table.Column<long>(type: "bigint", nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SignatureVerified = table.Column<bool>(type: "bit", nullable: false),
                    ScanStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsQuarantined = table.Column<bool>(type: "bit", nullable: false),
                    ScannerProvider = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ScannerReference = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    ScanDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsContentDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ContentDeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceBlobs", x => x.Id);
                    table.ForeignKey(name: "FK_EvidenceBlobs_Municipalities_MunicipalityId", column: x => x.MunicipalityId, principalTable: "Municipalities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<string>(name: "EvidenceBlobId", table: "PoeFiles", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EvidenceBlobId", table: "IdpDocuments", type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [PoeFiles] WHERE LEN([StoragePath]) > 1000)
                    THROW 51000, 'A POE storage path exceeds the EvidenceBlob storage-key limit; reconcile it before migration.', 1;
                IF EXISTS (SELECT 1 FROM [IdpDocuments] WHERE LEN([StoragePath]) > 1000)
                    THROW 51000, 'An IDP storage path exceeds the EvidenceBlob storage-key limit; reconcile it before migration.', 1;

                INSERT INTO [EvidenceBlobs] ([Id], [PublicId], [MunicipalityId], [StorageKey], [ContentType], [SizeInBytes], [Sha256], [SignatureVerified], [ScanStatus], [IsQuarantined], [ScannerProvider], [ScannerReference], [ScanDetail], [ScannedAt], [IsContentDeleted], [ContentDeletedAt], [CreatedAt])
                SELECT CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT('POE|', [Id])), 2), NEWID(), [MunicipalityId], [StoragePath], [ContentType], [SizeInBytes], [Sha256], [SignatureVerified], [ScanStatus], [IsQuarantined], [ScannerProvider], [ScannerReference], [ScanDetail], [ScannedAt], 0, NULL, [UploadedAt]
                FROM [PoeFiles];

                UPDATE [PoeFiles]
                SET [EvidenceBlobId] = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT('POE|', [Id])), 2);

                INSERT INTO [EvidenceBlobs] ([Id], [PublicId], [MunicipalityId], [StorageKey], [ContentType], [SizeInBytes], [Sha256], [SignatureVerified], [ScanStatus], [IsQuarantined], [ScannerProvider], [ScannerReference], [ScanDetail], [ScannedAt], [IsContentDeleted], [ContentDeletedAt], [CreatedAt])
                SELECT CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT('IDP|', d.[Id])), 2), NEWID(), p.[MunicipalityId], d.[StoragePath], d.[ContentType], d.[SizeInBytes], d.[Sha256], d.[SignatureVerified], d.[ScanStatus], d.[IsQuarantined], d.[ScannerProvider], d.[ScannerReference], d.[ScanDetail], d.[ScannedAt], 0, NULL, d.[UploadedAt]
                FROM [IdpDocuments] d
                INNER JOIN [IdpPlans] p ON p.[Id] = d.[IdpPlanId];

                UPDATE d
                SET [EvidenceBlobId] = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT('IDP|', d.[Id])), 2)
                FROM [IdpDocuments] d;
                """);

            migrationBuilder.AlterColumn<string>(name: "EvidenceBlobId", table: "PoeFiles", type: "nvarchar(64)", maxLength: 64, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(64)", oldMaxLength: 64, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "EvidenceBlobId", table: "IdpDocuments", type: "nvarchar(64)", maxLength: 64, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(64)", oldMaxLength: 64, oldNullable: true);

            foreach (var column in new[] { "ContentType", "IsQuarantined", "ScanDetail", "ScanStatus", "ScannedAt", "ScannerProvider", "ScannerReference", "Sha256", "SignatureVerified", "SizeInBytes", "StoragePath" })
                migrationBuilder.DropColumn(name: column, table: "PoeFiles");
            foreach (var column in new[] { "ContentType", "IsQuarantined", "ScanDetail", "ScanStatus", "ScannedAt", "ScannerProvider", "ScannerReference", "Sha256", "SignatureVerified", "SizeInBytes", "StoragePath" })
                migrationBuilder.DropColumn(name: column, table: "IdpDocuments");

            migrationBuilder.CreateIndex(name: "IX_PoeFiles_EvidenceBlobId", table: "PoeFiles", column: "EvidenceBlobId");
            migrationBuilder.CreateIndex(name: "IX_PoeFiles_MunicipalityId", table: "PoeFiles", column: "MunicipalityId");
            migrationBuilder.CreateIndex(name: "IX_IdpDocuments_EvidenceBlobId", table: "IdpDocuments", column: "EvidenceBlobId");
            migrationBuilder.CreateIndex(name: "IX_IdpDocuments_IdpPlanId_EvidenceBlobId", table: "IdpDocuments", columns: new[] { "IdpPlanId", "EvidenceBlobId" });
            migrationBuilder.CreateIndex(name: "IX_EvidenceBlobs_MunicipalityId_Sha256", table: "EvidenceBlobs", columns: new[] { "MunicipalityId", "Sha256" });
            migrationBuilder.CreateIndex(name: "IX_EvidenceBlobs_PublicId", table: "EvidenceBlobs", column: "PublicId", unique: true);
            migrationBuilder.AddForeignKey(name: "FK_IdpDocuments_EvidenceBlobs_EvidenceBlobId", table: "IdpDocuments", column: "EvidenceBlobId", principalTable: "EvidenceBlobs", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_PoeFiles_EvidenceBlobs_EvidenceBlobId", table: "PoeFiles", column: "EvidenceBlobId", principalTable: "EvidenceBlobs", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ContentType", table: "PoeFiles", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsQuarantined", table: "PoeFiles", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "ScanDetail", table: "PoeFiles", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScanStatus", table: "PoeFiles", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "ScannedAt", table: "PoeFiles", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScannerProvider", table: "PoeFiles", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScannerReference", table: "PoeFiles", type: "nvarchar(240)", maxLength: 240, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Sha256", table: "PoeFiles", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "SignatureVerified", table: "PoeFiles", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<long>(name: "SizeInBytes", table: "PoeFiles", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<string>(name: "StoragePath", table: "PoeFiles", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ContentType", table: "IdpDocuments", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsQuarantined", table: "IdpDocuments", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "ScanDetail", table: "IdpDocuments", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScanStatus", table: "IdpDocuments", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "ScannedAt", table: "IdpDocuments", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScannerProvider", table: "IdpDocuments", type: "nvarchar(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScannerReference", table: "IdpDocuments", type: "nvarchar(240)", maxLength: 240, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Sha256", table: "IdpDocuments", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "SignatureVerified", table: "IdpDocuments", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<long>(name: "SizeInBytes", table: "IdpDocuments", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<string>(name: "StoragePath", table: "IdpDocuments", type: "nvarchar(max)", nullable: false, defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE p SET [ContentType] = b.[ContentType], [IsQuarantined] = b.[IsQuarantined], [ScanDetail] = b.[ScanDetail], [ScanStatus] = b.[ScanStatus], [ScannedAt] = b.[ScannedAt], [ScannerProvider] = b.[ScannerProvider], [ScannerReference] = b.[ScannerReference], [Sha256] = b.[Sha256], [SignatureVerified] = b.[SignatureVerified], [SizeInBytes] = b.[SizeInBytes], [StoragePath] = b.[StorageKey]
                FROM [PoeFiles] p INNER JOIN [EvidenceBlobs] b ON b.[Id] = p.[EvidenceBlobId];
                UPDATE d SET [ContentType] = b.[ContentType], [IsQuarantined] = b.[IsQuarantined], [ScanDetail] = b.[ScanDetail], [ScanStatus] = b.[ScanStatus], [ScannedAt] = b.[ScannedAt], [ScannerProvider] = b.[ScannerProvider], [ScannerReference] = b.[ScannerReference], [Sha256] = b.[Sha256], [SignatureVerified] = b.[SignatureVerified], [SizeInBytes] = b.[SizeInBytes], [StoragePath] = b.[StorageKey]
                FROM [IdpDocuments] d INNER JOIN [EvidenceBlobs] b ON b.[Id] = d.[EvidenceBlobId];
                """);

            migrationBuilder.DropForeignKey(name: "FK_IdpDocuments_EvidenceBlobs_EvidenceBlobId", table: "IdpDocuments");
            migrationBuilder.DropForeignKey(name: "FK_PoeFiles_EvidenceBlobs_EvidenceBlobId", table: "PoeFiles");
            migrationBuilder.DropIndex(name: "IX_PoeFiles_EvidenceBlobId", table: "PoeFiles");
            migrationBuilder.DropIndex(name: "IX_PoeFiles_MunicipalityId", table: "PoeFiles");
            migrationBuilder.DropIndex(name: "IX_IdpDocuments_EvidenceBlobId", table: "IdpDocuments");
            migrationBuilder.DropIndex(name: "IX_IdpDocuments_IdpPlanId_EvidenceBlobId", table: "IdpDocuments");
            migrationBuilder.DropColumn(name: "EvidenceBlobId", table: "PoeFiles");
            migrationBuilder.DropColumn(name: "EvidenceBlobId", table: "IdpDocuments");
            migrationBuilder.DropTable(name: "EvidenceBlobs");
            migrationBuilder.CreateIndex(name: "IX_PoeFiles_MunicipalityId_Sha256", table: "PoeFiles", columns: new[] { "MunicipalityId", "Sha256" });
            migrationBuilder.CreateIndex(name: "IX_IdpDocuments_IdpPlanId_Sha256", table: "IdpDocuments", columns: new[] { "IdpPlanId", "Sha256" });
        }
    }
}
