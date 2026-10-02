namespace FTCERP.Tests;

public sealed class EvidenceInspectionTests
{
    private readonly EvidenceInspectionService _service = new();

    [Fact]
    public void Inspect_AcceptsMatchingPdfSignature_AndCalculatesStableHash()
    {
        var content = "%PDF-1.7\nsynthetic test evidence"u8.ToArray();
        var first = _service.Inspect(content, ".pdf");
        var second = _service.Inspect(content, ".pdf");

        first.SignatureValid.Should().BeTrue();
        first.Sha256.Should().HaveLength(64).And.Be(second.Sha256);
        first.ScanStatus.Should().Be("SignatureValidated");
    }

    [Fact]
    public void Inspect_RejectsExtensionSpoofing()
    {
        var result = _service.Inspect("not a real pdf"u8.ToArray(), ".pdf");

        result.SignatureValid.Should().BeFalse();
        result.ScanStatus.Should().Be("Rejected");
    }
}
