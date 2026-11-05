using ttf;

namespace ttf.Tests;

internal class FontSubsetBuilderTests
{
    [Test]
    public void BindsInstanceAndSnapshotsOptions()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetSubsetPrefixOverride("ABCDEF");
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);
        options.SetColorPolicy(FontSubsetColorPolicy.Retain);
        options.SetSubsetPrefixOverride("UVWXYZ");
        FontSubsetOptions snapshot = builder.CopyOptions();
        snapshot.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        snapshot.SetSubsetPrefixOverride("GHIJKL");

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInstance(), Is.SameAs(instance));
            Assert.That(builder.CopyOptions().GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Drop));
            Assert.That(builder.CopyOptions().GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Drop));
            Assert.That(builder.CopyOptions().GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.Throws<ArgumentNullException>(() =>
                new FontSubsetBuilder(null!, options));
            Assert.Throws<ArgumentNullException>(() =>
                new FontSubsetBuilder(instance, null!));
        });
    }

    [Test]
    public void CollectsAndDeduplicatesEverySetLikeInput()
    {
        FontInstance instance = CreatePaletteFace().CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        int liga = (int)TestSfntEncoding.EncodeTag("liga");

        builder.AddCodePoint('A');
        builder.AddCodePoint('A');
        builder.AddText("A\U0001F600");
        builder.AddText(string.Empty);
        builder.AddGlyph(2);
        builder.AddGlyph(2);
        builder.AddFeature(liga);
        builder.AddFeature(liga);
        builder.AddPalette(1);
        builder.AddPalette(1);

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetCodePointCount(), Is.EqualTo(2));
            Assert.That(builder.GetCodePoint(0), Is.EqualTo('A'));
            Assert.That(builder.GetCodePoint(1), Is.EqualTo(0x1F600));
            Assert.That(builder.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(builder.GetGlyph(0), Is.EqualTo(2));
            Assert.That(builder.GetFeatureCount(), Is.EqualTo(1));
            Assert.That(builder.GetFeature(0), Is.EqualTo(liga));
            Assert.That(builder.GetPaletteCount(), Is.EqualTo(1));
            Assert.That(builder.GetPalette(0), Is.EqualTo(1));
            Assert.That(builder.GetUnicodeSequenceCount(), Is.EqualTo(2));
            Assert.That(builder.CopyUnicodeSequence(0),
                Is.EqualTo(new int[] { 'A' }));
            Assert.That(builder.CopyUnicodeSequence(1),
                Is.EqualTo(new int[] { 'A', 0x1F600 }));
        });
    }

    [Test]
    public void CollectsGlyphsAndExactUnicodeFromShapedRuns()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.Create());
        ShapedRun run = CreateLigatureRun(1, "fi");

        builder.AddShapedRun(run);
        builder.AddShapedRun(run);
        FontSubsetGlyphRequest request = builder.GetShapedGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(builder.GetGlyph(0), Is.EqualTo(1));
            Assert.That(builder.GetCodePointCount(), Is.EqualTo(2));
            Assert.That(builder.GetCodePoint(0), Is.EqualTo('f'));
            Assert.That(builder.GetCodePoint(1), Is.EqualTo('i'));
            Assert.That(builder.GetUnicodeSequenceCount(), Is.EqualTo(1));
            Assert.That(builder.CopyUnicodeSequence(0),
                Is.EqualTo(new int[] { 'f', 'i' }));
            Assert.That(builder.GetShapedGlyphCount(), Is.EqualTo(1));
            Assert.That(request.GetGlyphId(), Is.EqualTo(1));
            Assert.That(request.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 'f', 'i' }));
        });
    }

    [Test]
    public void AddsExactUnicodeAndGlyphToSequenceInputsDefensively()
    {
        FontInstance instance = CreateMappedSubsetFace()
            .CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        int[] exactSequence = new int[] { 'B', 'A' };
        int[] glyphSequence = new int[] { 'f', 'i' };

        builder.AddUnicodeSequence(exactSequence);
        builder.AddUnicodeSequence(exactSequence);
        builder.AddGlyphUnicodeSequence(7, glyphSequence);
        builder.AddGlyphUnicodeSequence(7, glyphSequence);
        exactSequence[0] = 'x';
        glyphSequence[0] = 'y';
        FontSubset subset = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(CopyCodePoints(subset),
                Is.EqualTo(new int[] { 'A', 'B', 'f', 'i' }));
            Assert.That(subset.GetUnicodeSequenceCount(), Is.EqualTo(2));
            Assert.That(subset.CopyUnicodeSequence(0),
                Is.EqualTo(new int[] { 'B', 'A' }));
            Assert.That(subset.CopyUnicodeSequence(1),
                Is.EqualTo(new int[] { 'f', 'i' }));
            Assert.That(subset.GetShapedGlyphCount(), Is.EqualTo(1));
            Assert.That(subset.GetShapedGlyph(0).GetGlyphId(), Is.EqualTo(7));
            Assert.That(subset.GetShapedGlyph(0).CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 'f', 'i' }));
            Assert.That(subset.GetUnicodeMappingCount(), Is.EqualTo(3));
            AssertUnicodeMapping(subset, 0, 4, new int[] { 'A' });
            AssertUnicodeMapping(subset, 1, 5, new int[] { 'B' });
            AssertUnicodeMapping(subset, 2, 7, new int[] { 'f', 'i' });
        });
    }

    [Test]
    public void ValidatesExactUnicodeSequenceInputsBeforeAddingThem()
    {
        FontSubsetBuilder builder = new FontSubsetBuilder(
            CreateMappedSubsetFace().CreateDefaultInstance(),
            FontSubsetOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                builder.AddUnicodeSequence(null!));
            Assert.Throws<ArgumentException>(() =>
                builder.AddUnicodeSequence(Array.Empty<int>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddUnicodeSequence(new int[] { 0xD800 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddGlyphUnicodeSequence(
                    8,
                    new int[] { 'a' }));
            Assert.Throws<ArgumentNullException>(() =>
                builder.AddGlyphUnicodeSequence(1, null!));
            Assert.Throws<ArgumentException>(() =>
                builder.AddGlyphUnicodeSequence(1, Array.Empty<int>()));
            Assert.That(builder.GetCodePointCount(), Is.Zero);
            Assert.That(builder.GetGlyphCount(), Is.Zero);
            Assert.That(builder.GetUnicodeSequenceCount(), Is.Zero);
        });
    }

    [Test]
    public void RejectsInvalidInputsBeforeAddingShapedRunRequests()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.Create());
        ShapedRun foreignRun = CreateLigatureRun(4, "fi");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddCodePoint(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddCodePoint(0xD800));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddCodePoint(0x110000));
            Assert.Throws<ArgumentNullException>(() => builder.AddText(null!));
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                builder.AddText("\uD800"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddGlyph(4));
            Assert.Throws<ArgumentException>(() => builder.AddFeature(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AddPalette(0));
            Assert.Throws<ArgumentNullException>(() =>
                builder.AddShapedRun(null!));
            Assert.Throws<ArgumentException>(() =>
                builder.AddShapedRun(foreignRun));
            Assert.That(builder.GetGlyphCount(), Is.Zero);
            Assert.That(builder.GetCodePointCount(), Is.Zero);
            Assert.That(builder.GetUnicodeSequenceCount(), Is.Zero);
        });
    }

    [Test]
    public void BuildSortsEverySetLikeInputDeterministically()
    {
        FontInstance instance = CreatePaletteFace().CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        int aalt = (int)TestSfntEncoding.EncodeTag("aalt");
        int kern = (int)TestSfntEncoding.EncodeTag("kern");
        int liga = (int)TestSfntEncoding.EncodeTag("liga");
        builder.AddCodePoint(0x1F600);
        builder.AddCodePoint('z');
        builder.AddCodePoint('a');
        builder.AddText("ba");
        builder.AddGlyph(4);
        builder.AddGlyph(1);
        builder.AddGlyph(3);
        builder.AddFeature(liga);
        builder.AddFeature(aalt);
        builder.AddFeature(kern);
        builder.AddPalette(1);
        builder.AddPalette(0);

        FontSubset subset = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(CopyCodePoints(subset),
                Is.EqualTo(new int[] { 'a', 'b', 'z', 0x1F600 }));
            Assert.That(CopyGlyphs(subset),
                Is.EqualTo(new int[] { 0, 1, 3, 4 }));
            Assert.That(CopyFeatures(subset),
                Is.EqualTo(new int[] { aalt, kern, liga }));
            Assert.That(CopyPalettes(subset),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(subset.GetUnicodeSequenceCount(), Is.EqualTo(4));
            Assert.That(subset.CopyUnicodeSequence(0),
                Is.EqualTo(new int[] { 'a' }));
            Assert.That(subset.CopyUnicodeSequence(1),
                Is.EqualTo(new int[] { 'b', 'a' }));
            Assert.That(subset.CopyUnicodeSequence(2),
                Is.EqualTo(new int[] { 'z' }));
            Assert.That(subset.CopyUnicodeSequence(3),
                Is.EqualTo(new int[] { 0x1F600 }));
        });
    }

    [Test]
    public void BuildSortsShapedGlyphMappingsByGlyphAndSequence()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.Create());
        builder.AddShapedRun(CreateLigatureRun(3, "zz"));
        builder.AddShapedRun(CreateLigatureRun(1, "bb"));
        builder.AddShapedRun(CreateLigatureRun(1, "aa"));

        FontSubset subset = builder.Build();
        FontSubsetGlyphRequest first = subset.GetShapedGlyph(0);
        FontSubsetGlyphRequest second = subset.GetShapedGlyph(1);
        FontSubsetGlyphRequest third = subset.GetShapedGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(subset.GetShapedGlyphCount(), Is.EqualTo(3));
            Assert.That(first.GetGlyphId(), Is.EqualTo(1));
            Assert.That(first.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 'a', 'a' }));
            Assert.That(second.GetGlyphId(), Is.EqualTo(1));
            Assert.That(second.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 'b', 'b' }));
            Assert.That(third.GetGlyphId(), Is.EqualTo(3));
            Assert.That(third.CopyUnicodeSequence(),
                Is.EqualTo(new int[] { 'z', 'z' }));
        });
    }

    [Test]
    public void BuildIsIdempotentAndResultCopiesRemainImmutable()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetOptions options = FontSubsetOptions.Create();
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);
        builder.AddText("ba");
        FontSubset first = builder.Build();
        FontSubset second = builder.Build();
        int[] sequence = first.CopyUnicodeSequence(0);
        sequence[0] = 'x';
        FontSubsetOptions copiedOptions = first.CopyOptions();
        copiedOptions.SetColorPolicy(FontSubsetColorPolicy.Retain);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.GetInstance(), Is.SameAs(instance));
            Assert.That(first.CopyUnicodeSequence(0),
                Is.EqualTo(new int[] { 'b', 'a' }));
            Assert.That(first.CopyOptions().GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Drop));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                first.GetCodePoint(2));
        });
    }

    [Test]
    public void RejectsEveryAdditionAfterBuild()
    {
        FontInstance instance = CreatePaletteFace().CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.Create());
        ShapedRun run = CreateLigatureRun(1, "fi");
        int liga = (int)TestSfntEncoding.EncodeTag("liga");
        builder.Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddCodePoint('a'));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddText("a"));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddUnicodeSequence(new int[] { 'a' }));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddGlyph(1));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddGlyphUnicodeSequence(1, new int[] { 'a' }));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddShapedRun(run));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddFeature(liga));
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddPalette(0));
        });
    }

    [Test]
    public void SeedsNotdefAndRecommendedControlAndSpaceGlyphsByPolicy()
    {
        FontInstance instance = CreateMappedSubsetFace()
            .CreateDefaultInstance();
        FontSubset compatible = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateCompatible()).Build();
        FontSubset strict = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict()).Build();

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphs(compatible),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 6 }));
            Assert.That(compatible.GetUnicodeMappingCount(), Is.EqualTo(3));
            Assert.That(compatible.GetClosureReport().GetRecommendedGlyphIds(),
                Is.EqualTo(new int[] { 1, 2, 3, 6 }));
            AssertUnicodeMapping(compatible, 0, 1, new int[] { 0x0000 });
            AssertUnicodeMapping(compatible, 1, 2, new int[] { 0x000D });
            AssertUnicodeMapping(compatible, 2, 6, new int[] { 0x0020 });
            Assert.That(CopyGlyphs(strict), Is.EqualTo(new int[] { 0 }));
            Assert.That(strict.GetClosureReport().GetRecommendedGlyphIds(),
                Is.Empty);
            Assert.That(strict.GetUnicodeMappingCount(), Is.Zero);
        });
    }

    [Test]
    public void SeedsRequestedNominalVariationRawAndExactShapedMappings()
    {
        FontInstance instance = CreateMappedSubsetFace()
            .CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        builder.AddText("ABZ");
        builder.AddText("A\uFE0F");
        builder.AddGlyph(7);
        builder.AddShapedRun(CreateLigatureRun(7, "fi"));

        FontSubset subset = builder.Build();
        int[] copied = subset.GetUnicodeMapping(3).CopyUnicodeSequence();
        copied[0] = 'x';

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphs(subset),
                Is.EqualTo(new int[] { 0, 4, 5, 7 }));
            Assert.That(subset.GetUnicodeMappingCount(), Is.EqualTo(4));
            AssertUnicodeMapping(subset, 0, 4, new int[] { 'A' });
            AssertUnicodeMapping(subset, 1, 5, new int[] { 'B' });
            AssertUnicodeMapping(
                subset,
                2,
                7,
                new int[] { 'A', 0xFE0F });
            AssertUnicodeMapping(
                subset,
                3,
                7,
                new int[] { 'f', 'i' });
        });
    }

    [Test]
    public void AddsNestedTrueTypeComponentsOnceInSortedClosure()
    {
        FontInstance instance = CreateCompositeSubsetFace(
            new int[][]
            {
                Array.Empty<int>(),
                Array.Empty<int>(),
                new int[] { 1, 0 },
                new int[] { 2, 1, 2 },
                new int[] { 2 }
            },
            3).CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        builder.AddGlyph(4);
        builder.AddGlyph(3);

        FontSubset subset = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphs(subset),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
            Assert.That(subset.GetClosureReport().GetCompositeGlyphIds(),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
            Assert.That(subset.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void RetainsCycleMembersAndReportsDeterministicDiagnostic()
    {
        FontInstance instance = CreateCompositeSubsetFace(
            new int[][]
            {
                Array.Empty<int>(),
                new int[] { 2 },
                new int[] { 1 }
            },
            4).CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        builder.AddGlyph(1);

        FontSubset subset = builder.Build();
        FontDiagnostic?[] diagnostics = subset.GetDiagnostics();
        FontDiagnostic? diagnostic = diagnostics[0];
        diagnostics[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphs(subset),
                Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(subset.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(subset.GetDiagnostics()[0], Is.SameAs(diagnostic));
            Assert.That(diagnostic!.GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(FontDiagnosticCode.CompositeCycle));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Glyf));
            Assert.That(diagnostic.GetLogicalPath(),
                Is.EqualTo("subset.glyf.glyph[2].component[0]"));
            Assert.That(diagnostic.GetMessage(), Does.Contain("glyph 1"));
        });
    }

    [Test]
    public void ReportsDeclaredDepthMismatchWhileCompletingBoundedClosure()
    {
        FontInstance instance = CreateCompositeSubsetFace(
            new int[][]
            {
                Array.Empty<int>(),
                Array.Empty<int>(),
                new int[] { 1 },
                new int[] { 2 }
            },
            1).CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            instance,
            FontSubsetOptions.CreateStrict());
        builder.AddGlyph(3);

        FontSubset subset = builder.Build();
        FontDiagnostic diagnostic = subset.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphs(subset),
                Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(subset.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(FontDiagnosticCode.CompositeDepthExceeded));
            Assert.That(diagnostic.GetLogicalPath(),
                Is.EqualTo("subset.glyf.glyph[2]"));
        });
    }

    static void AssertUnicodeMapping(
        FontSubset subset,
        int index,
        int glyphId,
        int[] unicodeSequence)
    {
        FontSubsetUnicodeMapping mapping = subset.GetUnicodeMapping(index);
        Assert.That(mapping.GetGlyphId(), Is.EqualTo(glyphId));
        Assert.That(mapping.CopyUnicodeSequence(),
            Is.EqualTo(unicodeSequence));
    }

    static int[] CopyCodePoints(FontSubset subset)
    {
        int[] values = new int[subset.GetCodePointCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = subset.GetCodePoint(index);
            index++;
        }

        return values;
    }

    static int[] CopyGlyphs(FontSubset subset)
    {
        int[] values = new int[subset.GetGlyphCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = subset.GetGlyph(index);
            index++;
        }

        return values;
    }

    static int[] CopyFeatures(FontSubset subset)
    {
        int[] values = new int[subset.GetFeatureCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = subset.GetFeature(index);
            index++;
        }

        return values;
    }

    static int[] CopyPalettes(FontSubset subset)
    {
        int[] values = new int[subset.GetPaletteCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = subset.GetPalette(index);
            index++;
        }

        return values;
    }

    static ShapedRun CreateLigatureRun(int glyphId, string text)
    {
        RuneBuffer source = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        ShapingBuffer buffer = new ShapingBuffer(
            source,
            ShapingDirection.LeftToRight);
        buffer.SetGlyphId(0, glyphId);
        buffer.SetGlyphId(1, glyphId);
        buffer.MergeClusters(0, 2);
        buffer.SetLigature(0, 1, 2, 0);
        buffer.Remove(1);
        return ShapedRunBuilder.Build(buffer, source);
    }

    static FontFace CreatePaletteFace()
    {
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Maxp, BuildMaxp()),
            CreateTable(FontTags.Cpal, BuildCpal())
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateMappedSubsetFace()
    {
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Maxp, BuildMaxp(8)),
            CreateTable(FontTags.Cmap, BuildSubsetCmap())
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static FontFace CreateCompositeSubsetFace(
        int[][] componentGlyphIds,
        int maximumDepth)
    {
        byte[][] glyphs = new byte[componentGlyphIds.Length][];
        int maximumComponents = 0;
        int index = 0;

        while (index < glyphs.Length)
        {
            int[] components = componentGlyphIds[index];
            glyphs[index] = components.Length == 0 ?
                Array.Empty<byte>() :
                BuildSubsetCompositeGlyph(components);

            if (components.Length > maximumComponents)
            {
                maximumComponents = components.Length;
            }

            index++;
        }

        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Head, BuildSubsetHead()),
            CreateTable(
                FontTags.Maxp,
                BuildTrueTypeMaxp(
                    glyphs.Length,
                    maximumComponents,
                    maximumDepth)),
            CreateTable(FontTags.Loca, BuildSubsetLoca(glyphs)),
            CreateTable(FontTags.Glyf, JoinGlyphs(glyphs))
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
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

    static byte[] BuildMaxp()
    {
        return BuildMaxp(5);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildSubsetCmap()
    {
        byte[] nominal = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(76)
            .WriteUInt32(0)
            .WriteUInt32(5)
            .WriteUInt32(0x0000).WriteUInt32(0x0000).WriteUInt32(1)
            .WriteUInt32(0x000D).WriteUInt32(0x000D).WriteUInt32(2)
            .WriteUInt32(0x0020).WriteUInt32(0x0020).WriteUInt32(6)
            .WriteUInt32(0x0041).WriteUInt32(0x0041).WriteUInt32(4)
            .WriteUInt32(0x0042).WriteUInt32(0x0042).WriteUInt32(5)
            .CopyBytes();
        byte[] variation = new TestBinaryBuilder()
            .WriteUInt16(14)
            .WriteUInt32(30)
            .WriteUInt32(1)
            .WriteUInt24(0xFE0F)
            .WriteUInt32(0)
            .WriteUInt32(21)
            .WriteUInt32(1)
            .WriteUInt24(0x0041)
            .WriteUInt16(7)
            .CopyBytes();
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(0).WriteUInt16(5).WriteUInt32(96)
            .WriteUInt16(3).WriteUInt16(10).WriteUInt32(20)
            .WriteBytes(nominal)
            .WriteBytes(variation)
            .CopyBytes();
    }

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(2)
            .WriteUInt32(16)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(4).WriteUInt8(5).WriteUInt8(6).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildSubsetCompositeGlyph(int[] componentGlyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(-1)
            .WriteInt16(0).WriteInt16(0)
            .WriteInt16(0).WriteInt16(0);
        int index = 0;

        while (index < componentGlyphIds.Length)
        {
            int flags = 0x0002;

            if (index + 1 < componentGlyphIds.Length)
            {
                flags |= 0x0020;
            }

            builder
                .WriteUInt16((ushort)flags)
                .WriteUInt16((ushort)componentGlyphIds[index])
                .WriteInt8(0)
                .WriteInt8(0);
            index++;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildSubsetHead()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32(0x00010000U).WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U).WriteUInt16(0).WriteUInt16(1000)
            .WriteInt64(0).WriteInt64(0)
            .WriteInt16(0).WriteInt16(0).WriteInt16(0).WriteInt16(0)
            .WriteUInt16(0).WriteUInt16(8)
            .WriteInt16(2).WriteInt16(0).WriteInt16(0)
            .CopyBytes();
    }

    static byte[] BuildTrueTypeMaxp(
        int glyphCount,
        int maximumComponents,
        int maximumDepth)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)maximumComponents)
            .WriteUInt16((ushort)maximumDepth)
            .CopyBytes();
    }

    static byte[] BuildSubsetLoca(byte[][] glyphs)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        int offset = 0;
        builder.WriteUInt16(0);
        int index = 0;

        while (index < glyphs.Length)
        {
            offset += glyphs[index].Length;
            builder.WriteUInt16((ushort)(offset / 2));
            index++;
        }

        return builder.CopyBytes();
    }

    static byte[] JoinGlyphs(byte[][] glyphs)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        int index = 0;

        while (index < glyphs.Length)
        {
            builder.WriteBytes(glyphs[index]);
            index++;
        }

        return builder.CopyBytes();
    }
}
