using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffScanlineWriterTests
{
    [Test]
    public void WriteScanline_UpdatesContiguousRowAndPreservesOtherRows()
    {
        TiffImageInfo info = CreateInfo(3, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20, 26 };
        ulong[] byteCounts = new ulong[] { 6, 6 };

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 1, new byte[] { 9, 8, 7 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Row, Is.EqualTo(1));
        Assert.That(result.SampleIndex, Is.EqualTo(0));
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(destination[20], Is.EqualTo(20));
        Assert.That(destination[21], Is.EqualTo(21));
        Assert.That(destination[22], Is.EqualTo(22));
        Assert.That(destination[23], Is.EqualTo(9));
        Assert.That(destination[24], Is.EqualTo(8));
        Assert.That(destination[25], Is.EqualTo(7));
        Assert.That(destination[26], Is.EqualTo(26));
    }

    [Test]
    public void WriteScanline_FromDirectoryUsesDecodedStripDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(40);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 3, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 2, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.RowsPerStrip, 2, byteOrder),
            CreateShortEntry(TiffTagId.StripOffsets, 20, byteOrder),
            CreateShortEntry(TiffTagId.StripByteCounts, 6, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(directory, destination, byteOrder, 1, new byte[] { 88, 89, 90 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(destination[20], Is.EqualTo(20));
        Assert.That(destination[23], Is.EqualTo(88));
        Assert.That(destination[24], Is.EqualTo(89));
        Assert.That(destination[25], Is.EqualTo(90));
    }

    [Test]
    public void WriteScanline_MapsSeparateSamplePlaneToStrip()
    {
        TiffImageInfo info = CreateInfo(2, 4, 2, new int[] { 8, 8 }, 2, TiffPlanarConfiguration.Separate, TiffCompression.None);
        byte[] destination = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20, 24, 28, 32 };
        ulong[] byteCounts = new ulong[] { 4, 4, 4, 4 };

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 3, 1, new byte[] { 66, 67 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Row, Is.EqualTo(3));
        Assert.That(result.SampleIndex, Is.EqualTo(1));
        Assert.That(result.StripIndex, Is.EqualTo(3));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(4));
        Assert.That(destination[32], Is.EqualTo(32));
        Assert.That(destination[33], Is.EqualTo(33));
        Assert.That(destination[34], Is.EqualTo(66));
        Assert.That(destination[35], Is.EqualTo(67));
    }

    [Test]
    public void WriteScanline_RejectsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(20));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteScanline_ReportsUnsupportedCompressionFromDecodedStrip()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.Jpeg);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 2 };

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(20));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void WriteScanline_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 0, 0, new byte[] { 1, 2, 3 }, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void WriteScanline_RejectsTiledImageInfo()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        info.IsTiled = true;
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffScanlineWriteResult result = TiffScanlineWriter.WriteScanline(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, int samplesPerPixel, int[] bitsPerSample, ulong rowsPerStrip, TiffPlanarConfiguration planarConfiguration, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.RowsPerStrip = rowsPerStrip;
        info.PlanarConfiguration = planarConfiguration;
        info.Compression = compression;
        return info;
    }

    static byte[] CreateFileBytes(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        return bytes;
    }

    static TiffIfdEntry CreateShortEntry(int tagId, ushort value, TiffByteOrder byteOrder)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Short;
        entry.Count = 1;
        entry.RawCount = 1;
        entry.ValueByteCount = 2;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt16(entry.ValueOffsetBytes.AsSpan(0, 2), value, byteOrder);
        return entry;
    }
}
