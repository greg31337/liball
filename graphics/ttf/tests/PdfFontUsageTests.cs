using ttf;

namespace ttf.Tests;

internal class PdfFontUsageTests
{
    [Test]
    public void EquivalentFacesAndOptionsShareDocumentUsage()
    {
        byte[] bytes = FontToolsFreeTypeDifferentialTests.GetOracleFont();
        FontInstance first = Load(bytes).CreateDefaultInstance();
        FontInstance equivalent = Load(bytes).CreateDefaultInstance();
        PdfFontUsageAccumulator accumulator =
            new PdfFontUsageAccumulator();
        PdfFontOptions options = PdfFontOptions.Create();

        PdfFontUsage firstUsage = accumulator.GetOrAdd(first, options);
        PdfFontUsage secondUsage = accumulator.GetOrAdd(
            equivalent,
            PdfFontOptions.Create());

        Assert.Multiple(() =>
        {
            Assert.That(secondUsage, Is.SameAs(firstUsage));
            Assert.That(accumulator.GetCount(), Is.EqualTo(1));
            Assert.That(firstUsage.GetInstance(), Is.SameAs(first));
            Assert.That(firstUsage.GetKey(), Is.EqualTo(secondUsage.GetKey()));
        });
    }

    [Test]
    public void CoordinatesWritingModeAndSubsetPolicySeparateUsage()
    {
        FontFace face = VariableStaticInstanceDifferentialTests
            .GetVariableFontFace();
        FontCoordinates boldCoordinates = new FontCoordinates();
        boldCoordinates.Set(Tag("wght"), 700);
        FontInstance regular = face.CreateDefaultInstance();
        FontInstance bold = face.CreateInstance(boldCoordinates);
        PdfFontUsageAccumulator accumulator =
            new PdfFontUsageAccumulator();
        PdfFontOptions horizontalSubset = PdfFontOptions.Create();
        PdfFontOptions verticalSubset = PdfFontOptions.Create();
        verticalSubset.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontOptions horizontalFull = PdfFontOptions.Create();
        horizontalFull.SetEmbeddingMode(PdfFontEmbeddingMode.FullFont);
        PdfFontOptions archival = PdfFontOptions.Create();
        archival.SetArchivalProfile(PdfArchivalProfile.PdfA1);

        PdfFontUsage first = accumulator.GetOrAdd(
            regular,
            horizontalSubset);
        PdfFontUsage coordinate = accumulator.GetOrAdd(
            bold,
            horizontalSubset);
        PdfFontUsage writing = accumulator.GetOrAdd(
            regular,
            verticalSubset);
        PdfFontUsage subsetPolicy = accumulator.GetOrAdd(
            regular,
            horizontalFull);
        PdfFontUsage compatibility = accumulator.GetOrAdd(
            regular,
            archival);

        Assert.Multiple(() =>
        {
            Assert.That(accumulator.GetCount(), Is.EqualTo(5));
            Assert.That(coordinate, Is.Not.SameAs(first));
            Assert.That(writing, Is.Not.SameAs(first));
            Assert.That(subsetPolicy, Is.Not.SameAs(first));
            Assert.That(compatibility, Is.Not.SameAs(first));
            Assert.That(accumulator.GetUsage(0), Is.SameAs(first));
            Assert.That(accumulator.GetUsage(4), Is.SameAs(compatibility));
        });
    }

    [Test]
    public void AccumulatesSortedGlyphsAndExactUnicodeSequences()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator()
            .GetOrAdd(instance, PdfFontOptions.Create());
        usage.AddText("iA");
        usage.AddGlyph(4, Array.Empty<int>());
        usage.AddGlyph(6, new int[] { 0x66, 0x69 });
        usage.AddGlyph(6, new int[] { 0x66, 0x69 });
        usage.AddShapedRun(CreateShapedRun());

        PdfFontUsageMapping[] mappings = usage.CopyMappings();

        Assert.Multiple(() =>
        {
            Assert.That(usage.CopyGlyphIds(),
                Is.EqualTo(new int[] { 0, 2, 4, 5, 6, 7 }));
            Assert.That(mappings, Has.Length.EqualTo(4));
            AssertMapping(mappings[0], 2, new int[] { 0x41 });
            AssertMapping(mappings[1], 5, new int[] { 0x69 });
            AssertMapping(mappings[2], 6, new int[] { 0x66, 0x69 });
            AssertMapping(mappings[3], 7, new int[] { 0x301 });
        });
    }

    [Test]
    public void SnapshotsOptionsAndReturnsDefensiveUsageCopies()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            HarfBuzzLayoutDifferentialTests.Load().CreateDefaultInstance(),
            options);
        options.SetWritingMode(PdfWritingMode.Horizontal);
        PdfFontOptions snapshot = usage.CopyOptions();
        int[] glyphs = usage.CopyGlyphIds();
        glyphs[0] = 99;
        snapshot.SetWritingMode(PdfWritingMode.Horizontal);

        Assert.Multiple(() =>
        {
            Assert.That(usage.CopyOptions().GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Vertical));
            Assert.That(usage.CopyGlyphIds(), Is.EqualTo(new int[] { 0 }));
        });
    }

    [Test]
    public void RejectsInvalidInputsWithoutChangingUsage()
    {
        PdfFontUsageAccumulator accumulator =
            new PdfFontUsageAccumulator();
        PdfFontUsage usage = accumulator.GetOrAdd(
            HarfBuzzLayoutDifferentialTests.Load().CreateDefaultInstance(),
            PdfFontOptions.Create());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                accumulator.GetOrAdd(null!, PdfFontOptions.Create()));
            Assert.Throws<ArgumentNullException>(() =>
                accumulator.GetOrAdd(usage.GetInstance(), null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                accumulator.GetUsage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                usage.AddGlyph(-1, Array.Empty<int>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                usage.AddGlyph(14, Array.Empty<int>()));
            Assert.Throws<ArgumentNullException>(() =>
                usage.AddGlyph(1, null!));
            Assert.Throws<ArgumentException>(() =>
                usage.AddGlyph(1, new int[] { 0xD800 }));
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                usage.AddText("\uD800"));
            Assert.Throws<ArgumentNullException>(() => usage.AddText(null!));
            Assert.Throws<ArgumentNullException>(() =>
                usage.AddShapedRun(null!));
        });

        Assert.That(usage.CopyGlyphIds(), Is.EqualTo(new int[] { 0 }));
    }

    [Test]
    public void FrozenUsageRejectsEveryMutationAndKeepsItsSnapshot()
    {
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            HarfBuzzLayoutDifferentialTests.Load().CreateDefaultInstance(),
            PdfFontOptions.Create());
        usage.AddGlyph(4, new int[] { 0x42 });
        usage.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(usage.IsFrozen(), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                usage.AddText("A"));
            Assert.Throws<InvalidOperationException>(() =>
                usage.AddGlyph(5, new int[] { 0x69 }));
            Assert.Throws<InvalidOperationException>(() =>
                usage.AddShapedRun(CreateShapedRun()));
            Assert.That(usage.CopyGlyphIds(),
                Is.EqualTo(new int[] { 0, 4 }));
            Assert.That(usage.CopyMappings(), Has.Length.EqualTo(1));
        });
    }

    static ShapedRun CreateShapedRun()
    {
        ShapedGlyph first = new ShapedGlyph(
            6,
            0,
            0,
            2,
            new int[] { 0x66, 0x69 },
            1,
            0,
            0,
            0,
            ShapedGlyphFlags.Ligature,
            600,
            0,
            0,
            0);
        ShapedGlyph second = new ShapedGlyph(
            7,
            1,
            2,
            1,
            new int[] { 0x301 },
            1,
            0,
            1,
            1,
            ShapedGlyphFlags.MarkAttachment,
            0,
            0,
            0,
            0);
        ShapedCluster firstCluster = new ShapedCluster(
            0,
            2,
            new int[] { 0x66, 0x69 },
            new int[] { 0 },
            0,
            0,
            false);
        ShapedCluster secondCluster = new ShapedCluster(
            2,
            1,
            new int[] { 0x301 },
            new int[] { 1 },
            1,
            1,
            false);
        return new ShapedRun(
            new ShapedGlyph[] { first, second },
            new ShapedCluster[] { firstCluster, secondCluster },
            new int[] { 0, 1 },
            new int[] { 0, 1 },
            new int[] { 0, 1 },
            new int[] { 0, 1 },
            3,
            ShapingDirection.LeftToRight);
    }

    static void AssertMapping(
        PdfFontUsageMapping mapping,
        int glyphId,
        int[] sequence)
    {
        Assert.That(mapping.GetGlyphId(), Is.EqualTo(glyphId));
        Assert.That(mapping.CopyUnicodeSequence(), Is.EqualTo(sequence));
    }

    static FontFace Load(byte[] bytes)
    {
        return FontLoader.Load(bytes, FontLoadOptions.CreateStrict())
            .GetFace(0);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
