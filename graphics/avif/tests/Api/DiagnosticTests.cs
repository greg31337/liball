namespace avif.Tests;

public class DiagnosticTests
{
    [Test]
    public void BasicDiagnosticUsesUnknownContextSentinels()
    {
        AvifDiagnostic diagnostic = AvifDiagnostic.Create(
            AvifResultCode.InvalidContainer,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Container,
            "invalid box size");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(diagnostic.Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(diagnostic.Category, Is.EqualTo(AvifDiagnosticCategory.Container));
            Assert.That(diagnostic.Message, Is.EqualTo("invalid box size"));
            Assert.That(diagnostic.ByteOffset, Is.EqualTo(AvifDiagnostic.UnknownByteOffset));
            Assert.That(diagnostic.BitOffset, Is.EqualTo(AvifDiagnostic.UnknownBitOffset));
            Assert.That(diagnostic.BoxPath, Is.Empty);
            Assert.That(diagnostic.ItemId, Is.EqualTo(AvifDiagnostic.UnknownEntityId));
            Assert.That(diagnostic.TrackId, Is.EqualTo(AvifDiagnostic.UnknownEntityId));
            Assert.That(diagnostic.FrameIndex, Is.EqualTo(AvifDiagnostic.UnknownIndex));
            Assert.That(diagnostic.TileIndex, Is.EqualTo(AvifDiagnostic.UnknownIndex));
            Assert.That(diagnostic.Av1SyntaxPath, Is.Empty);
            Assert.That(diagnostic.StandardReference, Is.Empty);
        });
    }

    [Test]
    public void FullDiagnosticRetainsStructuredContext()
    {
        AvifDiagnostic diagnostic = AvifDiagnostic.Create(
            AvifResultCode.InvalidAv1Bitstream,
            AvifDiagnosticSeverity.Fatal,
            AvifDiagnosticCategory.Av1,
            "invalid tile size",
            4096,
            3,
            "/meta/iloc",
            17,
            4,
            8,
            2,
            "tile_group_obu/tile_size_minus_1",
            "AV1 6.10.1");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.ByteOffset, Is.EqualTo(4096));
            Assert.That(diagnostic.BitOffset, Is.EqualTo(3));
            Assert.That(diagnostic.BoxPath, Is.EqualTo("/meta/iloc"));
            Assert.That(diagnostic.ItemId, Is.EqualTo(17));
            Assert.That(diagnostic.TrackId, Is.EqualTo(4));
            Assert.That(diagnostic.FrameIndex, Is.EqualTo(8));
            Assert.That(diagnostic.TileIndex, Is.EqualTo(2));
            Assert.That(diagnostic.Av1SyntaxPath, Is.EqualTo("tile_group_obu/tile_size_minus_1"));
            Assert.That(diagnostic.StandardReference, Is.EqualTo("AV1 6.10.1"));
        });
    }

    [Test]
    public void ConstructorsNormalizeNullTextToEmptyStrings()
    {
        string? nullText = null;
        AvifDiagnostic diagnostic = AvifDiagnostic.Create(
            AvifResultCode.InvalidData,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.General,
            nullText,
            AvifDiagnostic.UnknownByteOffset,
            AvifDiagnostic.UnknownBitOffset,
            nullText,
            AvifDiagnostic.UnknownEntityId,
            AvifDiagnostic.UnknownEntityId,
            AvifDiagnostic.UnknownIndex,
            AvifDiagnostic.UnknownIndex,
            nullText,
            nullText);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Message, Is.Empty);
            Assert.That(diagnostic.BoxPath, Is.Empty);
            Assert.That(diagnostic.Av1SyntaxPath, Is.Empty);
            Assert.That(diagnostic.StandardReference, Is.Empty);
        });
    }
}
