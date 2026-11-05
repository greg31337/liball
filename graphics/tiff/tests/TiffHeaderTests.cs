using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffHeaderTests
{
    [Test]
    public void DetectByteOrder_ReturnsExpectedMarkers()
    {
        Assert.That(TiffHeader.DetectByteOrder(new byte[] { 0x49, 0x49 }), Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(TiffHeader.DetectByteOrder(new byte[] { 0x4D, 0x4D }), Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(TiffHeader.DetectByteOrder(new byte[] { 0x49 }), Is.EqualTo(TiffByteOrder.Unknown));
        Assert.That(TiffHeader.DetectByteOrder(new byte[] { 0x00, 0x00 }), Is.EqualTo(TiffByteOrder.Unknown));
    }

    [Test]
    public void IsClassicTiff_DetectsClassicHeaderWithoutReadingIfdOffset()
    {
        Assert.That(TiffHeader.IsClassicTiff(new byte[] { 0x49, 0x49, 0x2A, 0x00 }), Is.True);
        Assert.That(TiffHeader.IsClassicTiff(new byte[] { 0x4D, 0x4D, 0x00, 0x2A }), Is.True);
        Assert.That(TiffHeader.IsClassicTiff(new byte[] { 0x49, 0x49, 0x2B, 0x00 }), Is.False);
        Assert.That(TiffHeader.IsClassicTiff(new byte[] { 0x00, 0x00, 0x2A, 0x00 }), Is.False);
        Assert.That(TiffHeader.IsClassicTiff(new byte[] { 0x49, 0x49, 0x2A }), Is.False);
    }

    [Test]
    public void IsBigTiff_DetectsBigTiffHeaderWithoutReadingIfdOffset()
    {
        Assert.That(TiffHeader.IsBigTiff(new byte[] { 0x49, 0x49, 0x2B, 0x00 }), Is.True);
        Assert.That(TiffHeader.IsBigTiff(new byte[] { 0x4D, 0x4D, 0x00, 0x2B }), Is.True);
        Assert.That(TiffHeader.IsBigTiff(new byte[] { 0x49, 0x49, 0x2A, 0x00 }), Is.False);
        Assert.That(TiffHeader.IsBigTiff(new byte[] { 0x00, 0x00, 0x2B, 0x00 }), Is.False);
        Assert.That(TiffHeader.IsBigTiff(new byte[] { 0x49, 0x49, 0x2B }), Is.False);
    }

    [Test]
    public void ReadClassic_ReadsLittleEndianHeader()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(8));
        Assert.That(result.Header.IsClassic(), Is.True);
        Assert.That(result.Header.IsBigTiff(), Is.False);
    }

    [Test]
    public void ReadClassic_ReadsBigEndianHeader()
    {
        byte[] bytes = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x10 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
    }

    [Test]
    public void ReadClassic_AllowsZeroFirstIfdOffsetForHeaderOnlyInspection()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(0));
    }

    [Test]
    public void ReadClassic_ReportsInvalidByteOrder()
    {
        byte[] bytes = new byte[] { 0x00, 0x00, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.HasErrors(), Is.True);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.Unknown));
    }

    [Test]
    public void ReadClassic_ReportsInvalidVersion()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2B, 0x00, 0x08, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadClassic_ReportsTruncatedHeader()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadClassic_StrictModeReportsOffsetInsideHeaderAsError()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x04, 0x00, 0x00, 0x00 };

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadClassic_PermissiveModeReportsOffsetInsideHeaderAsWarning()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x04, 0x00, 0x00, 0x00 };
        TiffReadOptions options = new TiffReadOptions();
        options.Strict = false;

        TiffHeaderReadResult result = TiffHeader.ReadClassic(bytes, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadClassic_StreamReadsEightBytesAndAdvancesPosition()
    {
        byte[] bytes = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x10, 0xFF };
        MemoryStream stream = new MemoryStream(bytes);

        TiffHeaderReadResult result = TiffHeader.ReadClassic(stream);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
        Assert.That(stream.Position, Is.EqualTo(8));
    }

    [Test]
    public void ReadClassic_StreamReportsTruncationWithoutThrowing()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x49, 0x49, 0x2A });

        TiffHeaderReadResult result = TiffHeader.ReadClassic(stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadClassic_NullStreamReportsUnsupportedStream()
    {
        Stream stream = null!;

        TiffHeaderReadResult result = TiffHeader.ReadClassic(stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReadsLittleEndianHeader()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
        Assert.That(result.Header.IsClassic(), Is.False);
        Assert.That(result.Header.IsBigTiff(), Is.True);
    }

    [Test]
    public void ReadBigTiff_ReadsBigEndianHeader()
    {
        byte[] bytes = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B,
            0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x23, 0x45, 0x67, 0x89
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(0x0000000123456789UL));
    }

    [Test]
    public void ReadBigTiff_AllowsZeroFirstIfdOffsetForHeaderOnlyInspection()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(0));
    }

    [Test]
    public void ReadBigTiff_ReportsInvalidByteOrder()
    {
        byte[] bytes = new byte[]
        {
            0x00, 0x00, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.Unknown));
    }

    [Test]
    public void ReadBigTiff_ReportsInvalidVersion()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2A, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTiffVersion), Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadBigTiff_ReportsInvalidOffsetByteSize()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidBigTiffHeader), Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadBigTiff_ReportsInvalidReservedField()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x01, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidBigTiffHeader), Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadBigTiff_ReportsTruncatedHeader()
    {
        byte[] bytes = new byte[] { 0x49, 0x49, 0x2B, 0x00, 0x08 };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
        Assert.That(result.Header.FileKind, Is.EqualTo(TiffFileKind.Unknown));
    }

    [Test]
    public void ReadBigTiff_StrictModeReportsOffsetInsideHeaderAsError()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadBigTiff_PermissiveModeReportsOffsetInsideHeaderAsWarning()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        TiffReadOptions options = new TiffReadOptions();
        options.Strict = false;

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(bytes, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadBigTiff_StreamReadsSixteenBytesAndAdvancesPosition()
    {
        byte[] bytes = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF
        };
        MemoryStream stream = new MemoryStream(bytes);

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(stream);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Header.ByteOrder, Is.EqualTo(TiffByteOrder.LittleEndian));
        Assert.That(result.Header.FirstIfdOffset, Is.EqualTo(16));
        Assert.That(stream.Position, Is.EqualTo(16));
    }

    [Test]
    public void ReadBigTiff_StreamReportsTruncationWithoutThrowing()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x49, 0x49, 0x2B });

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.True);
    }

    [Test]
    public void ReadBigTiff_NullStreamReportsUnsupportedStream()
    {
        Stream stream = null!;

        TiffHeaderReadResult result = TiffHeader.ReadBigTiff(stream);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }
}
