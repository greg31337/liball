using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDecodedTileReaderTests
{
    [Test]
    public void ReadTile_DecodesNoCompressionRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(2, 1, 1, 1, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20, 24 };
        ulong[] byteCounts = new ulong[] { 1, 1 };

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(1));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(1));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void ReadTile_FromDirectoryUsesCodecDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] fileBytes = CreateFileBytes(40);
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateShortEntry(TiffTagId.ImageWidth, 2, byteOrder),
            CreateShortEntry(TiffTagId.ImageLength, 1, byteOrder),
            CreateShortEntry(TiffTagId.BitsPerSample, 8, byteOrder),
            CreateShortEntry(TiffTagId.PhotometricInterpretation, (ushort)TiffPhotometric.BlackIsZero, byteOrder),
            CreateShortEntry(TiffTagId.TileWidth, 1, byteOrder),
            CreateShortEntry(TiffTagId.TileLength, 1, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileOffsets, new ushort[] { 20, 24 }, byteOrder),
            CreateShortArrayEntry(TiffTagId.TileByteCounts, new ushort[] { 1, 1 }, byteOrder)
        };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, entries);

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(directory, fileBytes, byteOrder, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(1));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 24 }));
    }

    [Test]
    public void ReadTile_ReportsUnsupportedCompressionAndPreservesRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(1, 1, 1, 1, TiffCompression.Jpeg);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 1 };

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(1));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Jpeg));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 20 }));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void ReadTile_DecodesAdobeDeflateCompressedBytes()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.AdobeDeflate);
        byte[] fileBytes = CreateFileBytes(60);
        byte[] compressedBytes = new byte[] { 99, 100, 98, 102, 97, 101, 3, 0 };
        CopyBytes(compressedBytes, fileBytes, 20);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 8 };

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.AdobeDeflate));
        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void ReadTile_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(40);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void ReadTile_PropagatesRawReadDiagnostics()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] fileBytes = CreateFileBytes(24);
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedTileReadResult result = TiffDecodedTileReader.ReadTile(info, offsets, byteCounts, fileBytes, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.EqualTo(new byte[0]));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
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

    static void CopyBytes(byte[] source, byte[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationIndex + index] = source[index];
            index = index + 1;
        }
    }

    static TiffIfdEntry CreateShortEntry(int tagId, ushort value, TiffByteOrder byteOrder)
    {
        return CreateShortArrayEntry(tagId, new ushort[] { value }, byteOrder);
    }

    static TiffIfdEntry CreateShortArrayEntry(int tagId, ushort[] values, TiffByteOrder byteOrder)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = TiffDataType.Short;
        entry.Count = values.Length;
        entry.RawCount = (ulong)values.Length;
        entry.ValueByteCount = values.Length * 2;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];

        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(entry.ValueOffsetBytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index = index + 1;
        }

        return entry;
    }
}
