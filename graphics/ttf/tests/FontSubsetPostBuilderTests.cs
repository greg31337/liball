using ttf;

namespace ttf.Tests;

internal class FontSubsetPostBuilderTests
{
    [Test]
    public void RebuildsNamesInCompactGlyphOrderAndRetainsMetadata()
    {
        FontFace face = CreatePostFace(BuildNamedPost());
        SfntPostTable? table = Parse(BuildSubset(
            face,
            FontSubsetMetadataPolicy.RetainSafe,
            FontSubsetGlyphRetentionMode.Compact).CreatePostData(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version2));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(table.GetGlyphName(0), Is.EqualTo(".notdef"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("A.alt"));
            Assert.That(table.GetCustomNameCount(), Is.EqualTo(1));
            Assert.That(table.GetItalicAngle(), Is.EqualTo(-12.5));
            Assert.That(table.GetUnderlinePosition(), Is.EqualTo(-75));
            Assert.That(table.GetUnderlineThickness(), Is.EqualTo(50));
            Assert.That(table.GetFixedPitchRaw(), Is.EqualTo(2U));
            Assert.That(table.GetMinimumType42Memory(), Is.EqualTo(100U));
            Assert.That(table.GetMaximumType1Memory(), Is.EqualTo(400U));
        });
    }

    [Test]
    public void MinimalMetadataDropsNamesButRetainsHeaderMetrics()
    {
        FontFace face = CreatePostFace(BuildNamedPost());
        SfntPostTable? table = Parse(BuildSubset(
            face,
            FontSubsetMetadataPolicy.Minimal,
            FontSubsetGlyphRetentionMode.Compact).CreatePostData(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version3));
            Assert.That(table.ProvidesGlyphNames(), Is.False);
            Assert.That(table.GetGlyphName(0), Is.EqualTo("gid0000"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("gid0001"));
            Assert.That(table.GetItalicAngle(), Is.EqualTo(-12.5));
            Assert.That(table.GetUnderlinePosition(), Is.EqualTo(-75));
            Assert.That(table.GetUnderlineThickness(), Is.EqualTo(50));
            Assert.That(table.GetFixedPitchRaw(), Is.EqualTo(2U));
        });
    }

    [Test]
    public void PreserveGidModeNamesUnassignedSlotsDeterministically()
    {
        FontFace face = CreatePostFace(BuildNamedPost());
        SfntPostTable? table = Parse(BuildSubset(
            face,
            FontSubsetMetadataPolicy.RetainSafe,
            FontSubsetGlyphRetentionMode.Preserve).CreatePostData(), 4);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetGlyphName(0), Is.EqualTo(".notdef"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("gid0001"));
            Assert.That(table.GetGlyphName(2), Is.EqualTo("gid0002"));
            Assert.That(table.GetGlyphName(3), Is.EqualTo("A.alt"));
            Assert.That(table.GetCustomNameCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void RetainSafeDoesNotInventNamesForVersionThreeSource()
    {
        FontFace face = CreatePostFace(
            BuildHeader(0x00030000U).CopyBytes());
        SfntPostTable? table = Parse(BuildSubset(
            face,
            FontSubsetMetadataPolicy.RetainSafe,
            FontSubsetGlyphRetentionMode.Compact).CreatePostData(), 2);

        Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version3));
    }

    [Test]
    public void AppliesInstanceUnderlineVariations()
    {
        FontFace face = FontInstanceMvarTests.CreateFace(true);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("wght"), 0.5);
        FontInstance instance = face.CreateInstance(coordinates);
        byte[]? post = FontSubsetPostBuilder.Create(
            instance,
            new int[] { 0 },
            FontSubsetMetadataPolicy.Minimal);
        SfntPostTable? table = Parse(post, 1);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetUnderlinePosition(), Is.EqualTo(-90));
            Assert.That(table.GetUnderlineThickness(), Is.EqualTo(60));
        });
    }

    [Test]
    public void ResolvesItalicAngleFromSlantAxis()
    {
        FontFace face = CreatePostFace(BuildNamedPost());
        int slantTag = (int)TestSfntEncoding.EncodeTag("slnt");
        NormalizedFontCoordinates coordinates =
            new NormalizedFontCoordinates(
                new int[] { slantTag },
                new double[] { -10.5 },
                new double[] { 0 },
                new int[] { 0 },
                "slnt=-10.5",
                -1,
                false);
        FontInstance instance = new FontInstance(face, coordinates);
        byte[]? post = FontSubsetPostBuilder.Create(
            instance,
            new int[] { 0 },
            FontSubsetMetadataPolicy.Minimal);
        SfntPostTable? table = Parse(post, 1);

        Assert.That(table!.GetItalicAngle(), Is.EqualTo(-10.5));
    }

    [Test]
    public void ReturnsNullWhenSourceHasNoPostTable()
    {
        FontSubsetBuilder builder = new FontSubsetBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            CreateOptions(
                FontSubsetMetadataPolicy.RetainSafe,
                FontSubsetGlyphRetentionMode.Compact));
        builder.AddGlyph(3);

        Assert.That(builder.Build().CreatePostData(), Is.Null);
    }

    static FontSubset BuildSubset(
        FontFace face,
        FontSubsetMetadataPolicy metadataPolicy,
        FontSubsetGlyphRetentionMode retentionMode)
    {
        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            CreateOptions(metadataPolicy, retentionMode));
        builder.AddGlyph(3);
        return builder.Build();
    }

    static FontSubsetOptions CreateOptions(
        FontSubsetMetadataPolicy metadataPolicy,
        FontSubsetGlyphRetentionMode retentionMode)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetMetadataPolicy(metadataPolicy);
        options.SetGlyphRetentionMode(retentionMode);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        return options;
    }

    static FontFace CreatePostFace(byte[] post)
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            tables.Add(source.GetTable(index));
            index++;
        }

        AddTable(tables, FontTags.Post, post);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            sourceFace.GetScalerType(),
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                System.Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntPostTable? Parse(byte[]? post, int glyphCount)
    {
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        AddTable(tables, FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes());
        AddTable(tables, FontTags.Post, post);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return SfntPostTableParser.FindTable(cache);
    }

    static byte[] BuildNamedPost()
    {
        TestBinaryBuilder builder = BuildHeader(0x00020000U)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(36)
            .WriteUInt16(258)
            .WriteUInt16(259);
        WriteName(builder, "custom_name");
        WriteName(builder, "A.alt");
        return builder.CopyBytes();
    }

    static TestBinaryBuilder BuildHeader(uint version)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(version)
            .WriteInt32(-819200)
            .WriteInt16(-75)
            .WriteInt16(50)
            .WriteUInt32(2)
            .WriteUInt32(100)
            .WriteUInt32(200)
            .WriteUInt32(300)
            .WriteUInt32(400);
    }

    static void WriteName(TestBinaryBuilder builder, string value)
    {
        builder.WriteUInt8((byte)value.Length);
        int index = 0;
        while (index < value.Length)
        {
            builder.WriteUInt8((byte)value[index]);
            index++;
        }
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[]? bytes)
    {
        FontData data = FontData.Create(bytes!);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }
}
