using raw;

namespace raw.Tests;

public class PublicMetadataUtilityTests
{
    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void TypedTagOperationsRoundTripEveryTiffFieldTypeExactly(RawByteOrder byteOrder)
    {
        IRawCodec codec = new RawCodecService();
        RawRational unsignedRational = codec.CreateUnsignedRational(uint.MaxValue, 4000000000U);
        RawRational signedRational = codec.CreateSignedRational(int.MinValue, int.MaxValue);
        float singleNan = BitConverter.UInt32BitsToSingle(0x7FC12345U);
        double doubleNan = BitConverter.UInt64BitsToDouble(0x7FF8123456789ABCUL);

        RawTagValue bytes = codec.CreateByteTagValue(new byte[] { 0, 255 });
        RawTagValue ascii = codec.CreateAsciiTagValue("Camera");
        RawTagValue shorts = codec.CreateUnsignedShortTagValue(new ushort[] { 0, ushort.MaxValue }, byteOrder);
        RawTagValue longs = codec.CreateUnsignedLongTagValue(new uint[] { 0, uint.MaxValue }, byteOrder);
        RawTagValue rationals = codec.CreateUnsignedRationalTagValue(new RawRational[] { unsignedRational }, byteOrder);
        RawTagValue signedBytes = codec.CreateSignedByteTagValue(new sbyte[] { sbyte.MinValue, -1, sbyte.MaxValue });
        RawTagValue undefined = codec.CreateUndefinedTagValue(new byte[] { 0, 127, 128, 255 });
        RawTagValue signedShorts = codec.CreateSignedShortTagValue(new short[] { short.MinValue, short.MaxValue }, byteOrder);
        RawTagValue signedLongs = codec.CreateSignedLongTagValue(new int[] { int.MinValue, int.MaxValue }, byteOrder);
        RawTagValue signedRationals = codec.CreateSignedRationalTagValue(new RawRational[] { signedRational }, byteOrder);
        RawTagValue singles = codec.CreateFloatTagValue(new float[] { -12.5F, singleNan }, byteOrder);
        RawTagValue doubles = codec.CreateDoubleTagValue(new double[] { 1.5, doubleNan }, byteOrder);
        RawTagValue ifds = codec.CreateIfdTagValue(new uint[] { 0, uint.MaxValue }, byteOrder);
        RawTagValue long8s = codec.CreateUnsignedLong8TagValue(new ulong[] { 0, ulong.MaxValue }, byteOrder);
        RawTagValue signedLong8s = codec.CreateSignedLong8TagValue(new long[] { long.MinValue, long.MaxValue }, byteOrder);
        RawTagValue ifd8s = codec.CreateIfd8TagValue(new ulong[] { 0, ulong.MaxValue }, byteOrder);
        RawTagValue utf8 = codec.CreateUtf8TagValue("Cámara 東京");
        RawTagValue emptySignedRationals = codec.CreateSignedRationalTagValue(Array.Empty<RawRational>(), byteOrder);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetByteTagValues(bytes), Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(codec.GetAsciiTagText(ascii).Text, Is.EqualTo("Camera"));
            Assert.That(codec.GetUnsignedShortTagValues(shorts), Is.EqualTo(new ushort[] { 0, ushort.MaxValue }));
            Assert.That(codec.GetUnsignedLongTagValues(longs), Is.EqualTo(new uint[] { 0, uint.MaxValue }));
            AssertRational(codec.GetUnsignedRationalTagValues(rationals)[0], uint.MaxValue, 4000000000L, false);
            Assert.That(codec.GetSignedByteTagValues(signedBytes), Is.EqualTo(new sbyte[] { sbyte.MinValue, -1, sbyte.MaxValue }));
            Assert.That(codec.GetUndefinedTagBytes(undefined), Is.EqualTo(new byte[] { 0, 127, 128, 255 }));
            Assert.That(codec.GetSignedShortTagValues(signedShorts), Is.EqualTo(new short[] { short.MinValue, short.MaxValue }));
            Assert.That(codec.GetSignedLongTagValues(signedLongs), Is.EqualTo(new int[] { int.MinValue, int.MaxValue }));
            AssertRational(codec.GetSignedRationalTagValues(signedRationals)[0], int.MinValue, int.MaxValue, true);
            Assert.That(BitConverter.SingleToUInt32Bits(codec.GetFloatTagValues(singles)[1]), Is.EqualTo(0x7FC12345U));
            Assert.That(BitConverter.DoubleToUInt64Bits(codec.GetDoubleTagValues(doubles)[1]), Is.EqualTo(0x7FF8123456789ABCUL));
            Assert.That(codec.GetIfdTagValues(ifds), Is.EqualTo(new uint[] { 0, uint.MaxValue }));
            Assert.That(codec.GetUnsignedLong8TagValues(long8s), Is.EqualTo(new ulong[] { 0, ulong.MaxValue }));
            Assert.That(codec.GetSignedLong8TagValues(signedLong8s), Is.EqualTo(new long[] { long.MinValue, long.MaxValue }));
            Assert.That(codec.GetIfd8TagValues(ifd8s), Is.EqualTo(new ulong[] { 0, ulong.MaxValue }));
            Assert.That(codec.GetUtf8TagText(utf8).Text, Is.EqualTo("Cámara 東京"));
            Assert.That(emptySignedRationals.Type, Is.EqualTo(RawTagType.SignedRational));
            Assert.That(emptySignedRationals.Count, Is.Zero);
            Assert.Throws<RawFormatException>(() => codec.GetUnsignedLongTagValues(ifds));
            Assert.Throws<RawFormatException>(() => codec.GetUnsignedRationalTagValues(signedRationals));
        });
    }

    [Test]
    public void TextAndRawByteOperationsReturnIndependentExactCopies()
    {
        IRawCodec codec = new RawCodecService();
        byte[] source = new byte[] { 1, 2, 3 };
        RawTagValue undefined = codec.CreateUndefinedTagValue(source);
        source[0] = 99;
        byte[] firstCopy = codec.CopyTagValueBytes(undefined);
        firstCopy[1] = 88;
        RawTagValue prefixed = codec.CreateExifPrefixedTextTagValue("Привет", RawTextEncoding.Unicode, RawByteOrder.BigEndian);
        RawTextValue decoded = codec.GetExifPrefixedTagText(prefixed);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetUndefinedTagBytes(undefined), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(codec.CopyTagValueBytes(undefined), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(decoded.Text, Is.EqualTo("Привет"));
            Assert.That(decoded.Encoding, Is.EqualTo(RawTextEncoding.Unicode));
            Assert.That(decoded.Prefix, Is.EqualTo(new byte[] { 85, 78, 73, 67, 79, 68, 69, 0 }));
            Assert.That(prefixed.Type, Is.EqualTo(RawTagType.Undefined));
            Assert.Throws<RawFormatException>(() => codec.CreateAsciiTagValue("München"));
            Assert.Throws<RawFormatException>(() => codec.GetAsciiTagText(prefixed));
        });
    }

    [Test]
    public void MetadataLookupUsesStablePathKindIdentifierAndGraphOrder()
    {
        IRawCodec codec = new RawCodecService();
        RawTag rootTag = new RawTag(271, "Make", "IFD0", 10, true, codec.CreateAsciiTagValue("Root"));
        RawTag exifTag = new RawTag(42035, "Make", "IFD0/Exif", 20, true, codec.CreateAsciiTagValue("Exif"));
        RawIfd root = new RawIfd("IFD0", RawIfdKind.Image, 8, new RawTag[] { rootTag });
        RawIfd exif = new RawIfd("IFD0/Exif", RawIfdKind.Exif, 40, new RawTag[] { exifTag });
        RawMetadata metadata = new RawMetadata(new RawIfd[] { root, exif });

        Assert.Multiple(() =>
        {
            Assert.That(codec.FindMetadataIfdByPath(metadata, "IFD0/Exif"), Is.SameAs(exif));
            Assert.That(codec.FindMetadataTagByPathAndId(metadata, "IFD0", 271), Is.SameAs(rootTag));
            Assert.That(codec.FindMetadataTagByKindAndId(metadata, RawIfdKind.Exif, 42035), Is.SameAs(exifTag));
            Assert.That(codec.FindMetadataTagByName(metadata, "Make"), Is.SameAs(rootTag));
            Assert.That(codec.FindMetadataTagByPathAndId(metadata, "IFD0", 999), Is.Null);
            Assert.That(codec.FindMetadataIfdByPath(metadata, "IFD9"), Is.Null);
        });
    }

    [Test]
    public void ExifTimestampOperationsConvertAndEncodeExactComponents()
    {
        IRawCodec codec = new RawCodecService();
        DateTimeOffset source = new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(5.5)).AddTicks(7654321);
        RawExifTimestamp timestamp = codec.CreateExifTimestampFromDateTimeOffset(source, 6);
        DateTimeOffset converted = codec.ConvertExifTimestampToDateTimeOffset(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp.DateTimeText, Is.EqualTo("2024:02:29 23:59:58"));
            Assert.That(timestamp.OffsetText, Is.EqualTo("+05:30"));
            Assert.That(timestamp.Subseconds, Is.EqualTo("765432"));
            Assert.That(converted, Is.EqualTo(new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(5.5)).AddTicks(7654320)));
            Assert.That(codec.GetAsciiTagText(codec.CreateExifDateTimeTagValue(timestamp)).Text, Is.EqualTo("2024:02:29 23:59:58"));
            Assert.That(codec.GetAsciiTagText(codec.CreateExifOffsetTagValue(timestamp)).Text, Is.EqualTo("+05:30"));
            Assert.That(codec.GetAsciiTagText(codec.CreateExifSubsecondsTagValue(timestamp)).Text, Is.EqualTo("765432"));
            Assert.Throws<RawFormatException>(() => codec.CreateExifTimestampFromDateTimeOffset(source, 8));
            Assert.Throws<RawFormatException>(() => codec.ConvertExifTimestampToDateTimeOffset(new RawExifTimestamp("2024:02:29 23:59:58", "", "1")));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void GpsTimestampTagFormsRetainExactUtcDateAndRationalTime(RawByteOrder byteOrder)
    {
        IRawCodec codec = new RawCodecService();
        RawTag date = new RawTag(29, "GPSDateStamp", "IFD0/GPS", 100, true, codec.CreateAsciiTagValue("2026:08:26"));
        RawTag time = new RawTag(7, "GPSTimeStamp", "IFD0/GPS", 120, true, codec.CreateUnsignedRationalTagValue(new RawRational[]
        {
            codec.CreateUnsignedRational(23, 1),
            codec.CreateUnsignedRational(59, 1),
            codec.CreateUnsignedRational(119, 2)
        }, byteOrder));
        RawIfd gps = new RawIfd("IFD0/GPS", RawIfdKind.Gps, 80, new RawTag[] { time, date });
        RawMetadata metadata = new RawMetadata(new RawIfd[] { gps });
        RawTag foundDate = codec.FindMetadataTagByKindAndId(metadata, RawIfdKind.Gps, 29) ?? throw new InvalidOperationException();
        RawTag foundTime = codec.FindMetadataTagByPathAndId(metadata, "IFD0/GPS", 7) ?? throw new InvalidOperationException();
        RawRational[] components = codec.GetUnsignedRationalTagValues(foundTime.Value);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetAsciiTagText(foundDate.Value).Text, Is.EqualTo("2026:08:26"));
            AssertRational(components[0], 23, 1, false);
            AssertRational(components[1], 59, 1, false);
            AssertRational(components[2], 119, 2, false);
            Assert.That(foundTime.Value.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(foundDate.IfdPath, Is.EqualTo("IFD0/GPS"));
        });
    }

    private static void AssertRational(RawRational actual, long numerator, long denominator, bool signed)
    {
        Assert.That(actual.Numerator, Is.EqualTo(numerator));
        Assert.That(actual.Denominator, Is.EqualTo(denominator));
        Assert.That(actual.Signed, Is.EqualTo(signed));
    }
}
