using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageTests
{
    [Test]
    public void PageHandle_ReturnsInfoTagsDefaultedValuesAndRawStrip()
    {
        byte[] fileBytes = CreateClassicStripFile();
        TiffFile file = TiffFile.OpenRead(fileBytes);

        TiffImage image = file.GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffTagDefaultResult compression = image.GetTagDefaulted(TiffTagId.Compression);
        TiffRawStripReadResult strip = image.ReadRawStrip(0);
        byte[] destination = new byte[6];
        TiffDiagnostics copyDiagnostics = image.ReadEncodedStripToBuffer(0, destination);

        Assert.That(image.Success, Is.True);
        Assert.That(image.GetIndex(), Is.EqualTo(0));
        Assert.That(info.Width, Is.EqualTo(3));
        Assert.That(info.Length, Is.EqualTo(2));
        Assert.That(info.Photometric, Is.EqualTo(TiffPhotometric.BlackIsZero));
        Assert.That(image.GetTags().Length, Is.EqualTo(7));
        Assert.That(image.GetTag(TiffTagId.ImageWidth)!.ValueOffset, Is.EqualTo(3));
        Assert.That(compression.Found, Is.True);
        Assert.That(compression.UsedDefault, Is.True);
        Assert.That(compression.Value.UnsignedValues[0], Is.EqualTo((ulong)TiffCompression.None));
        Assert.That(image.GetStripCount(), Is.EqualTo(1));
        Assert.That(image.GetTileCount(), Is.EqualTo(0));
        Assert.That(image.GetScanlineSize(), Is.EqualTo(3));
        Assert.That(image.GetStripSize(0), Is.EqualTo(6));
        Assert.That(strip.Success, Is.True);
        Assert.That(strip.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(copyDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void PageHandle_ReturnsTileCountAndEdgeTileSize()
    {
        byte[] fileBytes = CreateClassicTileFile();
        TiffFile file = TiffFile.OpenRead(fileBytes);

        TiffImage image = file.GetImage(0);
        TiffRawTileReadResult tile = image.ReadEncodedTile(3);

        Assert.That(image.Success, Is.True);
        Assert.That(image.GetStripCount(), Is.EqualTo(0));
        Assert.That(image.GetTileCount(), Is.EqualTo(4));
        Assert.That(image.GetScanlineSize(), Is.EqualTo(5));
        Assert.That(image.GetTileSize(0), Is.EqualTo(6));
        Assert.That(image.GetTileSize(3), Is.EqualTo(4));
        Assert.That(tile.Success, Is.True);
        Assert.That(tile.Bytes, Is.EqualTo(new byte[] { 19, 20, 21, 22, 23, 24 }));
    }

    [Test]
    public void PageHandle_ReturnsSubImages()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 10),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 10),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero)
        };
        TiffIfdEntry[][][] subIfdChains = new TiffIfdEntry[][][]
        {
            new TiffIfdEntry[][]
            {
                new TiffIfdEntry[]
                {
                    CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 5),
                    CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 5),
                    CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero)
                }
            }
        };
        byte[] fileBytes = new byte[8 + TiffSubIfdWriter.CalculateClassicByteCount(parentEntries, subIfdChains)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffSubIfdWriter.WriteClassic(fileBytes, parentEntries, subIfdChains, TiffByteOrder.LittleEndian, 8, 0);
        TiffFile file = TiffFile.OpenRead(fileBytes);

        TiffImage image = file.GetImage(0);
        TiffImage subImage = image.GetSubImage(0);

        Assert.That(image.GetSubImageCount(), Is.EqualTo(1));
        Assert.That(subImage.Success, Is.True);
        Assert.That(subImage.GetInfo().Width, Is.EqualTo(5));
        Assert.That(subImage.GetInfo().Length, Is.EqualTo(5));
    }

    [Test]
    public void PageHandle_ReportsInvalidSubImageAndStrileIndexes()
    {
        TiffFile file = TiffFile.OpenRead(CreateClassicStripFile());
        TiffImage image = file.GetImage(0);

        TiffImage missingSubImage = image.GetSubImage(3);
        ulong stripSize = image.GetStripSize(4);
        ulong tileSize = image.GetTileSize(-1);

        Assert.That(missingSubImage.Success, Is.False);
        Assert.That(stripSize, Is.EqualTo(0));
        Assert.That(tileSize, Is.EqualTo(0));
        Assert.That(image.Success, Is.False);
        Assert.That(image.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    static byte[] CreateClassicStripFile()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 3),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.StripOffsets, TiffDataType.Long, 1, 104),
                CreateEntry(TiffTagId.StripByteCounts, TiffDataType.Long, 1, 6)
            }
        };
        byte[] fileBytes = new byte[110];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        fileBytes[104] = 1;
        fileBytes[105] = 2;
        fileBytes[106] = 3;
        fileBytes[107] = 4;
        fileBytes[108] = 5;
        fileBytes[109] = 6;
        return fileBytes;
    }

    static byte[] CreateClassicTileFile()
    {
        ulong[] offsets = new ulong[] { 200, 206, 212, 218 };
        ulong[] byteCounts = new ulong[] { 6, 6, 6, 6 };
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[]
            {
                CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 5),
                CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 4),
                CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1, 8),
                CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1, (ulong)TiffPhotometric.BlackIsZero),
                CreateEntry(TiffTagId.TileWidth, TiffDataType.Long, 1, 3),
                CreateEntry(TiffTagId.TileLength, TiffDataType.Long, 1, 2),
                CreateEntry(TiffTagId.TileOffsets, TiffDataType.Long, 4, 134),
                CreateEntry(TiffTagId.TileByteCounts, TiffDataType.Long, 4, 150)
            }
        };
        byte[] fileBytes = new byte[224];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        WriteUInt32Array(fileBytes, 134, offsets);
        WriteUInt32Array(fileBytes, 150, byteCounts);
        int index = 0;
        while (index < 24)
        {
            fileBytes[200 + index] = (byte)(index + 1);
            index = index + 1;
        }

        return fileBytes;
    }

    static void WriteUInt32Array(byte[] bytes, int offset, ulong[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + index * 4, 4), (uint)values[index], TiffByteOrder.LittleEndian);
            index = index + 1;
        }
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
