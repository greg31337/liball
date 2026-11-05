using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontVariationsPublicTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");

    [Test]
    public void ExposesLocalizedAxesAndNamedInstances()
    {
        FontFace face = CreateFace();
        FontVariations defaults = face.GetVariations();
        FontVariations french = face.GetLocalizedVariations("fr-FR");
        FontVariationAxis weight = french.GetAxis(0);
        FontVariationAxis width = french.FindAxis(WidthTag)!;
        FontNamedInstance regular = french.GetNamedInstance(0);
        FontNamedInstance boldCondensed = french.GetNamedInstance(1);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetVariations(), Is.SameAs(defaults));
            Assert.That(defaults.GetRequestedLanguage(), Is.Null);
            Assert.That(french.GetRequestedLanguage(), Is.EqualTo("fr-FR"));
            Assert.That(french.GetAxisCount(), Is.EqualTo(2));
            Assert.That(weight.GetTag(), Is.EqualTo(WeightTag));
            Assert.That(weight.GetMinimumValue(), Is.EqualTo(100));
            Assert.That(weight.GetDefaultValue(), Is.EqualTo(400));
            Assert.That(weight.GetMaximumValue(), Is.EqualTo(900));
            Assert.That(weight.GetFlags(), Is.EqualTo(1));
            Assert.That(weight.IsHidden(), Is.True);
            Assert.That(weight.GetNameId(), Is.EqualTo(256));
            Assert.That(weight.GetName().GetValue(), Is.EqualTo("Poids"));
            Assert.That(weight.GetName().GetLanguageTag(), Is.EqualTo("fr-FR"));
            Assert.That(width.GetName().GetValue(), Is.EqualTo("Largeur"));
            Assert.That(width.IsHidden(), Is.False);
            Assert.That(french.GetNamedInstanceCount(), Is.EqualTo(2));
            Assert.That(regular.GetIndex(), Is.Zero);
            Assert.That(regular.GetSubfamilyName().GetValue(), Is.EqualTo("Normal"));
            Assert.That(regular.GetCoordinateCount(), Is.EqualTo(2));
            Assert.That(regular.GetAxisTag(0), Is.EqualTo(WeightTag));
            Assert.That(regular.GetCoordinate(0), Is.EqualTo(400));
            Assert.That(regular.GetCoordinate(1), Is.EqualTo(100));
            Assert.That(regular.HasPostScriptName(), Is.False);
            Assert.That(regular.GetPostScriptNameId(), Is.EqualTo(65535));
            Assert.That(regular.GetPostScriptName(), Is.Null);
            Assert.That(boldCondensed.GetSubfamilyName().GetValue(),
                Is.EqualTo("Gras condensé"));
            Assert.That(boldCondensed.GetCoordinate(0), Is.EqualTo(700));
            Assert.That(boldCondensed.GetCoordinate(1), Is.EqualTo(75));
            Assert.That(french.FindAxis(Tag("opsz")), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => french.GetAxis(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                french.GetNamedInstance(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                regular.GetAxisTag(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                regular.GetCoordinate(-1));
        });
    }

    [Test]
    public void ExposesAvarMappingsAndLocalizedStatAttributes()
    {
        FontVariations variations = CreateFace().GetLocalizedVariations("fr-FR");
        FontAvar avar = variations.GetAvar()!;
        FontAvarAxisMap weightMap = avar.FindAxisMap(WeightTag)!;
        FontStat stat = variations.GetStat()!;
        FontStatAxisValue regular = stat.GetAxisValue(0);
        FontStatAxisValue combined = stat.GetAxisValue(1);
        FontStatAxisValueRecord combinedWeight = combined.GetRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(avar.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(avar.GetMinorVersion(), Is.Zero);
            Assert.That(avar.GetAxisMapCount(), Is.EqualTo(2));
            Assert.That(avar.HasVariableMappings(), Is.False);
            Assert.That(weightMap.GetAxisTag(), Is.EqualTo(WeightTag));
            Assert.That(weightMap.HasRequiredMappings(), Is.True);
            Assert.That(weightMap.GetValueMapCount(), Is.EqualTo(5));
            Assert.That(weightMap.GetValueMap(1).GetFromCoordinate(),
                Is.EqualTo(-0.5));
            Assert.That(weightMap.GetValueMap(1).GetToCoordinate(),
                Is.EqualTo(-0.25));
            Assert.That(weightMap.Map(-0.75), Is.EqualTo(-0.625));
            Assert.That(avar.GetAxisMap(1).HasRequiredMappings(), Is.True);
            Assert.That(avar.GetAxisMap(1).Map(0.4), Is.EqualTo(0.4));
            Assert.That(avar.FindAxisMap(Tag("opsz")), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => weightMap.Map(2));
            Assert.That(stat.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(stat.GetMinorVersion(), Is.EqualTo(2));
            Assert.That(stat.GetAxisCount(), Is.EqualTo(2));
            Assert.That(stat.GetAxis(0).GetTag(), Is.EqualTo(WidthTag));
            Assert.That(stat.GetAxis(0).GetOrdering(), Is.EqualTo(1));
            Assert.That(stat.GetAxis(0).GetName().GetValue(), Is.EqualTo("Largeur"));
            Assert.That(stat.FindAxis(WeightTag)!.GetName().GetValue(),
                Is.EqualTo("Poids"));
            Assert.That(stat.GetAxisValueCount(), Is.EqualTo(2));
            Assert.That(regular.GetFormat(), Is.EqualTo(1));
            Assert.That(regular.IsSupported(), Is.True);
            Assert.That(regular.GetAxisIndex(), Is.EqualTo(1));
            Assert.That(regular.IsElidable(), Is.True);
            Assert.That(regular.IsOlderSiblingFontAttribute(), Is.False);
            Assert.That(regular.GetName()!.GetValue(), Is.EqualTo("Normal"));
            Assert.That(regular.GetValue(), Is.EqualTo(400));
            Assert.That(combined.GetFormat(), Is.EqualTo(4));
            Assert.That(combined.IsOlderSiblingFontAttribute(), Is.True);
            Assert.That(combined.GetName()!.GetValue(), Is.EqualTo("Gras condensé"));
            Assert.That(combined.GetRecordCount(), Is.EqualTo(2));
            Assert.That(combinedWeight.GetAxisIndex(), Is.EqualTo(1));
            Assert.That(combinedWeight.GetAxisTag(), Is.EqualTo(WeightTag));
            Assert.That(combinedWeight.GetValue(), Is.EqualTo(700));
            Assert.That(stat.HasElidedFallbackName(), Is.True);
            Assert.That(stat.GetElidedFallbackNameId(), Is.EqualTo(258));
            Assert.That(stat.GetElidedFallbackName()!.GetValue(), Is.EqualTo("Normal"));
            Assert.Throws<ArgumentOutOfRangeException>(() => stat.GetAxisValue(2));
        });
    }

    [Test]
    public void ReportsClampingAndEveryUserToNormalizedMappingStage()
    {
        FontCoordinates requested = new FontCoordinates();
        requested.Set(WeightTag, 250);
        requested.Set(WidthTag, 300);
        NormalizedFontCoordinates normalized = requested.Normalize(CreateFace());
        FontCoordinateMapping weight = normalized.GetMapping(0);
        FontCoordinateMapping width = normalized.GetMapping(1);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.GetClampedAxisCount(), Is.EqualTo(1));
            Assert.That(weight.GetAxisTag(), Is.EqualTo(WeightTag));
            Assert.That(weight.WasExplicitlySpecified(), Is.True);
            Assert.That(weight.GetRequestedValue(), Is.EqualTo(250));
            Assert.That(weight.GetMinimumValue(), Is.EqualTo(100));
            Assert.That(weight.GetDefaultValue(), Is.EqualTo(400));
            Assert.That(weight.GetMaximumValue(), Is.EqualTo(900));
            Assert.That(weight.WasClamped(), Is.False);
            Assert.That(weight.GetUserValue(), Is.EqualTo(250));
            Assert.That(weight.GetUnmappedNormalizedValue(), Is.EqualTo(-0.5));
            Assert.That(weight.GetNormalizedValue(), Is.EqualTo(-0.25));
            Assert.That(weight.WasAvarMapped(), Is.True);
            Assert.That(width.GetRequestedValue(), Is.EqualTo(300));
            Assert.That(width.WasClamped(), Is.True);
            Assert.That(width.GetUserValue(), Is.EqualTo(200));
            Assert.That(width.GetNormalizedValue(), Is.EqualTo(1));
            Assert.That(width.WasAvarMapped(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                normalized.GetMapping(2));
        });
    }

    static FontFace CreateFace()
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(1)
            .CopyBytes());
        AddTable(tables, FontTags.Name, BuildNameTable());
        AddTable(tables, FontTags.Fvar, BuildFvar());
        AddTable(tables, FontTags.Avar, BuildAvar());
        AddTable(tables, FontTags.Stat, BuildStat());
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
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
        WriteFvarAxis(builder, "wght", 100, 400, 900, 1, 256);
        WriteFvarAxis(builder, "wdth", 50, 100, 200, 0, 257);
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
        int flags,
        int nameId)
    {
        builder
            .WriteTag(tag)
            .WriteInt32(Fixed(minimum))
            .WriteInt32(Fixed(defaultValue))
            .WriteInt32(Fixed(maximum))
            .WriteUInt16((ushort)flags)
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

    static byte[] BuildAvar()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(5);
        WriteAvarValue(builder, -1, -1);
        WriteAvarValue(builder, -0.5, -0.25);
        WriteAvarValue(builder, 0, 0);
        WriteAvarValue(builder, 0.5, 0.75);
        WriteAvarValue(builder, 1, 1);
        builder.WriteUInt16(0);
        return builder.CopyBytes();
    }

    static void WriteAvarValue(TestBinaryBuilder builder, double from, double to)
    {
        builder
            .WriteInt16(F2Dot14(from))
            .WriteInt16(F2Dot14(to));
    }

    static byte[] BuildStat()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(8)
            .WriteUInt16(2)
            .WriteUInt32(20)
            .WriteUInt16(2)
            .WriteUInt32(36)
            .WriteUInt16(258);
        WriteStatAxis(builder, "wdth", 257, 1);
        WriteStatAxis(builder, "wght", 256, 0);
        builder
            .WriteUInt16(4)
            .WriteUInt16(16)
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(258)
            .WriteInt32(Fixed(400))
            .WriteUInt16(4)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(259)
            .WriteUInt16(1)
            .WriteInt32(Fixed(700))
            .WriteUInt16(0)
            .WriteInt32(Fixed(75));
        return builder.CopyBytes();
    }

    static void WriteStatAxis(
        TestBinaryBuilder builder,
        string tag,
        int nameId,
        int ordering)
    {
        builder
            .WriteTag(tag)
            .WriteUInt16((ushort)nameId)
            .WriteUInt16((ushort)ordering);
    }

    static byte[] BuildNameTable()
    {
        int[] nameIds = new int[]
        {
            256, 257, 258, 259, 256, 257, 258, 259
        };
        int[] languageIds = new int[]
        {
            0x0409, 0x0409, 0x0409, 0x0409,
            0x040C, 0x040C, 0x040C, 0x040C
        };
        string[] values = new string[]
        {
            "Weight", "Width", "Regular", "Bold Condensed",
            "Poids", "Largeur", "Normal", "Gras condensé"
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
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)(6 + values.Length * 12));
        index = 0;
        while (index < values.Length)
        {
            builder
                .WriteUInt16(3)
                .WriteUInt16(1)
                .WriteUInt16((ushort)languageIds[index])
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

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }

    static short F2Dot14(double value)
    {
        return System.Convert.ToInt16(System.Math.Round(value * 16384.0));
    }

    static int Fixed(double value)
    {
        return System.Convert.ToInt32(System.Math.Round(value * 65536.0));
    }
}
