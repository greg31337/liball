using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageInfoExtractorTests
{
    [Test]
    public void Extract_AppliesBaselineDefaults()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(640, TiffByteOrder.BigEndian),
            UInt16Bytes(480, TiffByteOrder.BigEndian),
            UInt16Bytes((ushort)TiffPhotometric.BlackIsZero, TiffByteOrder.BigEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.BigEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.BigEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.BigEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(640));
        Assert.That(result.Info.Length, Is.EqualTo(480));
        Assert.That(result.Info.SamplesPerPixel, Is.EqualTo(1));
        Assert.That(result.Info.BitsPerSample, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Info.Compression, Is.EqualTo(TiffCompression.None));
        Assert.That(result.Info.FillOrder, Is.EqualTo(1));
        Assert.That(result.Info.Photometric, Is.EqualTo(TiffPhotometric.BlackIsZero));
        Assert.That(result.Info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Contiguous));
        Assert.That(result.Info.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger }));
        Assert.That(result.Info.ExtraSamples, Is.EqualTo(new TiffExtraSample[0]));
        Assert.That(result.Info.ColorMap, Is.EqualTo(new ushort[0]));
        Assert.That(result.Info.Orientation, Is.EqualTo(TiffOrientation.TopLeft));
        Assert.That(result.Info.RowsPerStrip, Is.EqualTo(uint.MaxValue));
        Assert.That(result.Info.T4Options, Is.EqualTo(0));
        Assert.That(result.Info.T6Options, Is.EqualTo(0));
        Assert.That(result.Info.YCbCrCoefficients, Is.EqualTo(new double[] { 0.299d, 0.587d, 0.114d }));
        Assert.That(result.Info.IsTiled, Is.False);
        Assert.That(result.Info.ReferenceBlackWhite, Is.EqualTo(new double[] { 0d, 255d, 128d, 255d, 128d, 255d }));
    }

    [Test]
    public void Extract_ReadsRgbExtensionTagsAndExternalArrays()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 4),
            CreateEntry(TiffTagId.Compression, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PlanarConfiguration, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.Orientation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.RowsPerStrip, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.TileWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.TileLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ExtraSamples, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.SampleFormat, TiffDataType.Short, 4),
            CreateEntry(TiffTagId.Predictor, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.YCbCrSubSampling, TiffDataType.Short, 2)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(320, TiffByteOrder.LittleEndian),
            UInt32Bytes(200, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(new ushort[] { 8, 8, 8, 8 }, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffCompression.PackBits, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.Rgb, TiffByteOrder.LittleEndian),
            UInt16Bytes(4, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPlanarConfiguration.Separate, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffOrientation.BottomRight, TiffByteOrder.LittleEndian),
            UInt32Bytes(16, TiffByteOrder.LittleEndian),
            UInt32Bytes(64, TiffByteOrder.LittleEndian),
            UInt32Bytes(32, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffExtraSample.UnassociatedAlpha, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(new ushort[] { 1, 1, 1, 1 }, TiffByteOrder.LittleEndian),
            UInt16Bytes(2, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(new ushort[] { 4, 2 }, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(320));
        Assert.That(result.Info.Length, Is.EqualTo(200));
        Assert.That(result.Info.SamplesPerPixel, Is.EqualTo(4));
        Assert.That(result.Info.BitsPerSample, Is.EqualTo(new int[] { 8, 8, 8, 8 }));
        Assert.That(result.Info.Compression, Is.EqualTo(TiffCompression.PackBits));
        Assert.That(result.Info.Photometric, Is.EqualTo(TiffPhotometric.Rgb));
        Assert.That(result.Info.PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(result.Info.Orientation, Is.EqualTo(TiffOrientation.BottomRight));
        Assert.That(result.Info.RowsPerStrip, Is.EqualTo(16));
        Assert.That(result.Info.IsTiled, Is.True);
        Assert.That(result.Info.TileWidth, Is.EqualTo(64));
        Assert.That(result.Info.TileLength, Is.EqualTo(32));
        Assert.That(result.Info.ExtraSamples, Is.EqualTo(new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha }));
        Assert.That(result.Info.SampleFormats, Is.EqualTo(new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger }));
        Assert.That(result.Info.Predictor, Is.EqualTo(2));
        Assert.That(result.Info.YCbCrSubSamplingHorizontal, Is.EqualTo(4));
        Assert.That(result.Info.YCbCrSubSamplingVertical, Is.EqualTo(2));
    }

    [Test]
    public void Extract_ReadsPaletteColorMap()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.ColorMap, TiffDataType.Short, 12)
        };
        ushort[] colorMap = new ushort[] { 0, 10, 20, 30, 1, 11, 21, 31, 2, 12, 22, 32 };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(2, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes(2, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.Palette, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(colorMap, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Photometric, Is.EqualTo(TiffPhotometric.Palette));
        Assert.That(result.Info.ColorMap, Is.EqualTo(colorMap));
    }

    [Test]
    public void Extract_ReadsYCbCrRationalTags()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 3),
            CreateEntry(TiffTagId.YCbCrCoefficients, TiffDataType.Rational, 3),
            CreateEntry(TiffTagId.ReferenceBlackWhite, TiffDataType.Rational, 6)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.YCbCr, TiffByteOrder.LittleEndian),
            UInt16Bytes(3, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(new ushort[] { 8, 8, 8 }, TiffByteOrder.LittleEndian),
            RationalArrayBytes(new TiffRational[] { TiffRational.Create(1, 4), TiffRational.Create(1, 2), TiffRational.Create(1, 4) }, TiffByteOrder.LittleEndian),
            RationalArrayBytes(new TiffRational[] { TiffRational.Create(16, 1), TiffRational.Create(235, 1), TiffRational.Create(128, 1), TiffRational.Create(240, 1), TiffRational.Create(128, 1), TiffRational.Create(240, 1) }, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.YCbCrCoefficients, Is.EqualTo(new double[] { 0.25d, 0.5d, 0.25d }));
        Assert.That(result.Info.ReferenceBlackWhite, Is.EqualTo(new double[] { 16d, 235d, 128d, 240d, 128d, 240d }));
    }

    [Test]
    public void Extract_ReadsBigTiffLong8Dimensions()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long8, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt64Bytes(0x0000000100000000UL, TiffByteOrder.LittleEndian),
            UInt32Bytes(1024, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.BlackIsZero, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteBigTiff(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Width, Is.EqualTo(0x0000000100000000UL));
        Assert.That(result.Info.Length, Is.EqualTo(1024));
    }

    [Test]
    public void Extract_ReportsUnsignedScalarThatDoesNotFitSignedModelField()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.SamplesPerPixel, TiffDataType.Long8, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.BlackIsZero, TiffByteOrder.LittleEndian),
            UInt64Bytes(ulong.MaxValue, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteBigTiff(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
        Assert.That(result.Info.SamplesPerPixel, Is.EqualTo(0));
    }

    [Test]
    public void Extract_ReadsT4Options()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.Compression, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.T4Options, TiffDataType.Long, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(8, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffCompression.CcittGroup3, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.WhiteIsZero, TiffByteOrder.LittleEndian),
            UInt32Bytes(4, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Compression, Is.EqualTo(TiffCompression.CcittGroup3));
        Assert.That(result.Info.T4Options, Is.EqualTo(4));
    }

    [Test]
    public void Extract_ReadsFillOrder()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.FillOrder, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(8, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes(2, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.WhiteIsZero, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.FillOrder, Is.EqualTo(2));
    }

    [Test]
    public void Extract_ReadsT6Options()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.Compression, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.T6Options, TiffDataType.Long, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(8, TiffByteOrder.LittleEndian),
            UInt32Bytes(1, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffCompression.CcittGroup4, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.WhiteIsZero, TiffByteOrder.LittleEndian),
            UInt32Bytes(2, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.Compression, Is.EqualTo(TiffCompression.CcittGroup4));
        Assert.That(result.Info.T6Options, Is.EqualTo(2));
    }

    [Test]
    public void Extract_ReportsMissingRequiredTags()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(640, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.MissingRequiredTag), Is.True);
    }

    [Test]
    public void Extract_ReportsArrayCountsThatDoNotMatchSamplesPerPixel()
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.ImageLength, TiffDataType.Long, 1),
            CreateEntry(TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1),
            CreateEntry(TiffTagId.BitsPerSample, TiffDataType.Short, 2)
        };
        byte[][] values = new byte[][]
        {
            UInt32Bytes(640, TiffByteOrder.LittleEndian),
            UInt32Bytes(480, TiffByteOrder.LittleEndian),
            UInt16Bytes((ushort)TiffPhotometric.Rgb, TiffByteOrder.LittleEndian),
            UInt16Bytes(3, TiffByteOrder.LittleEndian),
            UInt16ArrayBytes(new ushort[] { 8, 8 }, TiffByteOrder.LittleEndian)
        };
        byte[] fileBytes = WriteClassic(entries, values, TiffByteOrder.LittleEndian);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Info.BitsPerSample, Is.EqualTo(new int[] { 8, 8 }));
    }

    static byte[] WriteClassic(TiffIfdEntry[] entries, byte[][] values, TiffByteOrder byteOrder)
    {
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), byteOrder, 8);
        TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, values, byteOrder, 8, 0);
        return fileBytes;
    }

    static byte[] WriteBigTiff(TiffIfdEntry[] entries, byte[][] values, TiffByteOrder byteOrder)
    {
        int layoutByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[16 + layoutByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), byteOrder, 16);
        TiffDirectoryWriter.WriteBigTiffWithValueBytes(fileBytes.AsSpan(16, layoutByteCount), entries, values, byteOrder, 16, 0);
        return fileBytes;
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = 0;
        entry.ValueOffsetBytes = new byte[0];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 0;
        return entry;
    }

    static byte[] UInt16Bytes(ushort value, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[2];
        TiffBinaryWriter.WriteUInt16(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] UInt16ArrayBytes(ushort[] values, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index = index + 1;
        }

        return bytes;
    }

    static byte[] UInt32Bytes(uint value, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] UInt64Bytes(ulong value, TiffByteOrder byteOrder)
    {
        byte[] bytes = new byte[8];
        TiffBinaryWriter.WriteUInt64(bytes, value, byteOrder);
        return bytes;
    }

    static byte[] RationalArrayBytes(TiffRational[] values, TiffByteOrder byteOrder)
    {
        return TiffRationalCodec.Encode(values, TiffDataType.Rational, byteOrder).Bytes;
    }
}
