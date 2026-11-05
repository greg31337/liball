using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageJExpandedMetadataTests
{
    [Test]
    public void ReadImageJMetadata_ExposesHyperstackAxesAndCalibration()
    {
        string description = "ImageJ=1.54f\nimages=24\nchannels=3\nslices=4\nframes=2\nhyperstack=true\nunit=micron\nspacing=0.25\nfinterval=1.5e-1\ntunit=second\nfps=6.5\npixel_width=0.5\npixel_height=0.75\nxorigin=-2\nyorigin=3\nzorigin=4\n";
        TiffDirectory directory = CreateDirectory(description, new byte[0], new byte[0], out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.Axes, Has.Length.EqualTo(3));
        Assert.That(result.Metadata.Axes[0].Kind, Is.EqualTo(TiffImageJAxisKind.Channel));
        Assert.That(result.Metadata.Axes[0].Size, Is.EqualTo(3));
        Assert.That(result.Metadata.Axes[0].StorageOrder, Is.EqualTo(0));
        Assert.That(result.Metadata.Axes[1].Kind, Is.EqualTo(TiffImageJAxisKind.Depth));
        Assert.That(result.Metadata.Axes[1].Size, Is.EqualTo(4));
        Assert.That(result.Metadata.Axes[2].Kind, Is.EqualTo(TiffImageJAxisKind.Time));
        Assert.That(result.Metadata.Axes[2].Size, Is.EqualTo(2));
        Assert.That(result.Metadata.Calibration.Unit, Is.EqualTo("micron"));
        Assert.That(result.Metadata.Calibration.PixelWidth, Is.EqualTo(0.5d));
        Assert.That(result.Metadata.Calibration.PixelHeight, Is.EqualTo(0.75d));
        Assert.That(result.Metadata.Calibration.PixelDepth, Is.EqualTo(0.25d));
        Assert.That(result.Metadata.Calibration.FrameInterval, Is.EqualTo(0.15d).Within(0.0000001d));
        Assert.That(result.Metadata.Calibration.TimeUnit, Is.EqualTo("second"));
        Assert.That(result.Metadata.Calibration.FramesPerSecond, Is.EqualTo(6.5d));
        Assert.That(result.Metadata.Calibration.XOrigin, Is.EqualTo(-2d));
        Assert.That(result.Metadata.Calibration.YOrigin, Is.EqualTo(3d));
        Assert.That(result.Metadata.Calibration.ZOrigin, Is.EqualTo(4d));
    }

    [Test]
    public void ReadImageJMetadata_UsesTiffResolutionForPixelCalibration()
    {
        List<TiffTagValue> tags = new List<TiffTagValue>();
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 1));
        tags.Add(TiffTagValue.CreateRationalScalar(TiffTagId.XResolution, TiffDataType.Rational, 2, 1));
        tags.Add(TiffTagValue.CreateRationalScalar(TiffTagId.YResolution, TiffDataType.Rational, 4, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ResolutionUnit, TiffDataType.Short, (ulong)TiffResolutionUnit.Inch));
        tags.Add(TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "ImageJ=1.54f\n"));
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags.ToArray()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });
        writer.Flush();
        byte[] fileBytes = writer.GetFileBytes();
        TiffFile file = TiffFile.OpenRead(fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(file.GetDirectory(0), fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.Calibration.PixelWidth, Is.EqualTo(0.5d));
        Assert.That(result.Metadata.Calibration.PixelHeight, Is.EqualTo(0.25d));
        Assert.That(result.Metadata.Calibration.Unit, Is.EqualTo("inch"));
    }

    [Test]
    public void ReadImageJMetadata_DecodesKnownPrivateBlocksAndPreservesUnknownBlocks()
    {
        TiffByteOrder byteOrder = TiffByteOrder.LittleEndian;
        uint unknownType = 0x78797a31;
        uint[] types = new uint[]
        {
            TiffImageJPrivateMetadataReader.InfoType,
            TiffImageJPrivateMetadataReader.LabelsType,
            TiffImageJPrivateMetadataReader.RangesType,
            TiffImageJPrivateMetadataReader.LutsType,
            TiffImageJPrivateMetadataReader.PlotType,
            TiffImageJPrivateMetadataReader.RoiType,
            TiffImageJPrivateMetadataReader.OverlayType,
            TiffImageJPrivateMetadataReader.PropertiesType,
            unknownType
        };
        int[] counts = new int[] { 1, 2, 1, 2, 1, 1, 2, 2, 2 };
        byte[][] items = new byte[][]
        {
            EncodeUtf16("acquisition info", byteOrder),
            EncodeUtf16("slice one", byteOrder),
            EncodeUtf16("slice two", byteOrder),
            EncodeDoubles(new double[] { 0d, 255d, 10d, 20d }, byteOrder),
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 },
            new byte[] { 7, 8 },
            new byte[] { 9, 10 },
            new byte[] { 11, 12 },
            new byte[] { 13, 14 },
            EncodeUtf16("key=value", byteOrder),
            EncodeUtf16("name=sample", byteOrder),
            new byte[] { 21, 22, 23 },
            new byte[] { 24, 25 }
        };
        byte[] metadataBytes = BuildPrivateMetadata(types, counts, items, byteOrder, out byte[] countBytes);
        TiffDirectory directory = CreateDirectory("ImageJ=1.54f\nimages=2\n", countBytes, metadataBytes, out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasPrivateMetadata, Is.True);
        Assert.That(result.Metadata.Info, Is.EqualTo("acquisition info"));
        Assert.That(result.Metadata.SliceLabels, Is.EqualTo(new string[] { "slice one", "slice two" }));
        Assert.That(result.Metadata.DisplayRanges, Has.Length.EqualTo(2));
        Assert.That(result.Metadata.DisplayRanges[0].Minimum, Is.EqualTo(0d));
        Assert.That(result.Metadata.DisplayRanges[0].Maximum, Is.EqualTo(255d));
        Assert.That(result.Metadata.DisplayRanges[1].ChannelIndex, Is.EqualTo(1));
        Assert.That(result.Metadata.DisplayRanges[1].Minimum, Is.EqualTo(10d));
        Assert.That(result.Metadata.DisplayRanges[1].Maximum, Is.EqualTo(20d));
        Assert.That(result.Metadata.ChannelLuts, Is.EqualTo(new byte[][] { new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 } }));
        Assert.That(result.Metadata.Plot, Is.EqualTo(new byte[] { 7, 8 }));
        Assert.That(result.Metadata.Roi, Is.EqualTo(new byte[] { 9, 10 }));
        Assert.That(result.Metadata.Overlays, Is.EqualTo(new byte[][] { new byte[] { 11, 12 }, new byte[] { 13, 14 } }));
        Assert.That(result.Metadata.Properties, Is.EqualTo(new string[] { "key=value", "name=sample" }));
        Assert.That(result.Metadata.UnrecognizedBlocks, Has.Length.EqualTo(2));
        Assert.That(result.Metadata.UnrecognizedBlocks[0].TypeCode, Is.EqualTo(unknownType));
        Assert.That(result.Metadata.UnrecognizedBlocks[0].ItemIndex, Is.EqualTo(0));
        Assert.That(result.Metadata.UnrecognizedBlocks[0].Data, Is.EqualTo(new byte[] { 21, 22, 23 }));
        Assert.That(result.Metadata.UnrecognizedBlocks[1].Data, Is.EqualTo(new byte[] { 24, 25 }));
        Assert.That(result.Metadata.RawPrivateMetadataBytes, Is.EqualTo(metadataBytes));
        Assert.That(result.Metadata.PrivateMetadataByteCounts[0], Is.EqualTo(76));
    }

    [Test]
    public void ReadImageJMetadata_DetectsBigEndianPrivateMetadataWithoutDescription()
    {
        TiffByteOrder byteOrder = TiffByteOrder.BigEndian;
        uint[] types = new uint[] { TiffImageJPrivateMetadataReader.LabelsType, TiffImageJPrivateMetadataReader.RangesType };
        int[] counts = new int[] { 1, 1 };
        byte[][] items = new byte[][]
        {
            EncodeUtf16("big endian label", byteOrder),
            EncodeDoubles(new double[] { -1.5d, 42.25d }, byteOrder)
        };
        byte[] metadataBytes = BuildPrivateMetadata(types, counts, items, byteOrder, out byte[] countBytes);
        TiffDirectory directory = CreateDirectory("", countBytes, metadataBytes, out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasImageJ, Is.True);
        Assert.That(result.Metadata.HasPrivateMetadata, Is.True);
        Assert.That(result.Metadata.SliceLabels, Is.EqualTo(new string[] { "big endian label" }));
        Assert.That(result.Metadata.DisplayRanges[0].Minimum, Is.EqualTo(-1.5d));
        Assert.That(result.Metadata.DisplayRanges[0].Maximum, Is.EqualTo(42.25d));
    }

    [Test]
    public void ReadImageJMetadata_ReportsMissingPrivateMetadataPair()
    {
        byte[] countBytes = EncodeUInt32Values(new uint[] { 12, 4 }, TiffByteOrder.LittleEndian);
        TiffDirectory directory = CreateDirectory("ImageJ=1.54f\n", countBytes, new byte[0], out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static byte[] BuildPrivateMetadata(uint[] types, int[] counts, byte[][] items, TiffByteOrder byteOrder, out byte[] countBytes)
    {
        int headerSize = 4 + types.Length * 8;
        int totalLength = headerSize;
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            totalLength = totalLength + items[itemIndex].Length;
            itemIndex = itemIndex + 1;
        }

        byte[] metadata = new byte[totalLength];
        TiffBinaryWriter.WriteUInt32(metadata.AsSpan(0, 4), TiffImageJPrivateMetadataReader.Magic, byteOrder);
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            int offset = 4 + typeIndex * 8;
            TiffBinaryWriter.WriteUInt32(metadata.AsSpan(offset, 4), types[typeIndex], byteOrder);
            TiffBinaryWriter.WriteUInt32(metadata.AsSpan(offset + 4, 4), (uint)counts[typeIndex], byteOrder);
            typeIndex = typeIndex + 1;
        }

        uint[] byteCountValues = new uint[items.Length + 1];
        byteCountValues[0] = (uint)headerSize;
        int dataOffset = headerSize;
        itemIndex = 0;
        while (itemIndex < items.Length)
        {
            byteCountValues[itemIndex + 1] = (uint)items[itemIndex].Length;
            WriteBytes(metadata, dataOffset, items[itemIndex]);
            dataOffset = dataOffset + items[itemIndex].Length;
            itemIndex = itemIndex + 1;
        }

        countBytes = EncodeUInt32Values(byteCountValues, byteOrder);
        return metadata;
    }

    static byte[] EncodeUInt32Values(uint[] values, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 4];
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            index = index + 1;
        }

        return bytes;
    }

    static byte[] EncodeDoubles(double[] values, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 8];
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteDouble(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
            index = index + 1;
        }

        return bytes;
    }

    static byte[] EncodeUtf16(string text, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[text.Length * 2];
        int index = 0;
        while (index < text.Length)
        {
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), text[index], byteOrder);
            index = index + 1;
        }

        return bytes;
    }

    static TiffDirectory CreateDirectory(string description, byte[] countBytes, byte[] metadataBytes, out byte[] fileBytes)
    {
        int descriptionOffset = 64;
        byte[] descriptionBytes = new byte[0];
        if (description.Length > 0)
        {
            descriptionBytes = TiffAscii.EncodeString(description);
        }

        int countsOffset = descriptionOffset + descriptionBytes.Length;
        int metadataOffset = countsOffset + countBytes.Length;
        fileBytes = new byte[metadataOffset + metadataBytes.Length + 16];
        WriteBytes(fileBytes, descriptionOffset, descriptionBytes);
        WriteBytes(fileBytes, countsOffset, countBytes);
        WriteBytes(fileBytes, metadataOffset, metadataBytes);

        TiffIfdEntry[] entries = new TiffIfdEntry[0];
        if (descriptionBytes.Length > 0)
        {
            entries = AppendEntry(entries, CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, descriptionBytes.Length, descriptionOffset));
        }

        if (countBytes.Length > 0)
        {
            entries = AppendEntry(entries, CreateEntry(TiffTagId.ImageJMetadataByteCounts, TiffDataType.Long, countBytes.Length / 4, countsOffset));
        }

        if (metadataBytes.Length > 0)
        {
            entries = AppendEntry(entries, CreateEntry(TiffTagId.ImageJMetadata, TiffDataType.Byte, metadataBytes.Length, metadataOffset));
        }

        return TiffDirectory.Create(0, 8, 0, entries);
    }

    static TiffIfdEntry[] AppendEntry(TiffIfdEntry[] entries, TiffIfdEntry entry)
    {
        TiffIfdEntry[] result = new TiffIfdEntry[entries.Length + 1];
        int index = 0;
        while (index < entries.Length)
        {
            result[index] = entries[index];
            index = index + 1;
        }

        result[index] = entry;
        return result;
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, int count, int offset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = (ulong)offset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)offset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }

    static void WriteBytes(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
    }
}
