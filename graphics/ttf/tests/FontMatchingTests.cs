using ttf;

namespace ttf.Tests;

internal class FontMatchingTests
{
    static int WeightTag = (int)TestSfntEncoding.EncodeTag("wght");
    static int WidthTag = (int)TestSfntEncoding.EncodeTag("wdth");
    static int OpticalSizeTag = (int)TestSfntEncoding.EncodeTag("opsz");
    static int ItalicTag = (int)TestSfntEncoding.EncodeTag("ital");
    static int SlantTag = (int)TestSfntEncoding.EncodeTag("slnt");
    static int GradeTag = (int)TestSfntEncoding.EncodeTag("GRAD");

    [Test]
    public void MatchesStaticStyleWeightAndStretchDeterministically()
    {
        FontFace normal = CreateStaticFace("Demo Family", "Regular", 400, 5, false, false, false);
        FontFace medium = CreateStaticFace("Demo Family", "Medium", 500, 5, false, false, false);
        FontFace bold = CreateStaticFace("Demo Family", "Bold", 700, 5, false, true, false);
        FontFace italic = CreateStaticFace("Demo Family", "Italic", 400, 5, true, false, false);
        FontFace oblique = CreateStaticFace("Demo Family", "Oblique", 400, 5, false, false, true);
        FontFace condensed = CreateStaticFace("Demo Family", "Condensed", 400, 3, false, false, false);
        FontSet set = new FontSet();
        set.Add(normal);
        set.Add(medium);
        set.Add(bold);
        set.Add(italic);
        set.Add(oblique);
        set.Add(condensed);

        FontRequest italicRequest = new FontRequest("  demo   family ");
        italicRequest.SetStyle(FontStyle.Italic);
        FontRequest obliqueRequest = new FontRequest("DEMO FAMILY");
        obliqueRequest.SetStyle(FontStyle.Oblique);
        FontRequest mediumRequest = new FontRequest("Demo Family");
        mediumRequest.SetWeight(450);
        FontRequest boldRequest = new FontRequest("Demo Family");
        boldRequest.SetWeight(650);
        FontRequest condensedRequest = new FontRequest("Demo Family");
        condensedRequest.SetStretch(75);

        Assert.Multiple(() =>
        {
            Assert.That(set.Match(italicRequest)!.GetFace(), Is.SameAs(italic));
            Assert.That(set.Match(obliqueRequest)!.GetFace(), Is.SameAs(oblique));
            Assert.That(set.Match(mediumRequest)!.GetFace(), Is.SameAs(medium));
            Assert.That(set.Match(boldRequest)!.GetFace(), Is.SameAs(bold));
            Assert.That(set.Match(condensedRequest)!.GetFace(), Is.SameAs(condensed));
            Assert.That(set.Match(new FontRequest("Missing Family")), Is.Null);
        });
    }

    [Test]
    public void UsesOpticalRangesAfterOtherFaceAttributes()
    {
        FontFace text = CreateOpticalFace("Optical Family", "Text", 8, 14);
        FontFace display = CreateOpticalFace("Optical Family", "Display", 18, 72);
        FontSet set = new FontSet();
        set.Add(text);
        set.Add(display);
        FontRequest textRequest = new FontRequest("Optical Family");
        textRequest.SetOpticalSize(12);
        FontRequest displayRequest = new FontRequest("Optical Family");
        displayRequest.SetOpticalSize(24);
        FontRequest betweenRequest = new FontRequest("Optical Family");
        betweenRequest.SetOpticalSize(15);

        Assert.Multiple(() =>
        {
            Assert.That(set.Match(textRequest)!.GetFace(), Is.SameAs(text));
            Assert.That(set.Match(displayRequest)!.GetFace(), Is.SameAs(display));
            Assert.That(set.Match(betweenRequest)!.GetFace(), Is.SameAs(text));
        });
    }

    [Test]
    public void ResolvesStandardAndExplicitVariableCoordinates()
    {
        string[] tags = new string[] { "wght", "wdth", "opsz", "ital", "slnt", "GRAD" };
        double[] minimums = new double[] { 100, 50, 8, 0, -15, -1 };
        double[] defaults = new double[] { 400, 100, 12, 0, 0, 0 };
        double[] maximums = new double[] { 900, 200, 72, 1, 0, 1 };
        FontFace variable = CreateVariableFace(
            "Variable Family",
            tags,
            minimums,
            defaults,
            maximums);
        FontSet set = new FontSet();
        set.Add(variable);
        FontRequest request = new FontRequest("Variable Family");
        request.SetStyle(FontStyle.Oblique);
        request.SetWeight(650);
        request.SetStretch(80);
        request.SetOpticalSize(20);
        request.SetCoordinate(GradeTag, 0.5);

        FontMatch? match = set.Match(request);
        NormalizedFontCoordinates coordinates = match!.GetCoordinates();

        Assert.Multiple(() =>
        {
            Assert.That(match.GetFace(), Is.SameAs(variable));
            Assert.That(match.GetInstance().GetFace(), Is.SameAs(variable));
            Assert.That(match.GetFamily(), Is.SameAs(set.GetFamily(0)));
            Assert.That(FindUserValue(coordinates, WeightTag), Is.EqualTo(650));
            Assert.That(FindUserValue(coordinates, WidthTag), Is.EqualTo(80));
            Assert.That(FindUserValue(coordinates, OpticalSizeTag), Is.EqualTo(20));
            Assert.That(FindUserValue(coordinates, ItalicTag), Is.Zero);
            Assert.That(FindUserValue(coordinates, SlantTag), Is.EqualTo(-12));
            Assert.That(FindUserValue(coordinates, GradeTag), Is.EqualTo(0.5));
        });

        FontRequest overrideRequest = new FontRequest("Variable Family");
        overrideRequest.SetWeight(200);
        overrideRequest.SetCoordinate(WeightTag, 950);
        FontMatch? overridden = set.Match(overrideRequest);

        Assert.That(FindUserValue(overridden!.GetCoordinates(), WeightTag), Is.EqualTo(900));
    }

    [Test]
    public void RequiresEveryExplicitAxisAndUsesInsertionOrderForExactTies()
    {
        FontFace first = CreateStaticFace("Tie Family", "First", 400, 5, false, false, false);
        FontFace second = CreateStaticFace("Tie Family", "Second", 400, 5, false, false, false);
        FontSet staticSet = new FontSet();
        staticSet.Add(first);
        staticSet.Add(second);
        FontRequest plain = new FontRequest("Tie Family");
        FontRequest unsupported = new FontRequest("Tie Family");
        unsupported.SetCoordinate(GradeTag, 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(staticSet.Match(plain)!.GetFace(), Is.SameAs(first));
            Assert.That(staticSet.Match(unsupported), Is.Null);
        });
    }

    [Test]
    public void ValidatesRequestsAndMatchArguments()
    {
        FontRequest request = new FontRequest("Family");
        request.SetOpticalSize(12);
        request.SetCoordinate(WeightTag, 620);

        Assert.Multiple(() =>
        {
            Assert.That(request.GetFamilyName(), Is.EqualTo("Family"));
            Assert.That(request.GetStyle(), Is.EqualTo(FontStyle.Normal));
            Assert.That(request.GetWeight(), Is.EqualTo(400));
            Assert.That(request.GetStretch(), Is.EqualTo(100));
            Assert.That(request.HasOpticalSize(), Is.True);
            Assert.That(request.GetOpticalSize(), Is.EqualTo(12));
            Assert.That(request.GetCoordinateCount(), Is.EqualTo(1));
            Assert.That(request.HasCoordinate(WeightTag), Is.True);
            Assert.That(request.GetCoordinate(WeightTag), Is.EqualTo(620));
            Assert.Throws<ArgumentNullException>(() => new FontRequest(null!));
            Assert.Throws<ArgumentException>(() => new FontRequest(" \t "));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetStyle((FontStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetWeight(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetWeight(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetStretch(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetStretch(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetOpticalSize(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => request.SetOpticalSize(double.PositiveInfinity));
            Assert.Throws<ArgumentNullException>(() => new FontSet().Match(null!));
        });

        request.ClearOpticalSize();

        Assert.Multiple(() =>
        {
            Assert.That(request.HasOpticalSize(), Is.False);
            Assert.Throws<InvalidOperationException>(() => request.GetOpticalSize());
        });
    }

    internal static FontFace CreateStaticFace(
        string family,
        string style,
        int weight,
        int width,
        bool italic,
        bool bold,
        bool oblique)
    {
        return CreateFace(
            family,
            style,
            weight,
            width,
            italic,
            bold,
            oblique,
            false,
            0,
            0,
            null,
            null,
            null,
            1);
    }

    internal static FontFace CreateStaticFaceWithCmap(
        string family,
        byte[] cmap,
        int glyphCount)
    {
        return CreateFace(
            family,
            "Regular",
            400,
            5,
            false,
            false,
            false,
            false,
            0,
            0,
            null,
            null,
            cmap,
            glyphCount);
    }

    internal static FontFace CloneFaceWithCmap(
        FontFace source,
        string family,
        byte[] cmap)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;
        while (index < source.GetTableCount())
        {
            FontTableInfo table = source.GetTableInfo(index);
            if (table.GetTag() != FontTags.Name &&
                table.GetTag() != FontTags.Cmap)
            {
                tables.Add(CreateTable(table.GetTag(), table.CopyData()));
            }

            index += 1;
        }

        tables.Add(CreateTable(
            FontTags.Name,
            BuildName(family, "Regular", null)));
        tables.Add(CreateTable(FontTags.Cmap, cmap));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict,
            source.GetTableCache().GetLimits());
        return new FontFace(
            source.GetFaceIndex(),
            source.GetScalerType(),
            new SfntValidationResult(
                source.GetOutlineKind(),
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateOpticalFace(
        string family,
        string style,
        double minimumOpticalSize,
        double maximumOpticalSize)
    {
        return CreateFace(
            family,
            style,
            400,
            5,
            false,
            false,
            false,
            true,
            minimumOpticalSize,
            maximumOpticalSize,
            null,
            null,
            null,
            1);
    }

    static FontFace CreateVariableFace(
        string family,
        string[] tags,
        double[] minimums,
        double[] defaults,
        double[] maximums)
    {
        return CreateFace(
            family,
            "Regular",
            400,
            5,
            false,
            false,
            false,
            false,
            0,
            0,
            BuildFvar(tags, minimums, defaults, maximums),
            tags,
            null,
            1);
    }

    static FontFace CreateFace(
        string family,
        string style,
        int weight,
        int width,
        bool italic,
        bool bold,
        bool oblique,
        bool hasOpticalSizes,
        double minimumOpticalSize,
        double maximumOpticalSize,
        byte[]? fvar,
        string[]? axisTags,
        byte[]? cmap,
        int glyphCount)
    {
        int macStyle = (bold ? 1 : 0) | (italic ? 2 : 0);
        int selectionFlags = (italic ? 1 : 0) |
            (bold ? 1 << 5 : 0) |
            (oblique ? 1 << 9 : 0) |
            (!italic && !bold && !oblique ? 1 << 6 : 0);
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Head, BuildHead(macStyle)));
        tables.Add(CreateTable(FontTags.Maxp, BuildMaxp(glyphCount)));
        tables.Add(CreateTable(
            FontTags.Name,
            BuildName(family, style, axisTags)));
        tables.Add(CreateTable(
            FontTags.Os2,
            BuildOs2(
                weight,
                width,
                selectionFlags,
                hasOpticalSizes,
                minimumOpticalSize,
                maximumOpticalSize)));

        if (fvar != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, fvar));
        }

        if (cmap != null)
        {
            tables.Add(CreateTable(FontTags.Cmap, cmap));
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(FontOutlineKind.Cff1, Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildName(string family, string style, string[]? axisTags)
    {
        int axisCount = axisTags == null ? 0 : axisTags.Length;
        int[] nameIds = new int[2 + axisCount];
        int[] languageIds = new int[nameIds.Length];
        string[] values = new string[nameIds.Length];
        nameIds[0] = 16;
        nameIds[1] = 17;
        values[0] = family;
        values[1] = style;
        int index = 0;

        while (index < axisCount)
        {
            nameIds[index + 2] = 256 + index;
            values[index + 2] = axisTags![index];
            index += 1;
        }

        index = 0;

        while (index < languageIds.Length)
        {
            languageIds[index] = 0x0409;
            index += 1;
        }

        return BuildNameTable(nameIds, languageIds, values);
    }

    static byte[] BuildNameTable(int[] nameIds, int[] languageIds, string[] values)
    {
        byte[][] encoded = new byte[values.Length][];
        int storageOffset = 6 + values.Length * 12;
        int stringOffset = 0;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16((ushort)values.Length).WriteUInt16((ushort)storageOffset);
        int index = 0;

        while (index < values.Length)
        {
            encoded[index] = System.Text.Encoding.BigEndianUnicode.GetBytes(values[index]);
            builder.WriteUInt16(3).WriteUInt16(1)
                .WriteUInt16((ushort)languageIds[index])
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)encoded[index].Length)
                .WriteUInt16((ushort)stringOffset);
            stringOffset += encoded[index].Length;
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

    static byte[] BuildFvar(
        string[] tags,
        double[] minimums,
        double[] defaults,
        double[] maximums)
    {
        int axisCount = tags.Length;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(18).WriteUInt16(2)
            .WriteUInt16((ushort)axisCount).WriteUInt16(20)
            .WriteUInt16(0).WriteUInt16((ushort)(4 + axisCount * 4))
            .WriteZeros(2);
        int index = 0;

        while (index < axisCount)
        {
            builder.WriteTag(tags[index])
                .WriteInt32(Fixed(minimums[index]))
                .WriteInt32(Fixed(defaults[index]))
                .WriteInt32(Fixed(maximums[index]))
                .WriteUInt16(0)
                .WriteUInt16((ushort)(256 + index));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildHead(int macStyle)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(-20).WriteInt16(-30).WriteInt16(1000).WriteInt16(900)
            .WriteUInt16((ushort)macStyle).WriteUInt16(9)
            .WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildOs2(
        int weight,
        int width,
        int selectionFlags,
        bool hasOpticalSizes,
        double minimumOpticalSize,
        double maximumOpticalSize)
    {
        int version = hasOpticalSizes ? 5 : 4;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)version).WriteInt16(500)
            .WriteUInt16((ushort)weight).WriteUInt16((ushort)width).WriteUInt16(0)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(100)
            .WriteInt16(600).WriteInt16(600).WriteInt16(0).WriteInt16(400)
            .WriteInt16(50).WriteInt16(250).WriteInt16(0);

        for (int index = 0; index < 10; index += 1)
        {
            builder.WriteUInt8(0);
        }

        builder.WriteUInt32(1).WriteUInt32(0).WriteUInt32(0).WriteUInt32(0)
            .WriteUInt32(0x54455354U).WriteUInt16((ushort)selectionFlags)
            .WriteUInt16(0x20).WriteUInt16(0x7E)
            .WriteInt16(800).WriteInt16(-200).WriteInt16(20)
            .WriteUInt16(900).WriteUInt16(250)
            .WriteUInt32(0).WriteUInt32(0)
            .WriteInt16(450).WriteInt16(700)
            .WriteUInt16(0).WriteUInt16(0x20).WriteUInt16(2);

        if (hasOpticalSizes)
        {
            builder.WriteUInt16((ushort)System.Math.Round(minimumOpticalSize * 20))
                .WriteUInt16((ushort)System.Math.Round(maximumOpticalSize * 20));
        }

        return builder.CopyBytes();
    }

    static int Fixed(double value)
    {
        return System.Convert.ToInt32(System.Math.Round(value * 65536));
    }

    static double FindUserValue(NormalizedFontCoordinates coordinates, int tag)
    {
        int index = 0;

        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == tag)
            {
                return coordinates.GetUserValue(index);
            }

            index += 1;
        }

        throw new InvalidOperationException("The resolved axis was not found.");
    }
}
