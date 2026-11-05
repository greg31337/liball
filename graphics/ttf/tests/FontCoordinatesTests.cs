using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontCoordinatesTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");
    static int SlantTag = (int)TestSfntEncoding.EncodeTag("slnt");

    [Test]
    public void StoresValidFiniteCoordinatesAndReplacesValues()
    {
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(WeightTag, 300);
        coordinates.Set(WidthTag, 90);
        coordinates.Set(WeightTag, 350);

        Assert.Multiple(() =>
        {
            Assert.That(coordinates.GetCount(), Is.EqualTo(2));
            Assert.That(coordinates.Contains(WeightTag), Is.True);
            Assert.That(coordinates.Contains(SlantTag), Is.False);
            Assert.That(coordinates.Get(WeightTag), Is.EqualTo(350));
            Assert.That(coordinates.Get(WidthTag), Is.EqualTo(90));
            Assert.Throws<ArgumentException>(() => coordinates.Get(SlantTag));
            Assert.Throws<ArgumentOutOfRangeException>(() => coordinates.Set(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                coordinates.Set(WeightTag, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                coordinates.Set(WeightTag, double.PositiveInfinity));
        });
    }

    [Test]
    public void ClampsDefaultsCanonicalizesAndMapsVersionOneAvar()
    {
        FontFace face = CreateFace(BuildAvar1(), BuildFvar());
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(WeightTag, 250);
        coordinates.Set(WidthTag, 300);
        NormalizedFontCoordinates normalized = coordinates.Normalize(face);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GetAxisCount(), Is.EqualTo(2));
            Assert.That(normalized.GetAxisTag(0), Is.EqualTo(WeightTag));
            Assert.That(normalized.GetAxisTag(1), Is.EqualTo(WidthTag));
            Assert.That(normalized.GetUserValue(0), Is.EqualTo(250));
            Assert.That(normalized.GetUserValue(1), Is.EqualTo(200));
            Assert.That(normalized.GetNormalizedValue(0), Is.EqualTo(-0.25));
            Assert.That(normalized.GetNormalizedValue(1), Is.EqualTo(1));
            Assert.That(normalized.GetKey(),
                Is.EqualTo("v1:77676874=F000:77647468=4000"));
            Assert.That(normalized.HasNamedInstance(), Is.False);
            Assert.That(normalized.GetNamedInstanceIndex(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void UsesDefaultsAndCanonicalOpenTypeSixteenDotSixteenInput()
    {
        FontFace face = CreateFace(null, BuildFvar());
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(WeightTag, 400 + 0.5 / 65536.0);
        NormalizedFontCoordinates normalized = coordinates.Normalize(face);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GetUserValue(0), Is.EqualTo(400 + 1.0 / 65536.0));
            Assert.That(normalized.GetUserValue(1), Is.EqualTo(100));
            Assert.That(normalized.GetNormalizedValue(0), Is.Zero);
            Assert.That(normalized.GetNormalizedValue(1), Is.Zero);
            Assert.That(normalized.GetKey(),
                Is.EqualTo("v1:77676874=0000:77647468=0000"));
            Assert.That(normalized.HasNamedInstance(), Is.False);
        });
    }

    [Test]
    public void IgnoresInactiveAvarMapsWithoutRequiredAnchors()
    {
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(WeightTag, 250);
        NormalizedFontCoordinates normalized = coordinates.Normalize(
            CreateFace(BuildInactiveAvar(), BuildFvar()));

        Assert.That(normalized.GetNormalizedValue(0), Is.EqualTo(-0.5));
    }

    [Test]
    public void AppliesVersionTwoCoordinateDependentDeltasAfterSegmentMaps()
    {
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(WeightTag, 650);
        coordinates.Set(WidthTag, 75);
        NormalizedFontCoordinates normalized = coordinates.Normalize(
            CreateFace(BuildAvar2(), BuildFvar()));

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GetNormalizedValue(0),
                Is.EqualTo(11679.0 / 16384.0));
            Assert.That(normalized.GetNormalizedValue(1),
                Is.EqualTo(-6966.0 / 16384.0));
        });
    }

    [Test]
    public void SelectsFirstExactNamedInstanceInUserSpace()
    {
        FontFace face = CreateFace(BuildAvar1(), BuildFvar());
        FontCoordinates defaultCoordinates = new FontCoordinates();
        FontCoordinates namedCoordinates = new FontCoordinates();
        namedCoordinates.Set(WeightTag, 700);
        namedCoordinates.Set(WidthTag, 75);
        NormalizedFontCoordinates defaults = defaultCoordinates.Normalize(face);
        NormalizedFontCoordinates named = namedCoordinates.Normalize(face);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.HasNamedInstance(), Is.True);
            Assert.That(defaults.GetNamedInstanceIndex(), Is.Zero);
            Assert.That(named.HasNamedInstance(), Is.True);
            Assert.That(named.GetNamedInstanceIndex(), Is.EqualTo(1));
        });
    }

    [Test]
    public void NamedInstanceKeepsUserIdentityWhileAvarMapsItsCoordinates()
    {
        FontFace face = CreateFace(BuildAvar1(), BuildFvar());
        FontInstance instance = face.CreateNamedInstance(1);
        NormalizedFontCoordinates coordinates = instance.GetCoordinates();

        Assert.Multiple(() =>
        {
            Assert.That(coordinates.HasNamedInstance(), Is.True);
            Assert.That(coordinates.GetNamedInstanceIndex(), Is.EqualTo(1));
            Assert.That(coordinates.GetUserValue(0), Is.EqualTo(700));
            Assert.That(coordinates.GetUserValue(1), Is.EqualTo(75));
            Assert.That(coordinates.GetNormalizedValue(0),
                Is.EqualTo(0.8).Within(1.0 / 16384.0));
            Assert.That(coordinates.GetNormalizedValue(1), Is.EqualTo(-0.5));
            Assert.That(face.GetFvarTable()!.GetInstance(1)
                .GetSubfamilyName().GetValue(), Is.EqualTo("Bold Condensed"));
        });
    }

    [Test]
    public void BuildsDeterministicKeysInFvarOrderForEquivalentLocations()
    {
        FontFace face = CreateFace(BuildAvar1(), BuildFvar());
        FontCoordinates first = new FontCoordinates();
        first.Set(WidthTag, 1000);
        first.Set(WeightTag, 250);
        FontCoordinates second = new FontCoordinates();
        second.Set(WeightTag, 250);
        second.Set(WidthTag, 200);
        FontCoordinates different = new FontCoordinates();
        different.Set(WeightTag, 251);
        different.Set(WidthTag, 200);

        Assert.Multiple(() =>
        {
            Assert.That(first.Normalize(face).GetKey(),
                Is.EqualTo(second.Normalize(face).GetKey()));
            Assert.That(first.Normalize(face).GetKey(),
                Is.Not.EqualTo(different.Normalize(face).GetKey()));
        });
    }

    [Test]
    public void CreatesDefaultCustomAndNamedFontInstances()
    {
        FontFace face = CreateFace(BuildAvar1(), BuildFvar());
        FontCoordinates customCoordinates = new FontCoordinates();
        customCoordinates.Set(WeightTag, 650);
        FontInstance defaultInstance = face.CreateDefaultInstance();
        FontInstance equivalentDefault = face.CreateInstance(new FontCoordinates());
        FontInstance custom = face.CreateInstance(customCoordinates);
        FontInstance firstNamed = face.CreateNamedInstance(0);
        FontInstance secondNamed = face.CreateNamedInstance(1);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetNamedInstanceCount(), Is.EqualTo(2));
            Assert.That(equivalentDefault, Is.SameAs(defaultInstance));
            Assert.That(firstNamed, Is.SameAs(defaultInstance));
            Assert.That(custom, Is.Not.SameAs(defaultInstance));
            Assert.That(custom.GetFace(), Is.SameAs(face));
            Assert.That(custom.GetCoordinates().GetUserValue(0), Is.EqualTo(650));
            Assert.That(secondNamed.GetCoordinates().GetNamedInstanceIndex(), Is.EqualTo(1));
            Assert.That(secondNamed.GetCoordinates().GetUserValue(0), Is.EqualTo(700));
            Assert.That(secondNamed.GetCoordinates().GetUserValue(1), Is.EqualTo(75));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.CreateNamedInstance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.CreateNamedInstance(2));
            Assert.Throws<ArgumentNullException>(() => face.CreateInstance(null!));
        });
    }

    [Test]
    public void ExplicitNamedSelectionPreservesDuplicateLocationIdentity()
    {
        byte[] fvar = BuildFvar();
        PatchInt32(fvar, 72, Fixed(400));
        PatchInt32(fvar, 76, Fixed(100));
        FontFace face = CreateFace(BuildAvar1(), fvar);
        FontInstance selected = face.CreateNamedInstance(1);

        Assert.Multiple(() =>
        {
            Assert.That(new FontCoordinates().Normalize(face).GetNamedInstanceIndex(),
                Is.Zero);
            Assert.That(selected, Is.Not.SameAs(face.CreateDefaultInstance()));
            Assert.That(selected.GetCoordinates().GetNamedInstanceIndex(), Is.EqualTo(1));
        });
    }

    [Test]
    public void SupportsStaticFacesAndRejectsUnknownAxes()
    {
        FontFace staticFace = CreateFace(null, null);
        FontCoordinates empty = new FontCoordinates();
        FontCoordinates unknown = new FontCoordinates();
        unknown.Set(WeightTag, 400);
        NormalizedFontCoordinates normalized = empty.Normalize(staticFace);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GetAxisCount(), Is.Zero);
            Assert.That(normalized.GetKey(), Is.EqualTo("v1"));
            Assert.That(staticFace.GetNamedInstanceCount(), Is.Zero);
            Assert.That(staticFace.CreateInstance(empty),
                Is.SameAs(staticFace.CreateDefaultInstance()));
            Assert.Throws<ArgumentException>(() => unknown.Normalize(staticFace));
            Assert.Throws<ArgumentOutOfRangeException>(() => normalized.GetAxisTag(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => normalized.GetUserValue(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => normalized.GetNormalizedValue(0));
            Assert.Throws<ArgumentNullException>(() => empty.Normalize(null!));
        });
    }

    [Test]
    public void RejectsUnknownAxesAndUnsupportedVariationTables()
    {
        FontCoordinates unknown = new FontCoordinates();
        unknown.Set(SlantTag, 0);
        byte[] unsupportedFvar = BuildFvar();
        PatchUInt16(unsupportedFvar, 0, 2);
        byte[] unsupportedAvar = BuildAvar1();
        PatchUInt16(unsupportedAvar, 0, 3);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => unknown.Normalize(
                CreateFace(BuildAvar1(), BuildFvar())));
            Assert.Throws<FontFormatException>(() => new FontCoordinates().Normalize(
                CreateFace(null, unsupportedFvar)));
            Assert.Throws<FontFormatException>(() => new FontCoordinates().Normalize(
                CreateFace(unsupportedAvar, BuildFvar())));
        });
    }

    static FontFace CreateFace(byte[]? avar, byte[]? fvar)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(1)
            .CopyBytes());
        AddTable(tables, FontTags.Name, BuildNameTable());

        if (fvar != null)
        {
            AddTable(tables, FontTags.Fvar, fvar);
        }

        if (avar != null)
        {
            AddTable(tables, FontTags.Avar, avar);
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildFvar()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(16)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt16(20)
            .WriteUInt16(2)
            .WriteUInt16(12);
        WriteFvarAxis(builder, "wght", 100, 400, 900, 256);
        WriteFvarAxis(builder, "wdth", 50, 100, 200, 257);
        WriteFvarInstance(builder, 258, 400, 100);
        WriteFvarInstance(builder, 259, 700, 75);
        return builder.CopyBytes();
    }

    static void WriteFvarAxis(
        TestBinaryBuilder builder,
        string tag,
        double minimum,
        double defaultValue,
        double maximum,
        int nameId)
    {
        builder
            .WriteTag(tag)
            .WriteInt32(Fixed(minimum))
            .WriteInt32(Fixed(defaultValue))
            .WriteInt32(Fixed(maximum))
            .WriteUInt16(0)
            .WriteUInt16((ushort)nameId);
    }

    static void WriteFvarInstance(
        TestBinaryBuilder builder,
        int nameId,
        double weight,
        double width)
    {
        builder
            .WriteUInt16((ushort)nameId)
            .WriteUInt16(0)
            .WriteInt32(Fixed(weight))
            .WriteInt32(Fixed(width));
    }

    static byte[] BuildAvar1()
    {
        TestBinaryBuilder builder = WriteAvarHeader(1, 2);
        WriteWeightSegmentMap(builder);
        builder.WriteUInt16(0);
        return builder.CopyBytes();
    }

    static byte[] BuildInactiveAvar()
    {
        TestBinaryBuilder builder = WriteAvarHeader(1, 2)
            .WriteUInt16(2);
        WriteValueMap(builder, -1, -1);
        WriteValueMap(builder, 1, 1);
        builder.WriteUInt16(0);
        return builder.CopyBytes();
    }

    static byte[] BuildAvar2()
    {
        TestBinaryBuilder builder = WriteAvarHeader(2, 2);
        WriteWeightSegmentMap(builder);
        builder.WriteUInt16(0);
        int mapOffsetField = builder.GetLength();
        builder.WriteUInt32(0);
        int storeOffsetField = builder.GetLength();
        builder.WriteUInt32(0);
        int mapOffset = builder.GetLength();
        builder.PatchUInt32(mapOffsetField, (uint)mapOffset);
        builder
            .WriteUInt8(0)
            .WriteUInt8(0x17)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0);
        int storeOffset = builder.GetLength();
        builder.PatchUInt32(storeOffsetField, (uint)storeOffset);
        builder
            .WriteUInt16(1)
            .WriteUInt32(12)
            .WriteUInt16(1)
            .WriteUInt32(40)
            .WriteUInt16(2)
            .WriteUInt16(2);
        WriteRegionAxis(builder, 0, 1, 1);
        WriteRegionAxis(builder, 0, 0, 0);
        WriteRegionAxis(builder, 0, 0, 0);
        WriteRegionAxis(builder, -1, -1, 0);
        builder
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteInt16(1638)
            .WriteInt8(-5)
            .WriteInt16(-819)
            .WriteInt8(10);
        return builder.CopyBytes();
    }

    static TestBinaryBuilder WriteAvarHeader(int majorVersion, int mapCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)majorVersion)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)mapCount);
    }

    static void WriteWeightSegmentMap(TestBinaryBuilder builder)
    {
        builder.WriteUInt16(5);
        WriteValueMap(builder, -1, -1);
        WriteValueMap(builder, -0.5, -0.25);
        WriteValueMap(builder, 0, 0);
        WriteValueMap(builder, 0.5, 0.75);
        WriteValueMap(builder, 1, 1);
    }

    static void WriteValueMap(
        TestBinaryBuilder builder,
        double fromCoordinate,
        double toCoordinate)
    {
        builder
            .WriteInt16(F2Dot14(fromCoordinate))
            .WriteInt16(F2Dot14(toCoordinate));
    }

    static void WriteRegionAxis(
        TestBinaryBuilder builder,
        double start,
        double peak,
        double end)
    {
        builder
            .WriteInt16(F2Dot14(start))
            .WriteInt16(F2Dot14(peak))
            .WriteInt16(F2Dot14(end));
    }

    static byte[] BuildNameTable()
    {
        int[] nameIds = new int[] { 256, 257, 258, 259 };
        string[] values = new string[]
        {
            "Weight",
            "Width",
            "Regular",
            "Bold Condensed"
        };
        byte[][] encoded = new byte[values.Length][];
        int[] offsets = new int[values.Length];
        int nextOffset = 0;
        int index = 0;

        while (index < values.Length)
        {
            encoded[index] = Encoding.BigEndianUnicode.GetBytes(values[index]);
            offsets[index] = nextOffset;
            nextOffset += encoded[index].Length;
            index += 1;
        }

        int storageOffset = 6 + values.Length * 12;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)storageOffset);
        index = 0;

        while (index < values.Length)
        {
            builder
                .WriteUInt16(3)
                .WriteUInt16(1)
                .WriteUInt16(0x0409)
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)offsets[index]);
            index += 1;
        }

        index = 0;

        while (index < encoded.Length)
        {
            builder.WriteBytes(encoded[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static short F2Dot14(double value)
    {
        return System.Convert.ToInt16(System.Math.Round(value * 16384.0));
    }

    static int Fixed(double value)
    {
        return System.Convert.ToInt32(System.Math.Round(value * 65536.0));
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
