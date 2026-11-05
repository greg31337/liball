using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeoParameterTests
{
    [Test]
    public void ReadGeoParameters_ParsesDoubleAndAsciiParams()
    {
        byte[] fileBytes = new byte[192];
        double[] doubleValues = new double[] { 1.5, 2.25, -3.75 };
        byte[] asciiBytes = CreateAsciiBytes("WGS 84|World Geodetic System 1984|");
        WriteDoubles(fileBytes, 64, doubleValues, TiffByteOrder.LittleEndian);
        WriteBytes(fileBytes, 112, asciiBytes);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[]
        {
            CreateEntry(TiffTagId.GeoDoubleParams, TiffDataType.Double, doubleValues.Length, 64),
            CreateEntry(TiffTagId.GeoAsciiParams, TiffDataType.Ascii, asciiBytes.Length, 112)
        });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasDoubleParams, Is.True);
        Assert.That(result.Metadata.DoubleParams, Is.EqualTo(doubleValues));
        Assert.That(result.Metadata.HasAsciiParams, Is.True);
        Assert.That(result.Metadata.AsciiText, Is.EqualTo("WGS 84|World Geodetic System 1984|"));
        Assert.That(result.Metadata.AsciiValueCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
        Assert.That(result.Metadata.AsciiValues[1], Is.EqualTo("World Geodetic System 1984"));
        Assert.That(result.Metadata.AsciiBytes, Is.EqualTo(asciiBytes));
    }

    [Test]
    public void ReadGeoParameters_ReturnsEmptyWhenTagsAreMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasDoubleParams, Is.False);
        Assert.That(result.Metadata.HasAsciiParams, Is.False);
        Assert.That(result.Metadata.DoubleCount(), Is.EqualTo(0));
        Assert.That(result.Metadata.AsciiValueCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadGeoParameters_ReportsInvalidDoubleTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoDoubleParams, TiffDataType.Long, 2, 64) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoDoubleParams));
    }

    [Test]
    public void ReadGeoParameters_ReportsInvalidAsciiTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoAsciiParams, TiffDataType.Byte, 4, 64) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoAsciiParams));
    }

    [Test]
    public void ReadGeoParameters_ReportsInvalidParamCount()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoDoubleParams, TiffDataType.Double, 0, 64) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoDoubleParams));
    }

    [Test]
    public void ReadGeoParameters_ReportsExternalValueRangeOutsideFile()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoDoubleParams, TiffDataType.Double, 2, 120) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, new byte[128], TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadGeoParameters_ReportsInvalidAsciiByte()
    {
        byte[] fileBytes = new byte[128];
        fileBytes[64] = 65;
        fileBytes[65] = 200;
        fileBytes[66] = 124;
        fileBytes[67] = 66;
        fileBytes[68] = 124;
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GeoAsciiParams, TiffDataType.Ascii, 5, 64) });

        TiffGeoParameterReadResult result = TiffCodec.ReadGeoParameters(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
    }

    static byte[] CreateAsciiBytes(string text)
    {
        byte[] bytes = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            bytes[index] = (byte)text[index];
            index = index + 1;
        }

        return bytes;
    }

    static void WriteDoubles(byte[] bytes, int offset, double[] values, TiffByteOrder byteOrder)
    {
        int index = 0;
        while (index < values.Length)
        {
            TiffBinaryWriter.WriteDouble(bytes.AsSpan(offset + (index * 8), 8), values[index], byteOrder);
            index = index + 1;
        }
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

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
