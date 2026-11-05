using ttf;

namespace ttf.Tests;

internal class TextShaperTests
{
    [Test]
    public void DispatchesReorderingThroughTheShapingBarrierInterface()
    {
        RecordingTextShapingBarrier barrier =
            new RecordingTextShapingBarrier();
        TextShapingStagePlan barrierStage = new TextShapingStagePlan(
            DefaultShaperTableKind.Substitution,
            null!,
            barrier);
        TextShapingStagePlan ordinaryStage = new TextShapingStagePlan(
            DefaultShaperTableKind.Positioning,
            null!,
            null);

        barrierStage.ApplyBarrier(null!);

        Assert.Multiple(() =>
        {
            Assert.That(barrierStage.HasBarrier(), Is.True);
            Assert.That(barrier.GetApplicationCount(), Is.EqualTo(1));
            Assert.That(ordinaryStage.HasBarrier(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                ordinaryStage.ApplyBarrier(null!));
        });
    }

    [Test]
    public void OrchestratesLatinLigaturesAndPositioning()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult ligatureResult = TextShaper.Shape("fi", instance);
        TextShapingResult kerningResult = TextShaper.Shape("AV", instance);

        TextShapingRun ligatureRun = ligatureResult.GetRun(0);
        ShapedRun ligature = ligatureRun.GetShapedRun();
        ShapedRun kerning = kerningResult.GetRun(0).GetShapedRun();
        Assert.Multiple(() =>
        {
            Assert.That(ligatureResult.GetRunCount(), Is.EqualTo(1));
            Assert.That(ligatureRun.GetFontInstance(), Is.SameAs(instance));
            Assert.That(ligatureRun.GetScript(), Is.EqualTo("Latn"));
            Assert.That(ligatureRun.GetScriptTag(),
                Is.EqualTo(Tag("latn")));
            Assert.That(ligatureRun.GetPlanKind(),
                Is.EqualTo(TextShapingPlanKind.Default));
            Assert.That(ligatureRun.GetPlanStageCount(), Is.EqualTo(8));
            Assert.That(ligature.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(ligature.GetGlyph(0).GetGlyphId(), Is.EqualTo(6));
            Assert.That(ligature.GetGlyph(0).IsLigature(), Is.True);
            Assert.That(ligature.GetGlyph(0).GetLigatureComponentCount(),
                Is.EqualTo(2));
            Assert.That(ligature.GetGlyph(0).GetUnicodeScalarCount(),
                Is.EqualTo(2));
            Assert.That(ligature.GetGlyph(0).GetUnicodeScalar(0),
                Is.EqualTo('f'));
            Assert.That(ligature.GetGlyph(0).GetUnicodeScalar(1),
                Is.EqualTo('i'));
            Assert.That(kerning.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(kerning.GetGlyph(0).GetGlyphId(), Is.EqualTo(2));
            Assert.That(kerning.GetGlyph(1).GetGlyphId(), Is.EqualTo(3));
            Assert.That(kerning.GetGlyph(0).GetXAdvance(), Is.EqualTo(520));
            Assert.That(kerning.GetGlyph(1).GetXAdvance(), Is.EqualTo(600));
        });
    }

    [Test]
    public void NormalizesWhileRetainingOriginalUnicodeAttribution()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult result = TextShaper.Shape(
            "A\u030A",
            instance);

        ShapedGlyph glyph = result.GetRun(0).GetShapedRun().GetGlyph(0);
        Assert.Multiple(() =>
        {
            Assert.That(result.GetSourceRuneCount(), Is.EqualTo(2));
            Assert.That(result.GetNormalizedRuneCount(), Is.EqualTo(1));
            Assert.That(result.GetGraphemeClusterCount(), Is.EqualTo(1));
            Assert.That(glyph.GetGlyphId(), Is.Zero);
            Assert.That(glyph.GetUnicodeScalarCount(), Is.EqualTo(2));
            Assert.That(glyph.GetUnicodeScalar(0), Is.EqualTo('A'));
            Assert.That(glyph.GetUnicodeScalar(1), Is.EqualTo(0x030A));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(result.GetDiagnostic(0).GetMessage(),
                Does.Contain("U+00C5"));
        });
    }

    [Test]
    public void IntersectsScriptAndBidiRunsInVisualOrder()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult result = TextShaper.Shape("A\u05D0\u05D1", instance);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetRunCount(), Is.EqualTo(2));
            Assert.That(result.GetScriptRunCount(), Is.EqualTo(2));
            Assert.That(result.GetBidiRunCount(), Is.EqualTo(2));
            Assert.That(result.GetRun(0).GetScript(), Is.EqualTo("Latn"));
            Assert.That(result.GetRun(0).GetBidiLevel(), Is.Zero);
            Assert.That(result.GetRun(0).GetShapedRun().GetDirection(),
                Is.EqualTo(ShapingDirection.LeftToRight));
            Assert.That(result.GetRun(1).GetScript(), Is.EqualTo("Hebr"));
            Assert.That(result.GetRun(1).GetPlanKind(),
                Is.EqualTo(TextShapingPlanKind.Hebrew));
            Assert.That(result.GetRun(1).GetPlanStageCount(), Is.EqualTo(6));
            Assert.That(result.GetRun(1).GetBidiLevel() & 1, Is.EqualTo(1));
            Assert.That(result.GetRun(1).GetShapedRun().GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(result.GetRun(1).GetShapedRun().GetGlyph(0)
                .GetUnicodeScalar(0), Is.EqualTo(0x05D1));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void AppliesLanguageAndVerticalDirectionOptions()
    {
        FontInstance instance = LoadInstance();
        TextShapingOptions options = TextShapingOptions.CreateConfigured(
            Utf16DecodingMode.Strict,
            UnicodeNormalizationMode.Nfc,
            TextShapingDirection.TopToBottom,
            BidiParagraphDirection.Auto,
            "en-US",
            new SfntUserFeature[0],
            0,
            0);

        TextShapingRun run = TextShaper.Shape("A", instance, options)
            .GetRun(0);
        ShapedGlyph glyph = run.GetShapedRun().GetGlyph(0);

        Assert.Multiple(() =>
        {
            Assert.That(run.GetLanguageSystemTag(), Is.Not.Zero);
            Assert.That(run.GetShapedRun().GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
            Assert.That(glyph.GetXAdvance(), Is.Zero);
            Assert.That(glyph.GetYAdvance(), Is.LessThan(0));
        });
    }

    [Test]
    public void RemovesUnmappedIgnorablesWithoutLosingTheirSourceCluster()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult result = TextShaper.Shape("A\u200D", instance);

        ShapedRun run = result.GetRun(0).GetShapedRun();
        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
            Assert.That(run.GetGlyphCount(), Is.EqualTo(1));
            Assert.That(run.GetGlyph(0).GetGlyphId(), Is.EqualTo(2));
            Assert.That(run.GetClusterCount(), Is.EqualTo(2));
            Assert.That(run.GetCluster(1).GetUnicodeScalar(0),
                Is.EqualTo(0x200D));
            Assert.That(run.GetCluster(1).GetGlyphCount(), Is.Zero);
        });
    }

    [Test]
    public void RemovesTabsAndSoftHyphensButRetainsTheirSourceClusters()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult result = TextShaper.Shape(
            "A\tA\u00ADA",
            instance);
        bool foundTab = false;
        bool foundSoftHyphen = false;
        int runIndex = 0;
        while (runIndex < result.GetRunCount())
        {
            ShapedRun run = result.GetRun(runIndex).GetShapedRun();
            int clusterIndex = 0;
            while (clusterIndex < run.GetClusterCount())
            {
                ShapedCluster cluster = run.GetCluster(clusterIndex);
                if (cluster.GetUnicodeScalarCount() == 1 &&
                    cluster.GetUnicodeScalar(0) == 0x0009)
                {
                    foundTab = true;
                    Assert.That(cluster.GetGlyphCount(), Is.Zero);
                }

                if (cluster.GetUnicodeScalarCount() == 1 &&
                    cluster.GetUnicodeScalar(0) == 0x00AD)
                {
                    foundSoftHyphen = true;
                    Assert.That(cluster.GetGlyphCount(), Is.Zero);
                }

                clusterIndex++;
            }

            runIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnosticCount(), Is.Zero);
            Assert.That(result.GetGraphemeClusterCount(), Is.EqualTo(5));
            Assert.That(foundTab, Is.True);
            Assert.That(foundSoftHyphen, Is.True);
        });
    }

    [Test]
    public void SupportsEmptyTextAndValidatesInputs()
    {
        FontInstance instance = LoadInstance();

        TextShapingResult empty = TextShaper.Shape("", instance);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetRunCount(), Is.Zero);
            Assert.That(empty.GetDiagnosticCount(), Is.Zero);
            Assert.That(empty.GetSourceRuneCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape(null!, instance));
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape("A", null!));
            Assert.Throws<ArgumentNullException>(() =>
                TextShaper.Shape("A", instance, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TextShapingOptions.CreateConfigured(
                    Utf16DecodingMode.Strict,
                    UnicodeNormalizationMode.Nfc,
                    (TextShapingDirection)99,
                    BidiParagraphDirection.Auto,
                    "und",
                    new SfntUserFeature[0],
                    0,
                    0));
        });
    }

    static FontInstance LoadInstance()
    {
        return HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
    }

    class RecordingTextShapingBarrier : ITextShapingBarrier
    {
        int applicationCount;

        public void Apply(ShapingBuffer buffer)
        {
            applicationCount += 1;
        }

        internal int GetApplicationCount()
        {
            return applicationCount;
        }
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
