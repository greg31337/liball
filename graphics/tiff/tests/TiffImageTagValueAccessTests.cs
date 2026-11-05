using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageTagValueAccessTests
{
    const int ByteTag = 64000;
    const int AsciiTag = 64001;
    const int ShortTag = 64002;
    const int LongTag = 64003;
    const int RationalTag = 64004;
    const int SByteTag = 64005;
    const int UndefinedTag = 64006;
    const int SShortTag = 64007;
    const int SLongTag = 64008;
    const int SRationalTag = 64009;
    const int FloatTag = 64010;
    const int DoubleTag = 64011;
    const int IfdTag = 64012;
    const int Long8Tag = 64013;
    const int SLong8Tag = 64014;
    const int Ifd8Tag = 64015;

    [Test]
    public void BigTiffImage_ReturnsTypedScalarAndArrayValuesForEveryFieldType()
    {
        TiffTagValue[] expected = CreateAllTypeValues();
        ITiffImage image = TiffFile.OpenRead(CreateBigTiffFile(expected)).GetImage(0);

        TiffTagValue[] actual = image.GetTagValues();

        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            TiffTagValue? value = image.GetTagValue(expected[index].TagId);
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.DataType, Is.EqualTo(expected[index].DataType));
            Assert.That(value.Count, Is.EqualTo(expected[index].Count));
            index = index + 1;
        }

        Assert.That(image.GetUnsignedTagScalar(ByteTag), Is.EqualTo(7));
        Assert.That(image.GetUnsignedTagArray(ShortTag), Is.EqualTo(new ulong[] { 3, 4 }));
        Assert.That(image.GetUnsignedTagScalar(LongTag), Is.EqualTo(4000000000));
        Assert.That(image.GetUnsignedTagArray(IfdTag), Is.EqualTo(new ulong[] { 12, 34 }));
        Assert.That(image.GetUnsignedTagScalar(Long8Tag), Is.EqualTo(5000000000));
        Assert.That(image.GetUnsignedTagScalar(Ifd8Tag), Is.EqualTo(6000000000));
        Assert.That(image.GetSignedTagScalar(SByteTag), Is.EqualTo(-2));
        Assert.That(image.GetSignedTagArray(SShortTag), Is.EqualTo(new long[] { -3, 4 }));
        Assert.That(image.GetSignedTagScalar(SLongTag), Is.EqualTo(-100000));
        Assert.That(image.GetSignedTagArray(SLong8Tag), Is.EqualTo(new long[] { -5000000000, 5000000000 }));
        Assert.That(image.GetRationalTagArray(RationalTag)[1].Numerator, Is.EqualTo(3));
        Assert.That(image.GetRationalTagScalar(SRationalTag).Numerator, Is.EqualTo(-1));
        Assert.That(image.GetFloatingTagArray(FloatTag), Is.EqualTo(new double[] { 1.5, -2.5 }));
        Assert.That(image.GetFloatingTagScalar(DoubleTag), Is.EqualTo(3.25));
        Assert.That(image.GetAsciiTagArray(AsciiTag), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(image.GetTagValue(UndefinedTag)!.IsRaw, Is.True);
        Assert.That(image.GetTagValue(UndefinedTag)!.RawBytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void ClassicTiffImage_DecodesClassicFieldTypesInBigEndianStorage()
    {
        TiffTagValue[] allValues = CreateAllTypeValues();
        TiffTagValue[] classicValues = new TiffTagValue[13];
        int index = 0;
        while (index < classicValues.Length)
        {
            classicValues[index] = allValues[index].Copy();
            index = index + 1;
        }

        TiffImage image = TiffFile.OpenRead(CreateClassicTiffFile(classicValues)).GetImage(0);

        Assert.That(image.GetTagValues().Length, Is.EqualTo(13));
        Assert.That(image.GetUnsignedTagArray(ShortTag), Is.EqualTo(new ulong[] { 3, 4 }));
        Assert.That(image.GetSignedTagArray(SShortTag), Is.EqualTo(new long[] { -3, 4 }));
        Assert.That(image.GetRationalTagArray(RationalTag)[0].Denominator, Is.EqualTo(2));
        Assert.That(image.GetFloatingTagArray(FloatTag), Is.EqualTo(new double[] { 1.5, -2.5 }));
        Assert.That(image.GetRawTagBytes(UndefinedTag), Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void TagValueAccess_PreservesUnknownTagsRawBytesAndOriginalDirectoryOrder()
    {
        TiffTagValue[] expected = CreateAllTypeValues();
        byte[][] expectedBytes = EncodeValues(expected, TiffByteOrder.LittleEndian);
        byte[] fileBytes = CreateBigTiffFile(expected);
        SwapBigTiffEntries(fileBytes, 0, 1);
        TiffImage image = TiffFile.OpenRead(fileBytes).GetImage(0);

        TiffIfdEntry[] entries = image.GetTags();
        TiffTagValue[] values = image.GetTagValues();
        byte[][] rawValues = image.GetRawTagBytesInOriginalOrder();

        Assert.That(entries.Length, Is.EqualTo(expected.Length));
        Assert.That(values.Length, Is.EqualTo(entries.Length));
        Assert.That(rawValues.Length, Is.EqualTo(entries.Length));
        int index = 0;
        while (index < entries.Length)
        {
            Assert.That(values[index].TagId, Is.EqualTo(entries[index].TagId));
            int expectedIndex = FindValueIndex(expected, entries[index].TagId);
            Assert.That(expectedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(rawValues[index], Is.EqualTo(expectedBytes[expectedIndex]));
            index = index + 1;
        }

        Assert.That(entries[0].TagId, Is.EqualTo(AsciiTag));
        Assert.That(entries[1].TagId, Is.EqualTo(ByteTag));
        Assert.That(image.GetRawTagBytes(ShortTag), Is.EqualTo(expectedBytes[FindValueIndex(expected, ShortTag)]));
        Assert.That(image.GetTagValue(65000), Is.Null);
        Assert.That(image.GetRawTagBytes(65000), Is.Empty);
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void ExternalTagValue_IsDeferredThenCachedAcrossTypedAndRawAccess()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateAscii(AsciiTag, "a deferred external tag value")
        };
        byte[] bytes = CreateBigTiffFile(values);
        TrackingSource source = new TrackingSource(bytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;
        TiffImage image = TiffApi.Factory.OpenReadSource(source, options).GetImage(0);
        TiffIfdEntry entry = image.GetTag(AsciiTag)!;

        Assert.That(entry.ValueByteCount, Is.GreaterThan(entry.ValueSlotByteCount));
        Assert.That(image.GetIsTagValueLoaded(AsciiTag), Is.False);
        Assert.That(source.CountReadsAt((long)entry.ValueOffset), Is.EqualTo(0));

        string text = image.GetAsciiTagScalar(AsciiTag);

        Assert.That(text, Is.EqualTo("a deferred external tag value"));
        Assert.That(image.GetIsTagValueLoaded(AsciiTag), Is.True);
        Assert.That(source.CountReadsAt((long)entry.ValueOffset), Is.EqualTo(1));

        byte[] raw = image.GetRawTagBytes(AsciiTag);
        TiffTagValue? typed = image.GetTagValue(AsciiTag);

        Assert.That(raw.Length, Is.EqualTo(entry.ValueByteCount));
        Assert.That(typed!.Text, Is.EqualTo(text));
        Assert.That(source.CountReadsAt((long)entry.ValueOffset), Is.EqualTo(1));
    }

    [Test]
    public void DefaultAndNestedIfdAccess_AreAvailableDirectlyFromImage()
    {
        TiffIfdEntry[] parentEntries = new TiffIfdEntry[]
        {
            CreateClassicEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100)
        };
        TiffTagValue[] exifValues = new TiffTagValue[]
        {
            TiffTagValue.CreateAscii(36867, "2026:08:26 10:20:30")
        };
        int byteCount = TiffExifMetadataWriter.CalculateClassicWriteByteCount(parentEntries, exifValues);
        byte[] bytes = new byte[8 + byteCount];
        TiffHeader.WriteClassic(bytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffExifWriteResult writeResult = TiffCodec.WriteClassicExif(bytes, parentEntries, exifValues, TiffByteOrder.LittleEndian, 8, 0);
        TiffImage image = TiffFile.OpenRead(bytes).GetImage(0);

        TiffTagDefaultResult compression = image.GetTagDefaulted(TiffTagId.Compression);
        TiffIfdPointerReadResult pointers = image.ReadIfdPointers();

        Assert.That(writeResult.Success, Is.True);
        Assert.That(compression.Found, Is.True);
        Assert.That(compression.UsedDefault, Is.True);
        Assert.That(compression.Value.UnsignedValues, Is.EqualTo(new ulong[] { (ulong)TiffCompression.None }));
        Assert.That(pointers.Success, Is.True);
        Assert.That(pointers.Pointers.Length, Is.EqualTo(1));
        Assert.That(pointers.Pointers[0].TagId, Is.EqualTo(TiffTagId.ExifIfd));
        Assert.That(pointers.Pointers[0].Offset, Is.EqualTo(writeResult.ExifOffset));
        Assert.That(pointers.Pointers[0].Directories.Length, Is.EqualTo(1));
        Assert.That(pointers.Pointers[0].Directories[0].Entries[0].TagId, Is.EqualTo(36867));
    }

    static TiffTagValue[] CreateAllTypeValues()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateAsciiArray(AsciiTag, new string[] { "one", "two" }),
            TiffTagValue.CreateUnsignedScalar(ByteTag, TiffDataType.Byte, 7),
            TiffTagValue.CreateUnsignedArray(ShortTag, TiffDataType.Short, new ulong[] { 3, 4 }),
            TiffTagValue.CreateUnsignedScalar(LongTag, TiffDataType.Long, 4000000000),
            TiffTagValue.CreateRationalArray(RationalTag, TiffDataType.Rational, new TiffRational[] { TiffRational.Create(1, 2), TiffRational.Create(3, 4) }),
            TiffTagValue.CreateSignedScalar(SByteTag, TiffDataType.SByte, -2),
            TiffTagValue.CreateRaw(UndefinedTag, TiffDataType.Undefined, 3, new byte[] { 9, 8, 7 }),
            TiffTagValue.CreateSignedArray(SShortTag, TiffDataType.SShort, new long[] { -3, 4 }),
            TiffTagValue.CreateSignedScalar(SLongTag, TiffDataType.SLong, -100000),
            TiffTagValue.CreateRationalScalar(SRationalTag, TiffDataType.SRational, -1, 2),
            TiffTagValue.CreateFloatingArray(FloatTag, TiffDataType.Float, new double[] { 1.5, -2.5 }),
            TiffTagValue.CreateFloatingScalar(DoubleTag, TiffDataType.Double, 3.25),
            TiffTagValue.CreateUnsignedArray(IfdTag, TiffDataType.Ifd, new ulong[] { 12, 34 }),
            TiffTagValue.CreateUnsignedScalar(Long8Tag, TiffDataType.Long8, 5000000000),
            TiffTagValue.CreateSignedArray(SLong8Tag, TiffDataType.SLong8, new long[] { -5000000000, 5000000000 }),
            TiffTagValue.CreateUnsignedScalar(Ifd8Tag, TiffDataType.Ifd8, 6000000000)
        };
    }

    static byte[] CreateClassicTiffFile(TiffTagValue[] values)
    {
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        byte[][] valueBytes = EncodeValues(values, TiffByteOrder.BigEndian);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.BigEndian, 8);
        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, valueBytes, TiffByteOrder.BigEndian, 8, 0);
        Assert.That(diagnostics.HasErrors(), Is.False);
        return fileBytes;
    }

    static byte[] CreateBigTiffFile(TiffTagValue[] values)
    {
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        byte[][] valueBytes = EncodeValues(values, TiffByteOrder.LittleEndian);
        int layoutByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, valueBytes);
        byte[] fileBytes = new byte[16 + layoutByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);
        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffWithValueBytes(fileBytes.AsSpan(16, layoutByteCount), entries, valueBytes, TiffByteOrder.LittleEndian, 16, 0);
        Assert.That(diagnostics.HasErrors(), Is.False);
        return fileBytes;
    }

    static byte[][] EncodeValues(TiffTagValue[] values, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[][] bytes = TiffExifMetadataWriter.EncodeValues(values, byteOrder, diagnostics);
        Assert.That(diagnostics.HasErrors(), Is.False);
        return bytes;
    }

    static int FindValueIndex(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].TagId == tagId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static void SwapBigTiffEntries(byte[] fileBytes, int firstIndex, int secondIndex)
    {
        int firstOffset = 24 + firstIndex * 20;
        int secondOffset = 24 + secondIndex * 20;
        byte[] temporary = new byte[20];
        fileBytes.AsSpan(firstOffset, 20).CopyTo(temporary);
        fileBytes.AsSpan(secondOffset, 20).CopyTo(fileBytes.AsSpan(firstOffset, 20));
        temporary.CopyTo(fileBytes, secondOffset);
    }

    static TiffIfdEntry CreateClassicEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
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

    public class TrackingSource : ITiffRandomAccessSource
    {
        public byte[] Bytes;
        public bool IsOpen;
        public List<long> ReadOffsets;

        public TrackingSource(byte[] bytes)
        {
            Bytes = bytes;
            IsOpen = true;
            ReadOffsets = new List<long>();
        }

        public bool GetIsOpen()
        {
            return IsOpen;
        }

        public long GetLength()
        {
            return Bytes.Length;
        }

        public int Read(long offset, Span<byte> destination)
        {
            ReadOffsets.Add(offset);
            int count = destination.Length;
            long remaining = Bytes.Length - offset;
            if (remaining < count)
            {
                count = (int)remaining;
            }

            Bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public int CountReadsAt(long offset)
        {
            int count = 0;
            int index = 0;
            while (index < ReadOffsets.Count)
            {
                if (ReadOffsets[index] == offset)
                {
                    count = count + 1;
                }

                index = index + 1;
            }

            return count;
        }
    }
}
