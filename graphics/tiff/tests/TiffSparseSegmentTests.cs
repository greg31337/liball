using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSparseSegmentTests
{
    [Test]
    public void DecodedStripReader_ReturnsZeroFilledBytesForSparseStrip()
    {
        TiffImageInfo info = CreateStripInfo(3, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffDecodedStripReadResult result = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void DecodedTileReader_ReturnsZeroFilledBytesForSparseTile()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void ScanlineReader_ReadsZeroBytesFromSparseStrip()
    {
        TiffImageInfo info = CreateStripInfo(3, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffScanlineReadResult result = TiffScanlineReader.ReadScanline(info, offsets, byteCounts, fileBytes, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ScanlineByteCount, Is.EqualTo(3));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0 }));
    }

    [Test]
    public void StripRegionReader_ReadsZeroBytesFromSparseStrip()
    {
        TiffImageInfo info = CreateStripInfo(5, 2, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffRegionReadResult result = TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 0, 3, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RowByteCount, Is.EqualTo(3));
        Assert.That(result.TotalByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void TileRegionReader_ReadsZeroBytesFromSparseTile()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffRegionReadResult result = TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 1, 0, 2, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RowByteCount, Is.EqualTo(2));
        Assert.That(result.TotalByteCount, Is.EqualTo(4));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void DecodedStripWriter_KeepsSparseStripForZeroBytes()
    {
        TiffImageInfo info = CreateStripInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 0, 0, 0, 0, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(20));
    }

    [Test]
    public void DecodedStripWriter_RejectsNonZeroSparseStrip()
    {
        TiffImageInfo info = CreateStripInfo(3, 2, 2, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, 0, new byte[] { 0, 0, 1, 0, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(20));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void DecodedTileWriter_KeepsSparseTileForZeroBytes()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] destination = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 0 };
        ulong[] byteCounts = new ulong[] { 0 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 0, 0, 0, 0, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(20));
    }

    static TiffImageInfo CreateStripInfo(ulong width, ulong length, ulong rowsPerStrip, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
        info.Compression = compression;
        return info;
    }

    static TiffImageInfo CreateTileInfo(ulong width, ulong length, ulong tileWidth, ulong tileLength, TiffCompression compression)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.IsTiled = true;
        info.TileWidth = tileWidth;
        info.TileLength = tileLength;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { 8 };
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
}
