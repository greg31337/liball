using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class TiffValueDecoderTests
{
    [Test]
    public void Parse_ClassicTiffDecodesInlineBaselineScalars()
    {
        List<TiffValueTestEntry> entries = new List<TiffValueTestEntry>();
        entries.Add(InlineEntry(1, TiffValueDecoder.TypeByte, 1, new byte[] { 7 }));
        entries.Add(InlineEntry(2, TiffValueDecoder.TypeAscii, 4, new byte[] { 65, 66, 67, 0 }));
        entries.Add(InlineEntry(3, TiffValueDecoder.TypeShort, 1, LittleUnsigned16(300)));
        entries.Add(InlineEntry(4, TiffValueDecoder.TypeLong, 1, LittleUnsigned32(16909060)));
        entries.Add(InlineEntry(5, TiffValueDecoder.TypeSignedByte, 1, new byte[] { 254 }));
        entries.Add(InlineEntry(6, TiffValueDecoder.TypeSignedShort, 1, LittleSigned16(-123)));
        entries.Add(InlineEntry(7, TiffValueDecoder.TypeSignedLong, 1, LittleSigned32(-123456)));
        entries.Add(InlineEntry(8, TiffValueDecoder.TypeFloat, 1, new byte[] { 0, 0, 192, 63 }));

        ImageMetadataDocument document = ParseTiff(CreateClassicTiff(true, entries), ImageFormatKind.Tiff);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag1").Value.UnsignedIntegerValue, Is.EqualTo(7));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag2").Value.Text, Is.EqualTo("ABC"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag3").Value.UnsignedIntegerValue, Is.EqualTo(300));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag4").Value.UnsignedIntegerValue, Is.EqualTo(16909060));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag5").Value.IntegerValue, Is.EqualTo(-2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag6").Value.IntegerValue, Is.EqualTo(-123));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag7").Value.IntegerValue, Is.EqualTo(-123456));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag8").Value.DoubleValue, Is.EqualTo(1.5));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag8").RawValue, Has.Length.EqualTo(4));
    }

    [Test]
    public void Parse_ClassicTiffDecodesOffsetBackedValuesAndArrays()
    {
        List<TiffValueTestEntry> entries = new List<TiffValueTestEntry>();
        entries.Add(ExternalEntry(10, TiffValueDecoder.TypeRational, 1, Combine(LittleUnsigned32(300), LittleUnsigned32(1))));
        entries.Add(ExternalEntry(11, TiffValueDecoder.TypeSignedRational, 1, Combine(LittleSigned32(-1), LittleSigned32(2))));
        entries.Add(ExternalEntry(12, TiffValueDecoder.TypeDouble, 1, new byte[] { 0, 0, 0, 0, 0, 0, 244, 63 }));
        entries.Add(ExternalEntry(13, TiffValueDecoder.TypeShort, 3, Combine(LittleUnsigned16(10), LittleUnsigned16(20), LittleUnsigned16(30))));
        entries.Add(ExternalEntry(14, TiffValueDecoder.TypeByte, 5, new byte[] { 1, 2, 3, 4, 5 }));
        entries.Add(ExternalEntry(15, TiffValueDecoder.TypeUndefined, 5, new byte[] { 9, 8, 7, 6, 5 }));
        entries.Add(InlineEntry(16, TiffValueDecoder.TypeSignedShort, 2, Combine(LittleSigned16(-2), LittleSigned16(300))));
        entries.Add(ExternalEntry(17, TiffValueDecoder.TypeRational, 2, Combine(Combine(LittleUnsigned32(1), LittleUnsigned32(2)), Combine(LittleUnsigned32(3), LittleUnsigned32(4)))));
        entries.Add(ExternalEntry(18, TiffValueDecoder.TypeSignedRational, 2, Combine(Combine(LittleSigned32(-1), LittleSigned32(2)), Combine(LittleSigned32(3), LittleSigned32(-4)))));
        entries.Add(ExternalEntry(19, TiffValueDecoder.TypeFloat, 2, new byte[] { 0, 0, 192, 63, 0, 0, 16, 192 }));
        entries.Add(ExternalEntry(20, TiffValueDecoder.TypeDouble, 2, new byte[] { 0, 0, 0, 0, 0, 0, 244, 63, 0, 0, 0, 0, 0, 0, 4, 192 }));

        ImageMetadataDocument document = ParseTiff(CreateClassicTiff(true, entries), ImageFormatKind.Tiff);

        MetadataTag rational = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag10");
        MetadataTag signedRational = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag11");
        MetadataTag doubleTag = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag12");
        MetadataTag shortArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag13");
        MetadataTag byteArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag14");
        MetadataTag undefined = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag15");
        MetadataTag signedArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag16");
        MetadataTag rationalArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag17");
        MetadataTag signedRationalArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag18");
        MetadataTag floatArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag19");
        MetadataTag doubleArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag20");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(rational.Value.Kind, Is.EqualTo(MetadataValueKind.Rational));
        Assert.That(rational.Value.Numerator, Is.EqualTo(300));
        Assert.That(rational.Value.Denominator, Is.EqualTo(1));
        Assert.That(signedRational.Value.Kind, Is.EqualTo(MetadataValueKind.SignedRational));
        Assert.That(signedRational.Value.Numerator, Is.EqualTo(-1));
        Assert.That(signedRational.Value.Denominator, Is.EqualTo(2));
        Assert.That(doubleTag.Value.DoubleValue, Is.EqualTo(1.25));
        Assert.That(shortArray.Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(shortArray.Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 10, 20, 30 }));
        Assert.That(byteArray.Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(byteArray.Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 1, 2, 3, 4, 5 }));
        Assert.That(undefined.Value.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(undefined.Value.Bytes, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
        Assert.That(undefined.RawValue, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
        Assert.That(signedArray.Value.Kind, Is.EqualTo(MetadataValueKind.IntegerArray));
        Assert.That(signedArray.Value.IntegerValues, Is.EqualTo(new long[] { -2, 300 }));
        Assert.That(rationalArray.Value.Kind, Is.EqualTo(MetadataValueKind.RationalArray));
        Assert.That(rationalArray.Value.Numerators, Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(rationalArray.Value.Denominators, Is.EqualTo(new long[] { 2, 4 }));
        Assert.That(signedRationalArray.Value.Kind, Is.EqualTo(MetadataValueKind.SignedRationalArray));
        Assert.That(signedRationalArray.Value.Numerators, Is.EqualTo(new long[] { -1, 3 }));
        Assert.That(signedRationalArray.Value.Denominators, Is.EqualTo(new long[] { 2, -4 }));
        Assert.That(floatArray.Value.Kind, Is.EqualTo(MetadataValueKind.DoubleArray));
        Assert.That(floatArray.Value.DoubleValues, Is.EqualTo(new double[] { 1.5, -2.25 }));
        Assert.That(doubleArray.Value.Kind, Is.EqualTo(MetadataValueKind.DoubleArray));
        Assert.That(doubleArray.Value.DoubleValues, Is.EqualTo(new double[] { 1.25, -2.5 }));
    }

    [Test]
    public void Parse_BigEndianClassicTiffDecodesValues()
    {
        List<TiffValueTestEntry> entries = new List<TiffValueTestEntry>();
        entries.Add(InlineEntry(20, TiffValueDecoder.TypeShort, 1, new byte[] { 18, 52 }));
        entries.Add(InlineEntry(21, TiffValueDecoder.TypeLong, 1, new byte[] { 1, 2, 3, 4 }));

        ImageMetadataDocument document = ParseTiff(CreateClassicTiff(false, entries), ImageFormatKind.Tiff);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag20").Value.UnsignedIntegerValue, Is.EqualTo(4660));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag21").Value.UnsignedIntegerValue, Is.EqualTo(16909060));
    }

    [Test]
    public void Parse_BigTiffDecodesEightByteInlineValues()
    {
        List<TiffValueTestEntry> entries = new List<TiffValueTestEntry>();
        entries.Add(InlineEntry(30, TiffValueDecoder.TypeLong, 2, Combine(LittleUnsigned32(100), LittleUnsigned32(200))));
        entries.Add(InlineEntry(31, TiffValueDecoder.TypeDouble, 1, new byte[] { 0, 0, 0, 0, 0, 0, 4, 64 }));

        ImageMetadataDocument document = ParseTiff(CreateBigTiff(entries), ImageFormatKind.BigTiff);

        MetadataTag longArray = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag30");
        MetadataTag doubleTag = RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.Tag31");

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(longArray.Value.Kind, Is.EqualTo(MetadataValueKind.UnsignedIntegerArray));
        Assert.That(longArray.Value.UnsignedIntegerValues, Is.EqualTo(new ulong[] { 100, 200 }));
        Assert.That(doubleTag.Value.DoubleValue, Is.EqualTo(2.5));
        Assert.That(doubleTag.RawValue, Has.Length.EqualTo(8));
    }

    [Test]
    public void Parse_ExternalValueOutsideStreamAddsDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            40, 0, 3, 0, 3, 0, 0, 0, 100, 0, 0, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = ParseTiff(bytes, ImageFormatKind.Tiff);

        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("tiff.tag.value.truncated"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tiff.IFD0.Tag40"), Is.Null);
    }

    public static TiffValueTestEntry InlineEntry(int tagId, int typeId, ulong count, byte[] inlineValue)
    {
        TiffValueTestEntry entry = new TiffValueTestEntry();
        entry.TagId = tagId;
        entry.TypeId = typeId;
        entry.Count = count;
        entry.InlineValue = inlineValue;
        entry.ExternalValue = new byte[0];
        return entry;
    }

    public static TiffValueTestEntry ExternalEntry(int tagId, int typeId, ulong count, byte[] externalValue)
    {
        TiffValueTestEntry entry = new TiffValueTestEntry();
        entry.TagId = tagId;
        entry.TypeId = typeId;
        entry.Count = count;
        entry.InlineValue = new byte[0];
        entry.ExternalValue = externalValue;
        return entry;
    }

    public static byte[] CreateClassicTiff(bool littleEndian, List<TiffValueTestEntry> entries)
    {
        List<byte> bytes = new List<byte>();
        int dataOffset = 8 + 2 + entries.Count * 12 + 4;
        AssignExternalOffsets(entries, dataOffset);

        if (littleEndian)
        {
            bytes.Add(73);
            bytes.Add(73);
        }
        else
        {
            bytes.Add(77);
            bytes.Add(77);
        }

        AddUnsigned16(bytes, 42, littleEndian);
        AddUnsigned32(bytes, 8, littleEndian);
        AddUnsigned16(bytes, entries.Count, littleEndian);
        AddClassicEntries(bytes, entries, littleEndian);
        AddUnsigned32(bytes, 0, littleEndian);
        AddExternalData(bytes, entries);
        return bytes.ToArray();
    }

    public static byte[] CreateBigTiff(List<TiffValueTestEntry> entries)
    {
        List<byte> bytes = new List<byte>();
        int dataOffset = 16 + 8 + entries.Count * 20 + 8;
        AssignExternalOffsets(entries, dataOffset);
        bytes.Add(73);
        bytes.Add(73);
        AddUnsigned16(bytes, 43, true);
        AddUnsigned16(bytes, 8, true);
        AddUnsigned16(bytes, 0, true);
        AddUnsigned64(bytes, 16, true);
        AddUnsigned64(bytes, (ulong)entries.Count, true);
        AddBigTiffEntries(bytes, entries);
        AddUnsigned64(bytes, 0, true);
        AddExternalData(bytes, entries);
        return bytes.ToArray();
    }

    public static void AssignExternalOffsets(List<TiffValueTestEntry> entries, int firstOffset)
    {
        int offset = firstOffset;
        foreach (TiffValueTestEntry entry in entries)
        {
            if (entry.ExternalValue != null && entry.ExternalValue.Length > 0)
            {
                entry.ExternalOffset = offset;
                offset = offset + entry.ExternalValue.Length;
            }
        }
    }

    public static void AddClassicEntries(List<byte> bytes, List<TiffValueTestEntry> entries, bool littleEndian)
    {
        foreach (TiffValueTestEntry entry in entries)
        {
            AddUnsigned16(bytes, entry.TagId, littleEndian);
            AddUnsigned16(bytes, entry.TypeId, littleEndian);
            AddUnsigned32(bytes, (uint)entry.Count, littleEndian);
            if (entry.ExternalValue != null && entry.ExternalValue.Length > 0)
            {
                AddUnsigned32(bytes, (uint)entry.ExternalOffset, littleEndian);
            }
            else
            {
                AddFixedBytes(bytes, entry.InlineValue, 4);
            }
        }
    }

    public static void AddBigTiffEntries(List<byte> bytes, List<TiffValueTestEntry> entries)
    {
        foreach (TiffValueTestEntry entry in entries)
        {
            AddUnsigned16(bytes, entry.TagId, true);
            AddUnsigned16(bytes, entry.TypeId, true);
            AddUnsigned64(bytes, entry.Count, true);
            if (entry.ExternalValue != null && entry.ExternalValue.Length > 0)
            {
                AddUnsigned64(bytes, (ulong)entry.ExternalOffset, true);
            }
            else
            {
                AddFixedBytes(bytes, entry.InlineValue, 8);
            }
        }
    }

    public static void AddExternalData(List<byte> bytes, List<TiffValueTestEntry> entries)
    {
        foreach (TiffValueTestEntry entry in entries)
        {
            if (entry.ExternalValue != null && entry.ExternalValue.Length > 0)
            {
                AddFixedBytes(bytes, entry.ExternalValue, entry.ExternalValue.Length);
            }
        }
    }

    public static byte[] Combine(byte[] first, byte[] second)
    {
        List<byte> bytes = new List<byte>();
        AddFixedBytes(bytes, first, first.Length);
        AddFixedBytes(bytes, second, second.Length);
        return bytes.ToArray();
    }

    public static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        List<byte> bytes = new List<byte>();
        AddFixedBytes(bytes, first, first.Length);
        AddFixedBytes(bytes, second, second.Length);
        AddFixedBytes(bytes, third, third.Length);
        return bytes.ToArray();
    }

    public static byte[] LittleUnsigned16(int value)
    {
        return new byte[] { (byte)(value & 255), (byte)((value >> 8) & 255) };
    }

    public static byte[] LittleSigned16(short value)
    {
        return LittleUnsigned16((ushort)value);
    }

    public static byte[] LittleUnsigned32(uint value)
    {
        return new byte[] { (byte)(value & 255), (byte)((value >> 8) & 255), (byte)((value >> 16) & 255), (byte)((value >> 24) & 255) };
    }

    public static byte[] LittleSigned32(int value)
    {
        return LittleUnsigned32((uint)value);
    }

    public static void AddUnsigned16(List<byte> bytes, int value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddUnsigned32(List<byte> bytes, uint value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 24) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 24) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddUnsigned64(List<byte> bytes, ulong value, bool littleEndian)
    {
        if (littleEndian)
        {
            bytes.Add((byte)(value & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 24) & 255));
            bytes.Add((byte)((value >> 32) & 255));
            bytes.Add((byte)((value >> 40) & 255));
            bytes.Add((byte)((value >> 48) & 255));
            bytes.Add((byte)((value >> 56) & 255));
        }
        else
        {
            bytes.Add((byte)((value >> 56) & 255));
            bytes.Add((byte)((value >> 48) & 255));
            bytes.Add((byte)((value >> 40) & 255));
            bytes.Add((byte)((value >> 32) & 255));
            bytes.Add((byte)((value >> 24) & 255));
            bytes.Add((byte)((value >> 16) & 255));
            bytes.Add((byte)((value >> 8) & 255));
            bytes.Add((byte)(value & 255));
        }
    }

    public static void AddFixedBytes(List<byte> bytes, byte[] data, int width)
    {
        int index = 0;
        while (index < width)
        {
            if (data != null && index < data.Length)
            {
                bytes.Add(data[index]);
            }
            else
            {
                bytes.Add(0);
            }

            index = index + 1;
        }
    }

    public static ImageMetadataDocument ParseTiff(byte[] bytes, ImageFormatKind kind)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), new ImageMetadataReaderOptions());
    }
}

public class TiffValueTestEntry
{
    public int TagId;
    public int TypeId;
    public ulong Count;
    public byte[] InlineValue;
    public byte[] ExternalValue;
    public int ExternalOffset;

    public TiffValueTestEntry()
    {
        TagId = 0;
        TypeId = 0;
        Count = 0;
        InlineValue = new byte[0];
        ExternalValue = new byte[0];
        ExternalOffset = 0;
    }
}
