using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffScanlineReaderTests
{
    [Test]
    public void ReadScanline_ReadsContiguousRowFromDecodedStrip()
    {
        TiffImageInfo info = CreateInfo(3, 4, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20, 26 };
        ulong[] byteCounts = new ulong[] { 6, 6 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Row, Is.EqualTo(1));
        Assert.That(result.SampleIndex, Is.EqualTo(0));
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 23, 24, 25 }));
    }

    [Test]
    public void ReadScanline_FromDirectoryUsesDecodedStripDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(40);
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

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(directory, fileBytes, byteOrder, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StripIndex, Is.EqualTo(0));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 23, 24, 25 }));
    }

    [Test]
    public void ReadScanline_MapsSeparateSamplePlaneToStrip()
    {
        TiffImageInfo info = CreateInfo(2, 4, 2, new int[] { 8, 8 }, 2, TiffPlanarConfiguration.Separate, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(48);
        ulong[] offsets = new ulong[] { 20, 24, 28, 32 };
        ulong[] byteCounts = new ulong[] { 4, 4, 4, 4 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 3, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Row, Is.EqualTo(3));
        Assert.That(result.SampleIndex, Is.EqualTo(1));
        Assert.That(result.StripIndex, Is.EqualTo(3));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(2));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 34, 35 }));
    }

    [Test]
    public void ReadScanline_ReportsOutOfRangeRow()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20, 23 };
        ulong[] byteCounts = new ulong[] { 3, 3 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void ReadScanline_ReportsUnsupportedCompressionFromDecodedStrip()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.Jpeg);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 2 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void ReadScanline_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 0, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void ReadScanline_RejectsTiledImageInfo()
    {
        TiffImageInfo info = CreateInfo(3, 2, 1, new int[] { 8 }, 2, TiffPlanarConfiguration.Contiguous, TiffCompression.None);
        info.IsTiled = true;
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
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
