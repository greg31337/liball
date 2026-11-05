using ttf;

namespace ttf.Tests;

internal class TextShaperRegressionTests
{
    [Test]
    public void JoinersControlArabicJoiningAndRemainClustered()
    {
        FontInstance instance = LoadCorpusInstance("arabic.ttf");

        ShapedRun joined = TextShaper.Shape("سس", instance)
            .GetRun(0).GetShapedRun();
        ShapedRun nonJoined = TextShaper.Shape("س\u200Cس", instance)
            .GetRun(0).GetShapedRun();
        ShapedRun explicitlyJoined = TextShaper.Shape("س\u200Dس", instance)
            .GetRun(0).GetShapedRun();

        Assert.Multiple(() =>
        {
            AssertGlyphs(joined, new int[] { 6, 8 });
            AssertClusters(joined, new int[] { 1, 0 });
            AssertGlyphs(nonJoined, new int[] { 5, 5 });
            AssertClusters(nonJoined, new int[] { 2, 0 });
            Assert.That(nonJoined.GetClusterCount(), Is.EqualTo(3));
            Assert.That(nonJoined.GetCluster(1).GetGlyphCount(), Is.Zero);
            AssertGlyphs(explicitlyJoined, new int[] { 6, 8 });
            AssertClusters(explicitlyJoined, new int[] { 2, 0 });
            Assert.That(explicitlyJoined.GetClusterCount(), Is.EqualTo(3));
            Assert.That(explicitlyJoined.GetCluster(1).GetGlyphCount(),
                Is.Zero);
        });
    }

    [Test]
    public void RemovesDefaultIgnorablesWithoutLosingSourceClusters()
    {
        FontInstance instance = LoadDefaultInstance();

        TextShapingResult result = TextShaper.Shape(
            "A\u2060V",
            instance);
        ShapedRun run = result.GetRun(0).GetShapedRun();

        Assert.Multiple(() =>
        {
            AssertGlyphs(run, new int[] { 2, 3 });
            AssertClusters(run, new int[] { 0, 2 });
            Assert.That(run.GetClusterCount(), Is.EqualTo(3));
            Assert.That(run.GetCluster(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(run.GetCluster(1).GetUnicodeScalar(0),
                Is.EqualTo(0x2060));
            Assert.That(run.GetCluster(1).GetGlyphCount(), Is.Zero);
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
        });
    }

    [Test]
    public void InsertsDottedCircleForBrokenKhmerCluster()
    {
        FontInstance instance = LoadCorpusInstance("khmer.ttf");

        TextShapingResult result = TextShaper.Shape("\u17C2", instance);
        ShapedRun run = result.GetRun(0).GetShapedRun();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRun(0).GetPlanKind(),
                Is.EqualTo(TextShapingPlanKind.Khmer));
            AssertGlyphs(run, new int[] { 4, 11 });
            AssertClusters(run, new int[] { 0, 0 });
            Assert.That(run.GetGlyph(0).GetXAdvance(), Is.EqualTo(288));
            Assert.That(run.GetGlyph(1).GetXAdvance(), Is.EqualTo(635));
            Assert.That(run.GetGlyph(0).IsUnsafeToBreak(), Is.True);
            Assert.That(run.GetGlyph(1).IsUnsafeToBreak(), Is.True);
        });
    }

    [Test]
    public void CanonicalEquivalentsShapeIdenticallyWithOriginalRanges()
    {
        FontInstance instance = LoadDefaultInstance();

        ShapedGlyph composed = TextShaper.Shape("\u00C5", instance)
            .GetRun(0).GetShapedRun().GetGlyph(0);
        ShapedGlyph decomposed = TextShaper.Shape("A\u030A", instance)
            .GetRun(0).GetShapedRun().GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(decomposed.GetGlyphId(),
                Is.EqualTo(composed.GetGlyphId()));
            Assert.That(decomposed.GetXAdvance(),
                Is.EqualTo(composed.GetXAdvance()));
            Assert.That(decomposed.GetXOffset(),
                Is.EqualTo(composed.GetXOffset()));
            Assert.That(decomposed.GetYOffset(),
                Is.EqualTo(composed.GetYOffset()));
            Assert.That(composed.GetUtf16Start(), Is.Zero);
            Assert.That(composed.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(decomposed.GetUtf16Start(), Is.Zero);
            Assert.That(decomposed.GetUtf16Length(), Is.EqualTo(2));
        });
    }

    [Test]
    public void SupplementaryScalarRetainsItsUtf16Range()
    {
        FontInstance instance = LoadDefaultInstance();

        TextShapingResult result = TextShaper.Shape("\U0001F600", instance);
        ShapedGlyph glyph = result.GetRun(0).GetShapedRun().GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetSourceRuneCount(), Is.EqualTo(1));
            Assert.That(glyph.GetUtf16Start(), Is.Zero);
            Assert.That(glyph.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(glyph.GetUnicodeScalarCount(), Is.EqualTo(1));
            Assert.That(glyph.GetUnicodeScalar(0), Is.EqualTo(0x1F600));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
        });
    }

    [Test]
    public void MalformedUtf16HonorsStrictAndReplacementModes()
    {
        FontInstance instance = LoadDefaultInstance();
        string malformed = new string(new char[] { '\uD800' });

        Assert.Throws<System.Text.DecoderFallbackException>(() =>
            TextShaper.Shape(malformed, instance));
        TextShapingResult replacement = TextShaper.Shape(
            malformed,
            instance,
            CreateOptions(Utf16DecodingMode.Replacement,
                TextShapingDirection.Auto));
        ShapedGlyph glyph = replacement.GetRun(0).GetShapedRun().GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(replacement.GetSourceRuneCount(), Is.EqualTo(1));
            Assert.That(glyph.GetUnicodeScalar(0), Is.EqualTo(0xFFFD));
            Assert.That(glyph.GetUtf16Start(), Is.Zero);
            Assert.That(glyph.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(replacement.GetDiagnosticCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void GlyphClustersAreMonotonicInRunDirection()
    {
        FontInstance instance = LoadDefaultInstance();

        ShapedRun leftToRight = TextShaper.Shape("AfiV", instance)
            .GetRun(0).GetShapedRun();
        ShapedRun rightToLeft = TextShaper.Shape(
            "AV",
            instance,
            CreateOptions(Utf16DecodingMode.Strict,
                TextShapingDirection.RightToLeft))
            .GetRun(0).GetShapedRun();

        Assert.Multiple(() =>
        {
            AssertClusters(leftToRight, new int[] { 0, 1, 3 });
            AssertClusters(rightToLeft, new int[] { 1, 0 });
            AssertMonotonic(leftToRight, true);
            AssertMonotonic(rightToLeft, false);
        });
    }

    static TextShapingOptions CreateOptions(
        Utf16DecodingMode decodingMode,
        TextShapingDirection direction)
    {
        return TextShapingOptions.CreateConfigured(
            decodingMode,
            UnicodeNormalizationMode.Nfc,
            direction,
            BidiParagraphDirection.Auto,
            "und",
            new SfntUserFeature[0],
            0,
            0);
    }

    static FontInstance LoadCorpusInstance(string fontName)
    {
        return HarfBuzzShapingModelCorpusTests.LoadCorpusFace(fontName)
            .CreateDefaultInstance();
    }

    static FontInstance LoadDefaultInstance()
    {
        return HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
    }

    static void AssertGlyphs(ShapedRun run, int[] expected)
    {
        Assert.That(run.GetGlyphCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(run.GetGlyph(index).GetGlyphId(),
                Is.EqualTo(expected[index]), "glyph " + index);
            index += 1;
        }
    }

    static void AssertClusters(ShapedRun run, int[] expected)
    {
        Assert.That(run.GetGlyphCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(run.GetGlyph(index).GetUtf16Start(),
                Is.EqualTo(expected[index]), "cluster " + index);
            index += 1;
        }
    }

    static void AssertMonotonic(ShapedRun run, bool increasing)
    {
        int index = 1;
        while (index < run.GetGlyphCount())
        {
            int previous = run.GetGlyph(index - 1).GetUtf16Start();
            int current = run.GetGlyph(index).GetUtf16Start();
            if (increasing)
            {
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
            }
            else
            {
                Assert.That(current, Is.LessThanOrEqualTo(previous));
            }

            index += 1;
        }
    }
}
