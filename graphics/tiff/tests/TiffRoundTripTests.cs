using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRoundTripTests
{
    [Test]
    public void FullImageRoundTrip_ReadsBilevelImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.BlackIsZero, 1, 1);
        byte[] unpackedSamples = new byte[] { 0, 1, 0, 1, 0, 1 };
        TiffDiagnostics packDiagnostics = new TiffDiagnostics();
        byte[] packedSamples = TiffCodec.PackSubByteSamples(TiffCodecContext.Create(info, false, 0, 2, new TiffLimits()), unpackedSamples, packDiagnostics);

        byte[] fileBytes = WriteClassicImage(info, packedSamples);
        TiffDirectory directory = ReadSingleDirectory(fileBytes);
        TiffRgba8ReadResult result = TiffCodec.ReadRgba8(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(packDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(packedSamples, Is.EqualTo(new byte[] { 64, 160 }));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255,
            255, 255, 255, 255,
            0, 0, 0, 255,
            255, 255, 255, 255,
            0, 0, 0, 255,
            255, 255, 255, 255
        }));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsGrayscaleImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.BlackIsZero, 1, 8);
        byte[] rgba = CreateGrayRgba();

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 6);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsPaletteImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.Palette, 1, 8);
        info.ColorMap = CreateRoundTripColorMap();
        byte[] rgba = new byte[]
        {
            0, 0, 0, 255,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255,
            255, 0, 0, 255
        };

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 6);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsRgbImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.Rgb, 3, 8);
        byte[] rgba = CreateRgbRgba();

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 18);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsRgbaImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.Rgb, 4, 8);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        byte[] rgba = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 192,
            0, 0, 255, 128,
            255, 255, 255, 64,
            0, 0, 0, 0,
            255, 0, 0, 32
        };

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 24);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsCmykImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.Cmyk, 4, 8);
        byte[] rgba = new byte[]
        {
            0, 255, 255, 255,
            255, 0, 255, 255,
            255, 255, 0, 255,
            0, 0, 0, 255,
            255, 255, 255, 255,
            0, 255, 255, 255
        };

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 24);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba));
    }

    [Test]
    public void FullImageRoundTrip_WritesAndReadsYCbCrImageThroughClassicTiff()
    {
        TiffImageInfo info = CreateImageInfo(3, 2, TiffPhotometric.YCbCr, 3, 8);
        byte[] rgba = new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            0, 0, 0, 255,
            255, 255, 255, 255,
            255, 0, 0, 255
        };

        TiffRgba8ReadResult result = WriteAndReadRgba8(info, rgba, 18);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(rgba).Within(1));
    }

    static TiffRgba8ReadResult WriteAndReadRgba8(TiffImageInfo info, byte[] rgba, ulong sourceByteCount)
    {
        byte[] fileBytes = WriteClassicImage(info, new byte[(int)sourceByteCount]);
        ulong dataOffset = (ulong)fileBytes.Length - sourceByteCount;
        TiffSourceRasterWriteResult writeResult = TiffCodec.WriteRgba8(info, new ulong[] { dataOffset }, new ulong[] { sourceByteCount }, fileBytes, rgba);
        Assert.That(writeResult.Success, Is.True);

        TiffDirectory directory = ReadSingleDirectory(fileBytes);
        return TiffCodec.ReadRgba8(directory, fileBytes, TiffByteOrder.LittleEndian);
    }

    static byte[] WriteClassicImage(TiffImageInfo info, byte[] sourceBytes)
    {
        TiffIfdEntry[] entries = CreateImageEntries(info, 0, (ulong)sourceBytes.Length);
        byte[][] values = CreateImageValues(info, 0, (ulong)sourceBytes.Length);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values);
        ulong dataOffset = (ulong)(8 + layoutByteCount);
        entries = CreateImageEntries(info, dataOffset, (ulong)sourceBytes.Length);
        values = CreateImageValues(info, dataOffset, (ulong)sourceBytes.Length);

        byte[] fileBytes = new byte[(int)dataOffset + sourceBytes.Length];
        TiffDiagnostics headerDiagnostics = TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDiagnostics directoryDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, values, TiffByteOrder.LittleEndian, 8, 0);
        CopyBytes(sourceBytes, fileBytes, (int)dataOffset);

        Assert.That(headerDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(directoryDiagnostics.Count(), Is.EqualTo(0));
        return fileBytes;
    }

    static TiffDirectory ReadSingleDirectory(byte[] fileBytes)
    {
        TiffHeaderReadResult headerResult = TiffHeader.ReadClassic(fileBytes);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.Read(headerResult.Header, fileBytes);

        Assert.That(headerResult.Success, Is.True);
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        return directoryResult.Directories[0];
    }

    static TiffIfdEntry[] CreateImageEntries(TiffImageInfo info, ulong stripOffset, ulong stripByteCount)
    {
        List<TiffIfdEntry> entries = new List<TiffIfdEntry>();
        AddEntry(entries, TiffTagId.ImageWidth, TiffDataType.Long, 1);
        AddEntry(entries, TiffTagId.ImageLength, TiffDataType.Long, 1);
        AddEntry(entries, TiffTagId.BitsPerSample, TiffDataType.Short, info.BitsPerSample.Length);
        AddEntry(entries, TiffTagId.Compression, TiffDataType.Short, 1);
        AddEntry(entries, TiffTagId.PhotometricInterpretation, TiffDataType.Short, 1);
        AddEntry(entries, TiffTagId.StripOffsets, TiffDataType.Long, 1);
        AddEntry(entries, TiffTagId.SamplesPerPixel, TiffDataType.Short, 1);
        AddEntry(entries, TiffTagId.RowsPerStrip, TiffDataType.Long, 1);
        AddEntry(entries, TiffTagId.StripByteCounts, TiffDataType.Long, 1);
        AddEntry(entries, TiffTagId.PlanarConfiguration, TiffDataType.Short, 1);
        if (info.ExtraSamples != null && info.ExtraSamples.Length > 0)
        {
            AddEntry(entries, TiffTagId.ExtraSamples, TiffDataType.Short, info.ExtraSamples.Length);
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            AddEntry(entries, TiffTagId.ColorMap, TiffDataType.Short, info.ColorMap.Length);
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            AddEntry(entries, TiffTagId.YCbCrSubSampling, TiffDataType.Short, 2);
        }

        return entries.ToArray();
    }

    static byte[][] CreateImageValues(TiffImageInfo info, ulong stripOffset, ulong stripByteCount)
    {
        List<byte[]> values = new List<byte[]>();
        values.Add(UInt32Bytes((uint)info.Width));
        values.Add(UInt32Bytes((uint)info.Length));
        values.Add(UInt16ArrayBytes(ToUShortArray(info.BitsPerSample)));
        values.Add(UInt16Bytes((ushort)TiffCompression.None));
        values.Add(UInt16Bytes((ushort)info.Photometric));
        values.Add(UInt32Bytes((uint)stripOffset));
        values.Add(UInt16Bytes((ushort)info.SamplesPerPixel));
        values.Add(UInt32Bytes((uint)info.RowsPerStrip));
        values.Add(UInt32Bytes((uint)stripByteCount));
        values.Add(UInt16Bytes((ushort)TiffPlanarConfiguration.Contiguous));
        if (info.ExtraSamples != null && info.ExtraSamples.Length > 0)
        {
            values.Add(UInt16ArrayBytes(ToUShortArray(info.ExtraSamples)));
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            values.Add(UInt16ArrayBytes(info.ColorMap));
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            values.Add(UInt16ArrayBytes(new ushort[] { 1, 1 }));
        }

        return values.ToArray();
    }

    static void AddEntry(List<TiffIfdEntry> entries, int tagId, TiffDataType dataType, long count)
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
        entries.Add(entry);
    }

    static TiffImageInfo CreateImageInfo(ulong width, ulong length, TiffPhotometric photometric, int samplesPerPixel, int bitsPerSample)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = length;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = CreateBits(samplesPerPixel, bitsPerSample);
        info.SampleFormats = CreateFormats(samplesPerPixel);
        info.Photometric = photometric;
        info.Compression = TiffCompression.None;
        info.PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        return info;
    }

    static byte[] CreateGrayRgba()
    {
        return new byte[]
        {
            0, 0, 0, 255,
            51, 51, 51, 255,
            102, 102, 102, 255,
            153, 153, 153, 255,
            204, 204, 204, 255,
            255, 255, 255, 255
        };
    }

    static byte[] CreateRgbRgba()
    {
        return new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 0, 255,
            0, 255, 255, 255,
            255, 0, 255, 255
        };
    }

    static ushort[] CreateRoundTripColorMap()
    {
        ushort[] colorMap = new ushort[256 * 3];
        SetPaletteEntry(colorMap, 1, 65535, 0, 0);
        SetPaletteEntry(colorMap, 2, 0, 65535, 0);
        SetPaletteEntry(colorMap, 3, 0, 0, 65535);
        SetPaletteEntry(colorMap, 4, 65535, 65535, 65535);
        return colorMap;
    }

    static void SetPaletteEntry(ushort[] colorMap, int index, ushort red, ushort green, ushort blue)
    {
        int entryCount = colorMap.Length / 3;
        colorMap[index] = red;
        colorMap[entryCount + index] = green;
        colorMap[entryCount * 2 + index] = blue;
    }

    static int[] CreateBits(int count, int bitsPerSample)
    {
        int[] bits = new int[count];
        int index = 0;
        while (index < count)
        {
            bits[index] = bitsPerSample;
            index = index + 1;
        }

        return bits;
    }

    static TiffSampleFormat[] CreateFormats(int count)
    {
        TiffSampleFormat[] formats = new TiffSampleFormat[count];
        int index = 0;
        while (index < count)
        {
            formats[index] = TiffSampleFormat.UnsignedInteger;
            index = index + 1;
        }

        return formats;
    }

    static ushort[] ToUShortArray(int[] values)
    {
        ushort[] result = new ushort[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = (ushort)values[index];
            index = index + 1;
        }

        return result;
    }

    static ushort[] ToUShortArray(TiffExtraSample[] values)
    {
        ushort[] result = new ushort[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = (ushort)values[index];
            index = index + 1;
        }

        return result;
    }

    static byte[] UInt16Bytes(ushort value)
    {
        byte[] bytes = new byte[2];
        TiffBinaryWriter.WriteUInt16(bytes, value, TiffByteOrder.LittleEndian);
        return bytes;
    }

    static byte[] UInt16ArrayBytes(ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], TiffByteOrder.LittleEndian);
            index = index + 1;
        }

        return bytes;
    }

    static byte[] UInt32Bytes(uint value)
    {
        byte[] bytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(bytes, value, TiffByteOrder.LittleEndian);
        return bytes;
    }

    static void CopyBytes(byte[] source, byte[] destination, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
    }
}
