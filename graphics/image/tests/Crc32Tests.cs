using System.Collections.Generic;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class Crc32Tests
{
    [Test]
    public void Compute_ReturnsStandardCrc32ForKnownInput()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("123456789");

        uint crc = Crc32.Compute(bytes);

        Assert.That(crc, Is.EqualTo(0xcbf43926));
    }

    [Test]
    public void IncrementalUpdate_MatchesSinglePassComputation()
    {
        byte[] left = Encoding.ASCII.GetBytes("1234");
        byte[] right = Encoding.ASCII.GetBytes("56789");
        byte[] all = Encoding.ASCII.GetBytes("123456789");

        uint crc = Crc32.Begin();
        crc = Crc32.Update(crc, left, 0, left.Length, null, "test");
        crc = Crc32.Update(crc, right, 0, right.Length, null, "test");
        crc = Crc32.Finish(crc);

        Assert.That(crc, Is.EqualTo(Crc32.Compute(all)));
    }

    [Test]
    public void ComputeRange_ComputesCrcForSelectedBytes()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = Encoding.ASCII.GetBytes("xx123456789yy");

        uint crc = Crc32.ComputeRange(bytes, 2, 9, diagnostics, "range");

        Assert.That(crc, Is.EqualTo(0xcbf43926));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ComputePngChunk_ReturnsKnownIhdrCrc()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] ihdr = new byte[]
        {
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 6, 0, 0, 0
        };

        uint crc = Crc32.ComputePngChunk("IHDR", ihdr, diagnostics);

        Assert.That(crc, Is.EqualTo(0x1f15c489));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ValidatePngChunkCrc_ReturnsTrueForMatchAndDiagnosticForMismatch()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] data = Encoding.ASCII.GetBytes("hello");
        uint expected = Crc32.ComputePngChunk("tEXt", data, diagnostics);

        bool valid = Crc32.ValidatePngChunkCrc("tEXt", data, expected, 12, diagnostics);
        bool invalid = Crc32.ValidatePngChunkCrc("tEXt", data, expected + 1, 12, diagnostics);

        Assert.That(valid, Is.True);
        Assert.That(invalid, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(diagnostics[0].Code, Is.EqualTo("png.crc.mismatch"));
        Assert.That(diagnostics[0].BlockType, Is.EqualTo("tEXt"));
        Assert.That(diagnostics[0].Offset, Is.EqualTo(12));
    }

    [Test]
    public void ValidateChunkType_RejectsNullWrongLengthAndNonLetters()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        bool nullType = Crc32.ValidateChunkType(null, diagnostics);
        bool shortType = Crc32.ValidateChunkType("ID", diagnostics);
        bool invalidType = Crc32.ValidateChunkType("ID1T", diagnostics);

        Assert.That(nullType, Is.False);
        Assert.That(shortType, Is.False);
        Assert.That(invalidType, Is.False);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(3));
        Assert.That(diagnostics[0].Code, Is.EqualTo("png.chunk.type.null"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("png.chunk.type.length"));
        Assert.That(diagnostics[2].Code, Is.EqualTo("png.chunk.type.invalid"));
    }

    [Test]
    public void ValidateRange_RejectsInvalidRanges()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = new byte[] { 1, 2, 3 };

        uint nullCrc = Crc32.ComputeRange(null, 0, 1, diagnostics, "crc");
        uint negativeCrc = Crc32.ComputeRange(bytes, -1, 1, diagnostics, "crc");
        uint outsideCrc = Crc32.ComputeRange(bytes, 8, 1, diagnostics, "crc");
        uint truncatedCrc = Crc32.ComputeRange(bytes, 2, 4, diagnostics, "crc");

        Assert.That(nullCrc, Is.EqualTo(0));
        Assert.That(negativeCrc, Is.EqualTo(0));
        Assert.That(outsideCrc, Is.EqualTo(0));
        Assert.That(truncatedCrc, Is.EqualTo(0));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(2));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(2));
    }
}
