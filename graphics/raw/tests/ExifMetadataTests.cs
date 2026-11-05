using raw;
using System.Text;

namespace raw.Tests;

public class ExifMetadataTests
{
    [Test]
    public void Exif31GpsAndInteropNamesAreContextSensitiveAndStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 37511), Is.EqualTo("LearningOptOutIn"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 41997), Is.EqualTo("DevelopmentType"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 41998), Is.EqualTo("DevelopmentTypeDescription"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 41999), Is.EqualTo("DistortionCorrection"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 42000), Is.EqualTo("ChromaticAberrationCorrection"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 42001), Is.EqualTo("ShadingCorrection"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Exif, 42002), Is.EqualTo("NoiseReduction"));
            Assert.That(Enumerable.Range(0, 32).All(tag => RawExifTagNames.IsKnown(RawIfdKind.Gps, (ushort)tag)), Is.True);
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Gps, 31), Is.EqualTo("GPSHPositioningError"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Interoperability, 1), Is.EqualTo("InteroperabilityIndex"));
            Assert.That(RawExifTagNames.GetName(RawIfdKind.Image, 1), Is.EqualTo("Tag0x0001"));
            Assert.That(RawExifTagNames.IsKnown(RawIfdKind.Image, 1), Is.False);
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void TypedValuesRoundTripEveryNumericFamilyAndExactBytes(RawByteOrder byteOrder)
    {
        RawTagValue bytes = RawTagValue.CreateByte(new byte[] { 0, 255 });
        RawTagValue signedBytes = RawTagValue.CreateSignedByte(new sbyte[] { -128, -1, 127 });
        RawTagValue shorts = RawTagValue.CreateUInt16(new ushort[] { 0, 0xFEDC }, byteOrder);
        RawTagValue signedShorts = RawTagValue.CreateInt16(new short[] { short.MinValue, short.MaxValue }, byteOrder);
        RawTagValue longs = RawTagValue.CreateUInt32(new uint[] { 0, uint.MaxValue }, byteOrder);
        RawTagValue ifd = RawTagValue.CreateIfd(new uint[] { 42 }, byteOrder);
        RawTagValue signedLongs = RawTagValue.CreateInt32(new int[] { int.MinValue, int.MaxValue }, byteOrder);
        RawTagValue long8 = RawTagValue.CreateUInt64(new ulong[] { 0, ulong.MaxValue }, byteOrder);
        RawTagValue ifd8 = RawTagValue.CreateIfd8(new ulong[] { 99 }, byteOrder);
        RawTagValue signedLong8 = RawTagValue.CreateInt64(new long[] { long.MinValue, long.MaxValue }, byteOrder);
        RawTagValue rationals = RawTagValue.CreateRational(new RawRational[] { new RawRational(1U, 3U), new RawRational(uint.MaxValue, 7U) }, byteOrder);
        RawTagValue signedRationals = RawTagValue.CreateRational(new RawRational[] { RawRational.CreateSigned(-2, 5) }, byteOrder);
        float nan = BitConverter.UInt32BitsToSingle(0x7FC12345);
        double doubleNan = BitConverter.UInt64BitsToDouble(0x7FF8123456789ABC);
        RawTagValue singles = RawTagValue.CreateSingle(new float[] { -1.5F, nan }, byteOrder);
        RawTagValue doubles = RawTagValue.CreateDouble(new double[] { 2.25, doubleNan }, byteOrder);

        byte[] copy = doubles.CopyBytes();
        copy[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(bytes.GetByteValues(), Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(signedBytes.GetSignedByteValues(), Is.EqualTo(new sbyte[] { -128, -1, 127 }));
            Assert.That(shorts.GetUInt16Values(), Is.EqualTo(new ushort[] { 0, 0xFEDC }));
            Assert.That(signedShorts.GetInt16Values(), Is.EqualTo(new short[] { short.MinValue, short.MaxValue }));
            Assert.That(longs.GetUInt32Values(), Is.EqualTo(new uint[] { 0, uint.MaxValue }));
            Assert.That(ifd.GetUInt32Values(), Is.EqualTo(new uint[] { 42 }));
            Assert.That(signedLongs.GetInt32Values(), Is.EqualTo(new int[] { int.MinValue, int.MaxValue }));
            Assert.That(long8.GetUInt64Values(), Is.EqualTo(new ulong[] { 0, ulong.MaxValue }));
            Assert.That(ifd8.GetUInt64Values(), Is.EqualTo(new ulong[] { 99 }));
            Assert.That(signedLong8.GetInt64Values(), Is.EqualTo(new long[] { long.MinValue, long.MaxValue }));
            Assert.That(rationals.GetRationalValues()[1].Numerator, Is.EqualTo((long)uint.MaxValue));
            Assert.That(signedRationals.GetRationalValues()[0].Numerator, Is.EqualTo(-2));
            Assert.That(BitConverter.SingleToUInt32Bits(singles.GetSingleValues()[1]), Is.EqualTo(0x7FC12345));
            Assert.That(BitConverter.DoubleToUInt64Bits(doubles.GetDoubleValues()[1]), Is.EqualTo(0x7FF8123456789ABC));
            Assert.That(doubles.CopyBytes(), Is.Not.EqualTo(copy));
            Assert.Throws<RawFormatException>(() => shorts.GetInt16Values());
            Assert.Throws<RawFormatException>(() => new RawTagValue(RawTagType.Long, 2, byteOrder, new byte[4]));
        });
    }

    [Test]
    public void ExifTextEncodersSupportAsciiUtf8JisUnicodeAndLegacyUtf16()
    {
        RawTagValue ascii = RawTagValue.CreateAscii("Camera");
        RawTagValue utf8 = RawTagValue.CreateUtf8("Cámara 東京");
        RawTagValue jis = RawTagValue.CreatePrefixedText("東京", RawTextEncoding.Jis, RawByteOrder.BigEndian);
        RawTagValue unicode = RawTagValue.CreatePrefixedText("Привет", RawTextEncoding.Unicode, RawByteOrder.LittleEndian);
        byte[] legacyPrefix = new byte[] { 85, 78, 73, 67, 79, 68, 69, 0 };
        byte[] legacyText = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("旧式\0")).ToArray();
        RawTagValue legacy = RawTagValue.CreateUndefined(legacyPrefix.Concat(legacyText).ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(ascii.GetText().Text, Is.EqualTo("Camera"));
            Assert.That(ascii.GetText().NullTerminated, Is.True);
            Assert.That(utf8.Type, Is.EqualTo(RawTagType.Utf8));
            Assert.That(utf8.RawBytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(utf8.GetText().Text, Is.EqualTo("Cámara 東京"));
            Assert.That(jis.GetText().Text, Is.EqualTo("東京"));
            Assert.That(jis.GetText().Encoding, Is.EqualTo(RawTextEncoding.Jis));
            Assert.That(unicode.GetText().Text, Is.EqualTo("Привет"));
            Assert.That(unicode.GetText().Prefix, Is.EqualTo(legacyPrefix));
            Assert.That(legacy.GetText().Text, Is.EqualTo("旧式"));
            Assert.Throws<RawFormatException>(() => RawTagValue.CreateAscii("München"));
            Assert.Throws<RawFormatException>(() => RawTagValue.CreateUtf8("bad\0text"));
        });
    }

    [Test]
    public void TimestampCombinesOffsetAndSubsecondsAndWritesExactExifForms()
    {
        RawExifTimestamp timestamp = new RawExifTimestamp("2026:08:19 14:05:06", "-07:30", "123456789");
        DateTimeOffset converted = timestamp.ToDateTimeOffset();
        RawExifTimestamp created = RawExifTimestamp.Create(new DateTimeOffset(2024, 2, 29, 23, 59, 58, TimeSpan.FromHours(5.5)).AddTicks(7654321), 6);
        RawExifTimestamp invalidDate = new RawExifTimestamp("2023:02:29 00:00:00", "+00:00", "1");
        RawExifTimestamp invalidOffset = new RawExifTimestamp("2024:02:29 00:00:00", "+14:30", "1");

        Assert.Multiple(() =>
        {
            Assert.That(timestamp.Valid, Is.True);
            Assert.That(converted.Offset, Is.EqualTo(TimeSpan.FromMinutes(-450)));
            Assert.That(converted.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567));
            Assert.That(timestamp.EncodeDateTime().RawBytes.Length, Is.EqualTo(20));
            Assert.That(timestamp.EncodeOffset().GetText().Text, Is.EqualTo("-07:30"));
            Assert.That(timestamp.EncodeSubseconds().GetText().Text, Is.EqualTo("123456789"));
            Assert.That(created.DateTimeText, Is.EqualTo("2024:02:29 23:59:58"));
            Assert.That(created.OffsetText, Is.EqualTo("+05:30"));
            Assert.That(created.Subseconds, Is.EqualTo("765432"));
            Assert.That(invalidDate.Valid, Is.False);
            Assert.That(invalidOffset.Valid, Is.False);
            Assert.Throws<RawFormatException>(() => invalidDate.ToDateTimeOffset());
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void MetadataOpenBuildsTypedExifGpsAndInteropView(RawByteOrder byteOrder)
    {
        byte[] source = BuildMetadataTiff(byteOrder);
        RawOpenOptions options = new RawOpenOptions();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(source), null, options);
        RawMetadata metadata = document.Metadata;
        RawIfd? exif = metadata.FindIfd("IFD0/Exif");
        RawIfd? gps = metadata.FindIfd("IFD0/GPS");
        RawIfd? interoperability = metadata.FindIfd("IFD0/Exif/Interop");

        Assert.Multiple(() =>
        {
            Assert.That(document.MetadataLoaded, Is.True);
            Assert.That(metadata.Ifds.Length, Is.EqualTo(4));
            Assert.That(metadata.Make, Is.EqualTo("Acme"));
            Assert.That(metadata.Model, Is.EqualTo("模型一号"));
            Assert.That(metadata.LensMake, Is.EqualTo("Lens Co"));
            Assert.That(metadata.LensModel, Is.EqualTo("50mm ƒ/1.4"));
            Assert.That(metadata.ExifVersion, Is.EqualTo("0300"));
            Assert.That(metadata.InteroperabilityIndex, Is.EqualTo("R03"));
            Assert.That((metadata.UserComment ?? throw new System.InvalidOperationException("A required value is unavailable.")).Text, Is.EqualTo("sample comment"));
            Assert.That((metadata.OriginalTime ?? throw new System.InvalidOperationException("A required value is unavailable.")).Valid, Is.True);
            Assert.That(metadata.OriginalTime.OffsetMinutes, Is.EqualTo(-420));
            Assert.That(metadata.OriginalTime.Subseconds, Is.EqualTo("125"));
            Assert.That((metadata.GpsLocation ?? throw new System.InvalidOperationException("A required value is unavailable.")).HasCoordinates, Is.True);
            Assert.That(metadata.GpsLocation.Latitude, Is.EqualTo(37.8083333333).Within(0.0000001));
            Assert.That(metadata.GpsLocation.Longitude, Is.EqualTo(-122.4041666667).Within(0.0000001));
            Assert.That(metadata.GpsLocation.Altitude, Is.EqualTo(-15.5));
            Assert.That((exif ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIfdKind.Exif));
            Assert.That((exif.FindTag(41998) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.Type, Is.EqualTo(RawTagType.Utf8));
            Assert.That((gps ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIfdKind.Gps));
            Assert.That((interoperability ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawIfdKind.Interoperability));
            Assert.That((exif.FindTag(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Known, Is.False);
            Assert.That((exif.FindTag(65000) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(new byte[] { 9, 8, 7 }));
        });

        RawDocument previews = RawCodec.OpenPreviews(new ReadOnlyMemory<byte>(source), null, options);
        Assert.That(previews.Metadata.Ifds, Is.Empty);
    }

    [Test]
    public void TypedUtf8TagWritesThroughTiffPlannerAndParsesBack()
    {
        RawTag tag = new RawTag(42036, "LensModel", "IFD0", -1, true, RawTagValue.CreateUtf8("レンズ 35mm"));
        TiffLayoutEntryInput entry = ExifCodec.EncodeTag(tag);
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[] { entry }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(false, RawByteOrder.LittleEndian, "IFD0", new TiffLayoutIfdInput[] { root }, Array.Empty<TiffLayoutBlockInput>());
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        MemoryStream stream = new MemoryStream();

        TiffWriter.Write(stream, plan, new RawLimits(), CancellationToken.None);
        TiffIfdGraph graph = TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(stream.ToArray()), 0, stream.Length, new RawLimits()));
        TiffEntry? parsed = (graph.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(42036);

        Assert.Multiple(() =>
        {
            Assert.That((parsed ?? throw new System.InvalidOperationException("A required value is unavailable.")).FieldType, Is.EqualTo(TiffFieldType.Utf8));
            Assert.That(parsed.GetUtf8(), Is.EqualTo("レンズ 35mm\0"));
            Assert.That(parsed.RawBytes, Is.EqualTo(tag.Value.RawBytes));
        });
    }

    static byte[] BuildMetadataTiff(RawByteOrder byteOrder)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        SyntheticTiffIfd gps = builder.AddIfd();
        SyntheticTiffIfd interoperability = builder.AddIfd();

        AddValue(builder, root, 271, RawTagValue.CreateAscii("Acme"));
        AddValue(builder, root, 272, RawTagValue.CreateUtf8("模型一号"));
        AddValue(builder, root, 306, RawTagValue.CreateAscii("2026:08:19 14:05:06"));
        builder.AddPointer(root, 34665, TiffFieldType.Long, exif);
        builder.AddPointer(root, 34853, TiffFieldType.Long, gps);

        AddValue(builder, exif, 36864, RawTagValue.CreateUndefined(Encoding.ASCII.GetBytes("0300")));
        AddValue(builder, exif, 36867, RawTagValue.CreateAscii("2026:08:19 14:05:06"));
        AddValue(builder, exif, 36881, RawTagValue.CreateAscii("-07:00"));
        AddValue(builder, exif, 37521, RawTagValue.CreateAscii("125"));
        AddValue(builder, exif, 37510, RawTagValue.CreatePrefixedText("sample comment", RawTextEncoding.Ascii, byteOrder));
        AddValue(builder, exif, 41998, RawTagValue.CreateUtf8("straight reproduction"));
        AddValue(builder, exif, 42035, RawTagValue.CreateAscii("Lens Co"));
        AddValue(builder, exif, 42036, RawTagValue.CreateUtf8("50mm ƒ/1.4"));
        AddValue(builder, exif, 65000, RawTagValue.CreateUndefined(new byte[] { 9, 8, 7 }));
        builder.AddPointer(exif, 40965, TiffFieldType.Long, interoperability);

        AddValue(builder, gps, 0, RawTagValue.CreateByte(new byte[] { 2, 4, 0, 0 }));
        AddValue(builder, gps, 1, RawTagValue.CreateAscii("N"));
        AddValue(builder, gps, 2, RawTagValue.CreateRational(new RawRational[] { new RawRational(37U, 1U), new RawRational(48U, 1U), new RawRational(30U, 1U) }, byteOrder));
        AddValue(builder, gps, 3, RawTagValue.CreateAscii("W"));
        AddValue(builder, gps, 4, RawTagValue.CreateRational(new RawRational[] { new RawRational(122U, 1U), new RawRational(24U, 1U), new RawRational(15U, 1U) }, byteOrder));
        AddValue(builder, gps, 5, RawTagValue.CreateByte(new byte[] { 1 }));
        AddValue(builder, gps, 6, RawTagValue.CreateRational(new RawRational[] { new RawRational(31U, 2U) }, byteOrder));
        AddValue(builder, gps, 27, RawTagValue.CreatePrefixedText("GNSS", RawTextEncoding.Unicode, byteOrder));

        AddValue(builder, interoperability, 1, RawTagValue.CreateAscii("R03"));
        return builder.Build();
    }

    static void AddValue(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, RawTagValue value)
    {
        builder.AddField(ifd, tag, (TiffFieldType)(ushort)value.Type, value.Count, value.CopyBytes());
    }
}
