using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExifMetadataReaderTests
{
    const int ExposureTime = 33434;
    const int ExifVersion = 36864;
    const int DateTimeOriginal = 36867;
    const int IsoSpeedRatings = 34855;
    const int PixelXDimension = 40962;

    [Test]
    public void ReadExif_LoadsTypedValuesFromClassicExifIfd()
    {
        byte[] bytes = new byte[220];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicParentWithOnePointer(bytes, 8, TiffTagId.ExifIfd, 48);
        WriteClassicExifDirectory(bytes, 48);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(128, 4), 1, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(132, 4), 125, TiffByteOrder.LittleEndian);
        WriteAscii(bytes, 144, "2026:08:06 12:34:56");
        TiffHeaderReadResult header = TiffHeader.ReadClassic(bytes);
        TiffDirectory parent = TiffDirectoryReader.Read(header.Header, bytes).Directories[0];

        TiffExifReadResult result = TiffCodec.ReadExif(parent, bytes, header.Header);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasExif, Is.True);
        Assert.That(result.Metadata.ExifOffset, Is.EqualTo(48));
        Assert.That(result.Metadata.ValueCount(), Is.EqualTo(5));
        Assert.That(result.Metadata.GetAscii(DateTimeOriginal), Is.EqualTo("2026:08:06 12:34:56"));
        Assert.That(result.Metadata.GetUnsignedScalar(IsoSpeedRatings), Is.EqualTo(200));
        Assert.That(result.Metadata.GetUnsignedScalar(PixelXDimension), Is.EqualTo(4000));
        Assert.That(result.Metadata.GetRationalScalar(ExposureTime).Numerator, Is.EqualTo(1));
        Assert.That(result.Metadata.GetRationalScalar(ExposureTime).Denominator, Is.EqualTo(125));
        Assert.That(result.Metadata.FindValue(ExifVersion)!.RawBytes, Is.EqualTo(new byte[] { 48, 50, 51, 49 }));
    }

    [Test]
    public void ReadExif_ReturnsEmptyMetadataWhenNoExifPointerExists()
    {
        byte[] bytes = new byte[64];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicDirectory(bytes, 8, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(bytes);
        TiffDirectory parent = TiffDirectoryReader.Read(header.Header, bytes).Directories[0];

        TiffExifReadResult result = TiffCodec.ReadExif(parent, bytes, header.Header);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasExif, Is.False);
        Assert.That(result.Metadata.ValueCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadExif_ReportsInvalidExifPointerTagType()
    {
        TiffDirectory parent = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreatePointerEntry(TiffTagId.ExifIfd, TiffDataType.Short, 1, 32, 4) });

        TiffExifReadResult result = TiffCodec.ReadExif(parent, new byte[64], TiffByteOrder.LittleEndian, TiffFileKind.Classic);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Metadata.HasExif, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ExifIfd));
    }

    [Test]
    public void ReadExif_CapturesNestedInteroperabilityPointer()
    {
        byte[] bytes = new byte[160];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        WriteClassicParentWithOnePointer(bytes, 8, TiffTagId.ExifIfd, 48);
        WriteClassicParentWithOnePointer(bytes, 48, TiffTagId.InteroperabilityIfd, 96);
        WriteClassicDirectory(bytes, 96, TiffTagId.ImageWidth, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(bytes);
        TiffDirectory parent = TiffDirectoryReader.Read(header.Header, bytes).Directories[0];

        TiffExifReadResult result = TiffCodec.ReadExif(parent, bytes, header.Header);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasExif, Is.True);
        Assert.That(result.Metadata.InteroperabilityPointers.Length, Is.EqualTo(1));
        Assert.That(result.Metadata.InteroperabilityPointers[0].Offset, Is.EqualTo(96));
    }

    static void WriteClassicExifDirectory(byte[] bytes, int offset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 5, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, ExposureTime, TiffDataType.Rational, 1, 128);
        WriteClassicEntry(bytes, offset + 14, IsoSpeedRatings, TiffDataType.Short, 1, 200);
        WriteClassicInlineBytesEntry(bytes, offset + 26, ExifVersion, TiffDataType.Undefined, 4, new byte[] { 48, 50, 51, 49 });
        WriteClassicEntry(bytes, offset + 38, DateTimeOriginal, TiffDataType.Ascii, 20, 144);
        WriteClassicEntry(bytes, offset + 50, PixelXDimension, TiffDataType.Long, 1, 4000);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 62, 4), 0, TiffByteOrder.LittleEndian);
    }

    static TiffIfdEntry CreatePointerEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = slotByteCount;
        entry.ValueOffsetBytes = new byte[slotByteCount];
        return entry;
    }

    static void WriteClassicParentWithOnePointer(byte[] bytes, int offset, int pointerTagId, uint pointerOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, pointerTagId, TiffDataType.Long, 1, pointerOffset);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), 0, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicDirectory(byte[] bytes, int offset, int tagId, uint nextOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), 1, TiffByteOrder.LittleEndian);
        WriteClassicEntry(bytes, offset + 2, tagId, TiffDataType.Long, 1, 100);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 14, 4), nextOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicEntry(byte[] bytes, int offset, int tagId, TiffDataType dataType, uint count, uint valueOffset)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)dataType, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), count, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 8, 4), valueOffset, TiffByteOrder.LittleEndian);
    }

    static void WriteClassicInlineBytesEntry(byte[] bytes, int offset, int tagId, TiffDataType dataType, uint count, byte[] valueBytes)
    {
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)tagId, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)dataType, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), count, TiffByteOrder.LittleEndian);
        int index = 0;
        while (index < valueBytes.Length)
        {
            bytes[offset + 8 + index] = valueBytes[index];
            index = index + 1;
        }
    }

    static void WriteAscii(byte[] bytes, int offset, string text)
    {
        byte[] encoded = TiffAscii.EncodeString(text);
        int index = 0;
        while (index < encoded.Length)
        {
            bytes[offset + index] = encoded[index];
            index = index + 1;
        }
    }
}
