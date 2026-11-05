using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDecodedTileWriterTests
{
    [Test]
    public void WriteTile_EncodesNoCompressionAndWritesRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(2, 1, 1, 1, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20, 24 };
        ulong[] byteCounts = new ulong[] { 1, 1 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 1, new byte[] { 9 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(1));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(1));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.BytesWritten, Is.EqualTo(1));
        Assert.That(destination[23], Is.EqualTo(0));
        Assert.That(destination[24], Is.EqualTo(9));
        Assert.That(destination[25], Is.EqualTo(0));
    }

    [Test]
    public void WriteTile_FromDirectoryUsesCodecDispatcher()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        byte[] destination = CreateFileBytes(40);
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

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(directory, destination, byteOrder, 1, new byte[] { 88 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(1));
        Assert.That(result.BytesWritten, Is.EqualTo(1));
        Assert.That(destination[24], Is.EqualTo(88));
        Assert.That(destination[25], Is.EqualTo(25));
    }

    [Test]
    public void WriteTile_ReportsUnsupportedCompressionFromDispatcher()
    {
        TiffImageInfo info = CreateTileInfo(1, 1, 1, 1, TiffCompression.Jpeg);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 1 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 1 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedCompression), Is.True);
    }

    [Test]
    public void WriteTile_EncodesPackBitsAndWritesRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(6, 1, 6, 1, TiffCompression.PackBits);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 7 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 10, 11, 7, 7, 7, 12 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.TileIndex, Is.EqualTo(0));
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.PackBits));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
        Assert.That(destination[20], Is.EqualTo(1));
        Assert.That(destination[21], Is.EqualTo(10));
        Assert.That(destination[22], Is.EqualTo(11));
        Assert.That(destination[23], Is.EqualTo(254));
        Assert.That(destination[24], Is.EqualTo(7));
        Assert.That(destination[25], Is.EqualTo(0));
        Assert.That(destination[26], Is.EqualTo(12));
        Assert.That(destination[27], Is.EqualTo(0));
    }

    [Test]
    public void WriteTile_EncodesAdobeDeflateAndWritesRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(6, 1, 6, 1, TiffCompression.AdobeDeflate);
        byte[] decodedBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
        TiffCodecContext context = TiffCodecContext.Create(info, true, 0, 6, new TiffLimits());
        TiffCodecResult expectedEncoded = TiffDeflateCodec.Encode(context, decodedBytes);
        byte[] destination = new byte[100];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { (ulong)expectedEncoded.Bytes.Length };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, decodedBytes);
        byte[] writtenBytes = ExtractBytes(destination, 20, expectedEncoded.Bytes.Length);
        TiffCodecResult decodedResult = TiffDeflateCodec.Decode(context, writtenBytes);

        Assert.That(expectedEncoded.Success, Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(6));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.AdobeDeflate));
        Assert.That(result.BytesWritten, Is.EqualTo((ulong)expectedEncoded.Bytes.Length));
        Assert.That(writtenBytes, Is.EqualTo(expectedEncoded.Bytes));
        Assert.That(decodedResult.Success, Is.True);
        Assert.That(decodedResult.Bytes, Is.EqualTo(decodedBytes));
    }

    [Test]
    public void WriteTile_EncodesLzwAndWritesRawBytes()
    {
        TiffImageInfo info = CreateTileInfo(3, 1, 3, 1, TiffCompression.Lzw);
        byte[] decodedBytes = new byte[] { 65, 66, 67 };
        byte[] expectedEncodedBytes = new byte[] { 128, 16, 72, 68, 56, 8 };
        byte[] destination = new byte[60];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffCodecContext context = TiffCodecContext.Create(info, true, 0, 3, new TiffLimits());

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, decodedBytes);
        byte[] writtenBytes = ExtractBytes(destination, 20, expectedEncodedBytes.Length);
        TiffCodecResult decodedResult = TiffLzwCodec.Decode(context, writtenBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExpectedDecodedByteCount, Is.EqualTo(3));
        Assert.That(result.Compression, Is.EqualTo(TiffCompression.Lzw));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(writtenBytes, Is.EqualTo(expectedEncodedBytes));
        Assert.That(decodedResult.Success, Is.True);
        Assert.That(decodedResult.Bytes, Is.EqualTo(decodedBytes));
    }

    [Test]
    public void WriteTile_RejectsDecodedByteCountMismatch()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(destination[20], Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteTile_EnforcesDecodedExpansionLimit()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 6 };
        TiffLimits limits = new TiffLimits();
        limits.MaximumDecodedBytes = 5;

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 }, limits);

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.CodecExpansionLimitExceeded), Is.True);
    }

    [Test]
    public void WriteTile_PropagatesRawWriteDiagnostics()
    {
        TiffImageInfo info = CreateTileInfo(3, 2, 3, 2, TiffCompression.None);
        byte[] destination = new byte[40];
        ulong[] offsets = new ulong[] { 20 };
        ulong[] byteCounts = new ulong[] { 5 };

        TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, 0, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
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

    static byte[] ExtractBytes(byte[] source, int sourceIndex, int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < length)
        {
            bytes[index] = source[sourceIndex + index];
            index = index + 1;
        }

        return bytes;
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
