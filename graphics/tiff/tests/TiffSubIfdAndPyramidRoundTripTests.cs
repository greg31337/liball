using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSubIfdAndPyramidRoundTripTests
{
    [Test]
    public void Writer_RoundTripsSubIfdThroughPublicFileApi()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(8, 8, false)));
        int subImageIndex = writer.AddSubImage(imageIndex, TiffImageDefinition.Create(CreateStripTags(4, 4, true)));

        writer.WriteRawStrip(imageIndex, 0, CreateBytes(64));
        TiffDiagnostics flushDiagnostics = writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffImage subImage = image.GetSubImage(subImageIndex);
        TiffSubIfdReadResult subIfdResult = image.ReadSubIfds();
        TiffPyramidDetectionResult pyramidResult = TiffCodec.DetectPyramid(image.GetDirectory(), fileBytes, TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
        Assert.That(image.GetSubImageCount(), Is.EqualTo(1));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(1));
        Assert.That(subIfdResult.SubIfds[0].ParentOffset, Is.EqualTo(file.GetDirectory(0).Offset));
        Assert.That(subIfdResult.SubIfds[0].Directories[0].NextIfdOffset, Is.EqualTo(0));
        Assert.That(image.GetTag(TiffTagId.SubIFDs), Is.Not.Null);
        Assert.That(subImage.Success, Is.True);
        Assert.That(subImage.GetInfo().Width, Is.EqualTo(4));
        Assert.That(subImage.GetInfo().Length, Is.EqualTo(4));
        Assert.That(ReadUnsigned(subImage.GetDirectory(), fileBytes, TiffByteOrder.LittleEndian, TiffTagId.NewSubfileType), Is.EqualTo(1));
        Assert.That(pyramidResult.Success, Is.True);
        Assert.That(pyramidResult.Pyramid.HasPyramid, Is.True);
        Assert.That(pyramidResult.Pyramid.LevelCount(), Is.EqualTo(1));
        Assert.That(pyramidResult.Pyramid.Levels[0].Width, Is.EqualTo(4));
        Assert.That(pyramidResult.Pyramid.Levels[0].Length, Is.EqualTo(4));
    }

    [Test]
    public void ClassicPyramid_RoundTripsThroughPublicFileApi()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(1024, 1024);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(512, 512, 0, false),
            CreateOverviewEntries(256, 256, 0, false)
        };
        int byteCount = TiffCodec.CalculateClassicPyramidWriteByteCount(parentEntries, overviewDirectories);
        byte[] fileBytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffPyramidWriteResult writeResult = TiffCodec.WriteClassicPyramid(fileBytes, parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 8, 0);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffSubIfdReadResult subIfdResult = image.ReadSubIfds();
        TiffPyramidDetectionResult pyramidResult = TiffCodec.DetectPyramid(image.GetDirectory(), file.GetFileBytes(), file.GetHeader().ByteOrder, file.GetHeader().FileKind);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(writeResult.BytesWritten, Is.EqualTo((ulong)byteCount));
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.Classic));
        Assert.That(image.GetSubImageCount(), Is.EqualTo(2));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(file.GetDirectory(0).FindEntry(TiffTagId.SubIFDs), Is.Not.Null);
        Assert.That(ReadUnsigned(subIfdResult.SubIfds[0].Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffTagId.NewSubfileType), Is.EqualTo(1));
        AssertPyramid(pyramidResult, 1024, 1024, new ulong[] { 512, 256 });
    }

    [Test]
    public void BigTiffPyramid_RoundTripsThroughPublicFileApi()
    {
        TiffIfdEntry[] parentEntries = CreateParentEntries(2048, 2048);
        TiffIfdEntry[][] overviewDirectories = new TiffIfdEntry[][]
        {
            CreateOverviewEntries(1024, 1024, 0, false),
            CreateOverviewEntries(512, 512, 0, false)
        };
        int byteCount = TiffCodec.CalculateBigTiffPyramidWriteByteCount(parentEntries, overviewDirectories);
        byte[] fileBytes = new byte[16 + byteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffPyramidWriteResult writeResult = TiffCodec.WriteBigTiffPyramid(fileBytes, parentEntries, overviewDirectories, TiffByteOrder.LittleEndian, 16, 0);
        TiffFile file = TiffFile.OpenRead(fileBytes);
        TiffImage image = file.GetImage(0);
        TiffSubIfdReadResult subIfdResult = image.ReadSubIfds();
        TiffPyramidDetectionResult pyramidResult = TiffCodec.DetectPyramid(image.GetDirectory(), file.GetFileBytes(), file.GetHeader().ByteOrder, file.GetHeader().FileKind);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(writeResult.BytesWritten, Is.EqualTo((ulong)byteCount));
        Assert.That(file.Success, Is.True);
        Assert.That(file.GetImageCount(), Is.EqualTo(1));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(image.GetSubImageCount(), Is.EqualTo(2));
        Assert.That(subIfdResult.Success, Is.True);
        Assert.That(subIfdResult.SubIfdCount(), Is.EqualTo(2));
        Assert.That(file.GetDirectory(0).FindEntry(TiffTagId.SubIFDs)!.DataType, Is.EqualTo(TiffDataType.Long8));
        Assert.That(ReadUnsigned(subIfdResult.SubIfds[0].Directories[0], fileBytes, TiffByteOrder.LittleEndian, TiffTagId.NewSubfileType), Is.EqualTo(1));
        AssertPyramid(pyramidResult, 2048, 2048, new ulong[] { 1024, 512 });
    }

    static void AssertPyramid(TiffPyramidDetectionResult result, ulong baseWidth, ulong baseLength, ulong[] expectedWidths)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Pyramid.HasPyramid, Is.True);
        Assert.That(result.Pyramid.BaseWidth, Is.EqualTo(baseWidth));
        Assert.That(result.Pyramid.BaseLength, Is.EqualTo(baseLength));
        Assert.That(result.Pyramid.LevelCount(), Is.EqualTo(expectedWidths.Length));

        int index = 0;
        while (index < expectedWidths.Length)
        {
            Assert.That(result.Pyramid.Levels[index].LevelIndex, Is.EqualTo(index));
            Assert.That(result.Pyramid.Levels[index].SubIfdIndex, Is.EqualTo(index));
            Assert.That(result.Pyramid.Levels[index].Width, Is.EqualTo(expectedWidths[index]));
            Assert.That(result.Pyramid.Levels[index].Length, Is.EqualTo(expectedWidths[index]));
            Assert.That(result.Pyramid.Levels[index].IsReducedResolution, Is.True);
            index = index + 1;
        }
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, bool reducedResolution)
    {
        TiffTagValue[] tags = new TiffTagValue[5];
        if (reducedResolution)
        {
            tags = new TiffTagValue[6];
        }

        tags[0] = TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width);
        tags[1] = TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length);
        tags[2] = TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8);
        tags[3] = TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero);
        tags[4] = TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length);
        if (reducedResolution)
        {
            tags[5] = TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1);
        }

        return tags;
    }

    static TiffIfdEntry[] CreateParentEntries(uint width, uint length)
    {
        return new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length)
        };
    }

    static TiffIfdEntry[] CreateOverviewEntries(uint width, uint length, uint newSubfileType, bool includeNewSubfileType)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[2];
        if (includeNewSubfileType)
        {
            entries = new TiffIfdEntry[3];
        }

        entries[0] = CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, width);
        entries[1] = CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1, length);
        if (includeNewSubfileType)
        {
            entries[2] = CreateEntry(TiffTagId.NewSubfileType, TiffDataType.Long, 1, newSubfileType);
        }

        return entries;
    }

    static ulong ReadUnsigned(TiffDirectory directory, byte[] fileBytes, TiffByteOrder byteOrder, int tagId)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ulong value = TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, new TiffLimits(), tagId, 0, diagnostics);
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        return value;
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

    static byte[] CreateBytes(int count)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index + 1);
            index = index + 1;
        }

        return bytes;
    }
}
