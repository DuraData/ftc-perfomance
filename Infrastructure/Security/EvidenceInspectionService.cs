using System.Security.Cryptography;

namespace FTCERP.Host.Infrastructure.Security;

public sealed record EvidenceInspectionResult(bool SignatureValid, string Sha256, string ScanStatus, string? Error);

public interface IEvidenceInspectionService
{
    EvidenceInspectionResult Inspect(byte[] content, string extension);
}

public sealed class EvidenceInspectionService : IEvidenceInspectionService
{
    public EvidenceInspectionResult Inspect(byte[] content, string extension)
    {
        if (content.Length == 0) return new(false, string.Empty, "Rejected", "File is empty.");
        var normalized = extension.ToLowerInvariant();
        var valid = normalized switch
        {
            ".pdf" => StartsWith(content, "%PDF-"u8),
            ".png" => content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".jpg" or ".jpeg" => content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff,
            ".docx" or ".xlsx" => content.Length >= 4 && content[0] == 0x50 && content[1] == 0x4b && content[2] == 0x03 && content[3] == 0x04,
            _ => false
        };
        var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        if (!valid) return new(false, sha, "Rejected", "File signature does not match its extension.");
        if (content.Length >= 2 && content[0] == 0x4d && content[1] == 0x5a) return new(false, sha, "Rejected", "Executable content is not allowed.");
        return new(true, sha, "SignatureValidated", null);
    }

    private static bool StartsWith(byte[] content, ReadOnlySpan<byte> prefix) => content.Length >= prefix.Length && content.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
