using ttf;

namespace ttf.Tests;

internal class FontSetShapingPublicTests
{
    [Test]
    public void ReturnsOrderedRunsForEveryResolvedFontTransition()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontFace fallback = FontClusterFallbackTests.Clone(
            source,
            "Latin Fallback",
            new int[] { 0x0042 });
        FontSet set = FontClusterFallbackTests.Set(primary, fallback);

        TextShapingResult result = set.ShapeText(
            "ABBA",
            new FontRequest("Primary"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(3));
            Assert.That(result.GetRun(0).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(1).GetFontFace(), Is.SameAs(fallback));
            Assert.That(result.GetRun(2).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(0).GetFontInstance().GetFace(),
                Is.SameAs(primary));
            Assert.That(result.GetRun(1).GetFontInstance().GetFace(),
                Is.SameAs(fallback));
            Assert.That(result.GetRun(0).GetUtf16Start(), Is.Zero);
            Assert.That(result.GetRun(0).GetUtf16Length(), Is.EqualTo(1));
            Assert.That(result.GetRun(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(result.GetRun(1).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(result.GetRun(2).GetUtf16Start(), Is.EqualTo(3));
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void ResolvesAnEntireGraphemeClusterToOneFallbackFace()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontFace fallback = FontClusterFallbackTests.Clone(
            source,
            "Mark Fallback",
            new int[] { 0x0041, 0x0301 });
        FontSet set = FontClusterFallbackTests.Set(primary, fallback);
        TextShapingOptions options = new TextShapingOptions();
        options.SetNormalizationMode(UnicodeNormalizationMode.None);

        TextShapingResult result = set.ShapeTextWithOptions(
            "A\u0301",
            new FontRequest("Primary"),
            options);
        TextShapingRun run = result.GetRun(0);
        ShapedRun shaped = run.GetShapedRun();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(1));
            Assert.That(run.GetFontFace(), Is.SameAs(fallback));
            Assert.That(run.GetUtf16Start(), Is.Zero);
            Assert.That(run.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(shaped.GetClusterCount(), Is.EqualTo(2));
            Assert.That(shaped.GetCluster(0).GetUnicodeScalar(0),
                Is.EqualTo(0x0041));
            Assert.That(shaped.GetCluster(1).GetUnicodeScalar(0),
                Is.EqualTo(0x0301));
            Assert.That(shaped.GetGlyph(0).GetGlyphId(), Is.Not.Zero);
            Assert.That(shaped.GetGlyph(1).GetGlyphId(), Is.Not.Zero);
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void SplitsAtScriptAndDirectionBoundariesWithoutChangingFont()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041, 0x05D0, 0x05D1 });
        FontSet set = new FontSet();
        set.Add(primary);

        TextShapingResult result = set.ShapeText(
            "A\u05D0\u05D1",
            new FontRequest("Primary"));

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(2));
            Assert.That(result.GetRun(0).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(1).GetFontFace(), Is.SameAs(primary));
            Assert.That(result.GetRun(0).GetScript(), Is.EqualTo("Latn"));
            Assert.That(result.GetRun(0).GetBidiLevel(), Is.Zero);
            Assert.That(result.GetRun(1).GetScript(), Is.EqualTo("Hebr"));
            Assert.That(result.GetRun(1).GetBidiLevel() & 1, Is.EqualTo(1));
            Assert.That(result.GetRun(0).GetUtf16Start(), Is.Zero);
            Assert.That(result.GetRun(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(result.GetRun(1).GetUtf16Length(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ShapesCopiedSpansWithDefaultAndExplicitOptions()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontSet set = new FontSet();
        set.Add(source);
        string family = source.GetNames().GetFamilyName()!
            .GetValue();
        FontRequest request = new FontRequest(family);
        char[] sourceText = "XfiY".ToCharArray();

        TextShapingResult defaults = set.ShapeSpan(
            sourceText.AsSpan(1, 2),
            request);
        sourceText[1] = 'A';
        sourceText[2] = 'V';

        TextShapingOptions options = new TextShapingOptions();
        options.SetFeatures(new FontLayoutFeatureSetting[]
        {
            new FontLayoutFeatureSetting(Tag("liga"), 0, 0, 2)
        });
        TextShapingResult explicitResult = set.ShapeSpanWithOptions(
            "fi".AsSpan(),
            request,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetRunCount(), Is.EqualTo(1));
            Assert.That(defaults.GetRun(0).GetShapedRun().GetGlyphCount(),
                Is.EqualTo(1));
            Assert.That(defaults.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetGlyphId(), Is.EqualTo(6));
            Assert.That(defaults.GetRun(0).GetUtf16Start(), Is.Zero);
            Assert.That(explicitResult.GetRun(0).GetShapedRun().GetGlyphCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ReportsUnavailableFallbackAndValidatesPublicInputs()
    {
        FontSet empty = new FontSet();
        FontRequest request = new FontRequest("Unavailable");
        TextShapingResult missing = empty.ShapeText("A", request);

        Assert.Multiple(() =>
        {
            Assert.That(missing.GetRunCount(), Is.Zero);
            Assert.That(missing.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(missing.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(empty.ShapeText("", request).GetRunCount(), Is.Zero);
            Assert.That(empty.ShapeSpan(ReadOnlySpan<char>.Empty, request)
                .GetRunCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                empty.ShapeText(null!, request));
            Assert.Throws<ArgumentNullException>(() =>
                empty.ShapeText("A", null!));
            Assert.Throws<ArgumentNullException>(() =>
                empty.ShapeTextWithOptions("A", request, null!));
            Assert.Throws<ArgumentNullException>(() =>
                ShapeSpanWithNullRequest(empty));
            Assert.Throws<ArgumentNullException>(() =>
                ShapeSpanWithNullOptions(empty, request));
            Assert.Throws<System.Text.DecoderFallbackException>(() =>
                empty.ShapeText("\uD800", request));
        });
    }

    static void ShapeSpanWithNullRequest(FontSet set)
    {
        set.ShapeSpan("A".AsSpan(), null!);
    }

    static void ShapeSpanWithNullOptions(
        FontSet set,
        FontRequest request)
    {
        set.ShapeSpanWithOptions("A".AsSpan(), request, null!);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
