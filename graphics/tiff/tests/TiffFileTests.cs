using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffFileTests
{
    [Test]
    public void OpenRead_ReadsClassicHeaderAndDirectories()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) },
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, 50) }
        };
        byte[] fileBytes = new byte[8 + TiffDirectoryWriter.CalculateClassicDirectoryChainByteCount(directories)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);

        TiffFile file = TiffFile.OpenRead(fileBytes);

        Assert.That(file.Success, Is.True);
        Assert.That(file.IsOpen, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(file.GetHeader().FirstIfdOffset, Is.EqualTo(8));
        Assert.That(file.GetImageCount(), Is.EqualTo(2));
        Assert.That(file.GetImage(0).GetTag(TiffTagId.ImageWidth)!.ValueOffset, Is.EqualTo(100));
        Assert.That(file.GetImage(1).GetTag(TiffTagId.ImageLength)!.ValueOffset, Is.EqualTo(50));
    }

    [Test]
    public void OpenRead_ReadsBigTiffHeaderAndDirectories()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 200) }
        };
        byte[] fileBytes = new byte[16 + TiffDirectoryWriter.CalculateBigTiffDirectoryChainByteCount(directories)];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);
        TiffDirectoryWriter.WriteBigTiffChain(fileBytes, directories, TiffByteOrder.LittleEndian, 16);

        TiffFile file = TiffFile.OpenRead(fileBytes);

        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
        Assert.That(file.GetImage(0).GetTag(TiffTagId.ImageWidth)!.ValueOffset, Is.EqualTo(200));
    }

    [Test]
    public void Accessors_ReturnCopies()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) }
        };
        byte[] fileBytes = new byte[8 + TiffDirectoryWriter.CalculateClassicDirectoryChainByteCount(directories)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);

        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffHeader header = file.GetHeader();
        TiffImage image = file.GetImage(0);
        TiffImage[] images = file.GetImages();
        TiffDirectory directory = file.GetDirectory(0);
        TiffDirectory[] directoriesCopy = file.GetDirectories();
        byte[] bytes = file.GetFileBytes();
        header.FirstIfdOffset = 999;
        image.Directory.Entries[0].ValueOffset = 333;
        images[0].Directory.Entries[0].ValueOffset = 444;
        directory.Entries[0].ValueOffset = 555;
        directoriesCopy[0].Entries[0].ValueOffset = 666;
        bytes[0] = 0;

        Assert.That(file.GetHeader().FirstIfdOffset, Is.EqualTo(8));
        Assert.That(file.GetImage(0).Directory.Entries[0].ValueOffset, Is.EqualTo(100));
        Assert.That(file.GetImages()[0].Directory.Entries[0].ValueOffset, Is.EqualTo(100));
        Assert.That(file.GetDirectory(0).Entries[0].ValueOffset, Is.EqualTo(100));
        Assert.That(file.GetDirectories()[0].Entries[0].ValueOffset, Is.EqualTo(100));
        Assert.That(file.GetFileBytes()[0], Is.EqualTo((byte)0x49));
    }

    [Test]
    public void OpenRead_ReportsInvalidHeader()
    {
        TiffFile file = TiffFile.OpenRead(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

        Assert.That(file.Success, Is.False);
        Assert.That(file.IsOpen, Is.False);
        Assert.That(file.GetImageCount(), Is.EqualTo(0));
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void GetImage_ReportsOutOfRangeIndex()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) }
        };
        byte[] fileBytes = new byte[8 + TiffDirectoryWriter.CalculateClassicDirectoryChainByteCount(directories)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        TiffFile file = TiffFile.OpenRead(fileBytes);

        TiffImage image = file.GetImage(2);

        Assert.That(image.Success, Is.False);
        Assert.That(image.GetDirectory().EntryCount(), Is.EqualTo(0));
        Assert.That(file.Success, Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.InvalidOffset), Is.True);
    }

    [Test]
    public void Close_ClearsOpenStateAndFileBytes()
    {
        TiffIfdEntry[][] directories = new TiffIfdEntry[][]
        {
            new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) }
        };
        byte[] fileBytes = new byte[8 + TiffDirectoryWriter.CalculateClassicDirectoryChainByteCount(directories)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDirectoryWriter.WriteClassicChain(fileBytes, directories, TiffByteOrder.LittleEndian, 8);
        TiffFile file = TiffFile.OpenRead(fileBytes);

        file.Close();

        Assert.That(file.IsOpen, Is.False);
        Assert.That(file.GetFileByteCount(), Is.EqualTo(0));
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
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
