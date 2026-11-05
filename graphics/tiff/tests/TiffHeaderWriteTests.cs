using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffHeaderWriteTests
{
    [Test]
    public void WriteClassic_WritesLittleEndianHeaderToSpan()
    {
        byte[] bytes = new byte[8];

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 }));
    }

    [Test]
    public void WriteClassic_WritesBigEndianHeaderToSpan()
    {
        byte[] bytes = new byte[8];

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.BigEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x10 }));
    }

    [Test]
    public void WriteClassic_HeaderOverloadValidatesClassicFileKind()
    {
        byte[] bytes = new byte[8];
        TiffHeader header = TiffHeader.Create(TiffByteOrder.LittleEndian, TiffFileKind.BigTiff, 16);

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, header);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[8]));
    }

    [Test]
    public void WriteClassic_HeaderOverloadWritesClassicHeader()
    {
        byte[] bytes = new byte[8];
        TiffHeader header = TiffHeader.Create(TiffByteOrder.BigEndian, TiffFileKind.Classic, 32);

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, header);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x20 }));
    }

    [Test]
    public void WriteClassic_AllowsZeroFirstIfdOffset()
    {
        byte[] bytes = new byte[8];

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00 }));
    }

    [Test]
    public void WriteClassic_ReturnsDiagnosticsForShortDestinationWithoutWriting()
    {
        byte[] bytes = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }));
    }

    [Test]
    public void WriteClassic_ReturnsDiagnosticsForInvalidByteOrderWithoutWriting()
    {
        byte[] bytes = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.Unknown, 8);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA }));
    }

    [Test]
    public void WriteClassic_ReturnsDiagnosticsForOffsetThatDoesNotFitClassicTiff()
    {
        byte[] bytes = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, (ulong)uint.MaxValue + 1UL);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA }));
    }

    [Test]
    public void WriteClassic_StrictModeReportsOffsetInsideHeaderAsError()
    {
        byte[] bytes = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, 4);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA }));
    }

    [Test]
    public void WriteClassic_PermissiveModeReportsOffsetInsideHeaderAsWarningAndWrites()
    {
        byte[] bytes = new byte[8];
        TiffWriteOptions options = new TiffWriteOptions();
        options.Strict = false;

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(bytes, TiffByteOrder.LittleEndian, 4, options);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x04, 0x00, 0x00, 0x00 }));
    }

    [Test]
    public void WriteClassic_WritesHeaderToStream()
    {
        MemoryStream stream = new MemoryStream();

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(stream, TiffByteOrder.BigEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(stream.Position, Is.EqualTo(8));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x10 }));
    }

    [Test]
    public void WriteClassic_StreamHeaderRoundTripsThroughReader()
    {
        MemoryStream stream = new MemoryStream();

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(stream, TiffByteOrder.LittleEndian, 16);
        stream.Position = 0;
        TiffHeaderReadResult result = TiffHeader.ReadClassic(stream);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
    }

    [Test]
    public void WriteClassic_ReturnsDiagnosticsForNullStream()
    {
        Stream stream = null!;

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(stream, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void WriteClassic_ReturnsDiagnosticsForReadOnlyStream()
    {
        MemoryStream stream = new MemoryStream(new byte[8], false);

        TiffDiagnostics diagnostics = TiffHeader.WriteClassic(stream, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void WriteBigTiff_WritesLittleEndianHeaderToSpan()
    {
        byte[] bytes = new byte[16];

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void WriteBigTiff_WritesBigEndianHeaderToSpan()
    {
        byte[] bytes = new byte[16];

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.BigEndian, 0x0000000123456789UL);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x23, 0x45, 0x67, 0x89
        }));
    }

    [Test]
    public void WriteBigTiff_HeaderOverloadValidatesBigTiffFileKind()
    {
        byte[] bytes = new byte[16];
        TiffHeader header = TiffHeader.Create(TiffByteOrder.LittleEndian, TiffFileKind.Classic, 16);

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, header);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[16]));
    }

    [Test]
    public void WriteBigTiff_HeaderOverloadWritesBigTiffHeader()
    {
        byte[] bytes = new byte[16];
        TiffHeader header = TiffHeader.Create(TiffByteOrder.BigEndian, TiffFileKind.BigTiff, 32);

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, header);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20
        }));
    }

    [Test]
    public void WriteBigTiff_AllowsZeroFirstIfdOffset()
    {
        byte[] bytes = new byte[16];

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.LittleEndian, 0);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void WriteBigTiff_ReturnsDiagnosticsForShortDestinationWithoutWriting()
    {
        byte[] bytes = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }));
    }

    [Test]
    public void WriteBigTiff_ReturnsDiagnosticsForInvalidByteOrderWithoutWriting()
    {
        byte[] bytes = new byte[]
        {
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA
        };

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.Unknown, 16);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA
        }));
    }

    [Test]
    public void WriteBigTiff_StrictModeReportsOffsetInsideHeaderAsError()
    {
        byte[] bytes = new byte[]
        {
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA
        };

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.LittleEndian, 8);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0xAA
        }));
    }

    [Test]
    public void WriteBigTiff_PermissiveModeReportsOffsetInsideHeaderAsWarningAndWrites()
    {
        byte[] bytes = new byte[16];
        TiffWriteOptions options = new TiffWriteOptions();
        options.Strict = false;

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(bytes, TiffByteOrder.LittleEndian, 8, options);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
        Assert.That(bytes, Is.EqualTo(new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void WriteBigTiff_WritesHeaderToStream()
    {
        MemoryStream stream = new MemoryStream();

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(stream, TiffByteOrder.BigEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(stream.Position, Is.EqualTo(16));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10
        }));
    }

    [Test]
    public void WriteBigTiff_StreamHeaderRoundTripsThroughReader()
    {
        MemoryStream stream = new MemoryStream();

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(stream, TiffByteOrder.LittleEndian, 16);
        stream.Position = 0;
        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(stream);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
    }

    [Test]
    public void WriteBigTiff_ReturnsDiagnosticsForNullStream()
    {
        Stream stream = null!;

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(stream, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void WriteBigTiff_ReturnsDiagnosticsForReadOnlyStream()
    {
        MemoryStream stream = new MemoryStream(new byte[16], false);

        TiffDiagnostics diagnostics = TiffHeader.WriteBigTiff(stream, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
        Assert.That(stream.Position, Is.EqualTo(0));
    }
}
