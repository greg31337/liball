namespace ttf.Tests;

public class VerticalOrientationTests
{
    static int DefaultScript = Tag("DFLT");
    static int HanScript = Tag("hani");
    static int HorizontalKerning = Tag("kern");
    static int VerticalAlternates = Tag("vert");
    static int VerticalAlternatesAndRotation = Tag("vrt2");
    static int VerticalKerning = Tag("vkrn");

    [Test]
    public void ResolvesAllUnicodeVerticalOrientationValuesAndRuns()
    {
        VerticalOrientationMap result = Resolve("A\u4E00\u3001\uFF08");

        Assert.That(result.GetClusterCount(), Is.EqualTo(4));
        Assert.That(result.GetRuneCount(), Is.EqualTo(4));
        Assert.That(result.GetClusterOrientation(0),
            Is.EqualTo(VerticalOrientation.Rotated));
        Assert.That(result.GetClusterOrientation(1),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetClusterOrientation(2),
            Is.EqualTo(VerticalOrientation.TransformedUpright));
        Assert.That(result.GetClusterOrientation(3),
            Is.EqualTo(VerticalOrientation.TransformedRotated));
        Assert.That(result.UsesUprightFallback(1), Is.True);
        Assert.That(result.UsesUprightFallback(2), Is.True);
        Assert.That(result.IsRuneTransformed(2), Is.True);
        Assert.That(result.IsRuneTransformed(3), Is.True);
        Assert.That(result.NeedsFallbackClockwiseRotation(0), Is.True);
        Assert.That(result.NeedsFallbackClockwiseRotation(2), Is.False);
        Assert.That(result.NeedsFallbackClockwiseRotation(3), Is.True);
        Assert.That(result.GetRunCount(), Is.EqualTo(4));
        AssertRun(result, 2, 2, 1, 2, 1, 2, 1,
            VerticalOrientation.TransformedUpright);
        Assert.That(result.GetRunClusterEnd(2), Is.EqualTo(3));
        Assert.That(result.GetRunRuneEnd(2), Is.EqualTo(3));
        Assert.That(result.GetRunUtf16End(2), Is.EqualTo(3));
        Assert.That(result.GetSourceLength(), Is.EqualTo(4));
    }

    [Test]
    public void AppliesOneOrientationToEachExtendedGraphemeCluster()
    {
        VerticalOrientationMap result = Resolve(
            "A\u0301\u4E00\u0301A\u20DD\U0001F600");

        Assert.That(result.GetClusterCount(), Is.EqualTo(4));
        Assert.That(result.GetRuneCount(), Is.EqualTo(7));
        Assert.That(result.GetRuneOrientation(0),
            Is.EqualTo(VerticalOrientation.Rotated));
        Assert.That(result.GetRuneOrientation(1),
            Is.EqualTo(VerticalOrientation.Rotated));
        Assert.That(result.GetRuneOrientation(2),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetRuneOrientation(3),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetRuneOrientation(4),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetRuneOrientation(5),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetRuneOrientation(6),
            Is.EqualTo(VerticalOrientation.Upright));
        Assert.That(result.GetRunCount(), Is.EqualTo(2));
        AssertRun(result, 0, 0, 1, 0, 2, 0, 2,
            VerticalOrientation.Rotated);
        AssertRun(result, 1, 1, 3, 2, 5, 2, 6,
            VerticalOrientation.Upright);
        Assert.That(result.GetSourceLength(), Is.EqualTo(8));
    }

    [Test]
    public void PrefersVrt2AndAppliesItAcrossTheVerticalRun()
    {
        VerticalOrientationMap orientations = Resolve("A\u4E00\u3001\uFF08");
        SfntLayoutTable substitutions = CreateLayoutTable(new int[]
        {
            VerticalAlternates,
            VerticalAlternatesAndRotation
        });
        VerticalFeaturePlan plan = VerticalFeaturePlanBuilder.Build(
            orientations,
            substitutions,
            null!,
            HanScript,
            0);
        SfntLayoutFeaturePlanEntry? entry = plan.GetSubstitutionPlan()
            .FindEntry(VerticalAlternatesAndRotation);

        Assert.That(plan.HasSubstitutionFeature(), Is.True);
        Assert.That(plan.UsesVerticalAlternatesAndRotation(), Is.True);
        Assert.That(plan.UsesVerticalAlternates(), Is.False);
        Assert.That(plan.GetSubstitutionFeatureTag(),
            Is.EqualTo(VerticalAlternatesAndRotation));
        Assert.That(entry!.GetValueAt(0), Is.EqualTo(1));
        Assert.That(entry.GetValueAt(3), Is.EqualTo(1));
        Assert.That(plan.GetSubstitutionPlan().FindEntry(VerticalAlternates),
            Is.Null);
        Assert.That(plan.GetSubstitutionSelection()!.GetLookupCount(),
            Is.EqualTo(1));
        Assert.That(plan.GetSubstitutionSelection()!.GetLookup(0)
            .GetLookupIndex(), Is.EqualTo(1));
    }

    [Test]
    public void FallsBackToVertOnlyForTransformedClusters()
    {
        VerticalOrientationMap orientations = Resolve("A\u4E00\u3001\uFF08");
        VerticalFeaturePlan plan = VerticalFeaturePlanBuilder.Build(
            orientations,
            CreateLayoutTable(new int[] { VerticalAlternates }),
            null!,
            HanScript,
            0);
        SfntLayoutFeaturePlanEntry? entry = plan.GetSubstitutionPlan()
            .FindEntry(VerticalAlternates);

        Assert.That(plan.UsesVerticalAlternates(), Is.True);
        Assert.That(plan.UsesVerticalAlternatesAndRotation(), Is.False);
        Assert.That(entry!.GetRangeCount(), Is.EqualTo(1));
        Assert.That(entry.GetValueAt(0), Is.Zero);
        Assert.That(entry.GetValueAt(1), Is.Zero);
        Assert.That(entry.GetValueAt(2), Is.EqualTo(1));
        Assert.That(entry.GetValueAt(3), Is.EqualTo(1));
        Assert.That(plan.GetSubstitutionSelection()!.GetLookupCount(),
            Is.EqualTo(1));
    }

    [Test]
    public void SelectsVerticalKerningAndUsesVerticalGlyphMetrics()
    {
        VerticalOrientationMap orientations = Resolve("\u4E00\u4E01");
        SfntLayoutTable positioning = CreateLayoutTable(new int[]
        {
            HorizontalKerning,
            VerticalKerning
        });
        VerticalFeaturePlan plan = VerticalFeaturePlanBuilder.Build(
            orientations,
            null!,
            positioning,
            HanScript,
            0);
        GlyphMetrics metrics = new GlyphMetrics(
            7,
            501,
            20,
            900,
            40,
            760,
            true,
            false);

        Assert.That(plan.UsesVerticalKerning(), Is.True);
        Assert.That(plan.GetPositioningPlan().FindEntry(VerticalKerning),
            Is.Not.Null);
        Assert.That(plan.GetPositioningPlan().FindEntry(HorizontalKerning),
            Is.Null);
        Assert.That(plan.GetPositioningSelection()!.GetLookupCount(),
            Is.EqualTo(1));
        Assert.That(plan.GetPositioningSelection()!.GetLookup(0)
            .GetLookupIndex(), Is.EqualTo(1));
        Assert.That(plan.UsesVerticalMetrics(), Is.True);
        Assert.That(plan.GetAdvance(metrics), Is.EqualTo(900));
        Assert.That(plan.GetVerticalOriginX(metrics), Is.EqualTo(250.5));
        Assert.That(plan.GetVerticalOriginY(metrics), Is.EqualTo(760));
        Assert.That(plan.UsesSynthesizedVerticalMetrics(metrics), Is.True);
        Assert.Throws<ArgumentNullException>(() => plan.GetAdvance(null!));
    }

    [Test]
    public void HandlesUnavailableFeaturesEmptyTextAndResourceLimits()
    {
        VerticalOrientationMap empty = Resolve(string.Empty, 0, 0);
        VerticalFeaturePlan plan = VerticalFeaturePlanBuilder.Build(
            empty,
            null!,
            null!,
            HanScript,
            0);

        Assert.That(empty.GetClusterCount(), Is.Zero);
        Assert.That(empty.GetRunCount(), Is.Zero);
        Assert.That(plan.HasSubstitutionFeature(), Is.False);
        Assert.That(plan.GetSubstitutionFeatureTag(), Is.Zero);
        Assert.That(plan.GetSubstitutionPlan().GetEntryCount(), Is.Zero);
        Assert.That(plan.GetSubstitutionSelection(), Is.Null);
        Assert.That(plan.UsesVerticalKerning(), Is.False);
        Assert.That(plan.GetPositioningPlan().GetEntryCount(), Is.Zero);
        Assert.That(plan.GetPositioningSelection(), Is.Null);
        Assert.That(Assert.Throws<FontLimitException>(() =>
            Resolve("AB", 1, 2))!.Message, Is.EqualTo(
            "Vertical orientation resolution exceeds the configured rune limit."));
        Assert.That(Assert.Throws<FontLimitException>(() =>
            Resolve("A\u4E00", 2, 1))!.Message, Is.EqualTo(
            "Vertical orientation resolution exceeds the configured run limit."));
    }

    [Test]
    public void ValidatesArgumentsClusterMapsAndIndexes()
    {
        RuneBuffer input = Decode("A");
        GraphemeClusterMap clusters = Segment(input);
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        VerticalOrientationMap result = VerticalOrientationResolver.Resolve(
            input,
            clusters,
            unicodeData,
            1,
            1);
        GraphemeClusterMap mismatched = new GraphemeClusterMap(
            new int[] { 0 },
            new int[] { 1 },
            new int[] { 0 },
            new int[] { 1 },
            2);

        Assert.Throws<ArgumentNullException>(() =>
            VerticalOrientationResolver.Resolve(
                null!, clusters, unicodeData, 1, 1));
        Assert.Throws<ArgumentNullException>(() =>
            VerticalOrientationResolver.Resolve(
                input, null!, unicodeData, 1, 1));
        Assert.Throws<ArgumentNullException>(() =>
            VerticalOrientationResolver.Resolve(
                input, clusters, null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VerticalOrientationResolver.Resolve(
                input, clusters, unicodeData, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VerticalOrientationResolver.Resolve(
                input, clusters, unicodeData, 1, -1));
        Assert.Throws<ArgumentException>(() =>
            VerticalOrientationResolver.Resolve(
                input, mismatched, unicodeData, 1, 1));
        Assert.Throws<ArgumentNullException>(() =>
            VerticalFeaturePlanBuilder.Build(
                null!, null!, null!, HanScript, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetClusterOrientation(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRuneOrientation(1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRunOrientation(1));
    }

    static VerticalOrientationMap Resolve(string text)
    {
        RuneBuffer input = Decode(text);
        return VerticalOrientationResolver.Resolve(
            input,
            Segment(input),
            UnicodeData.LoadDefault(),
            input.GetCount(),
            input.GetCount());
    }

    static VerticalOrientationMap Resolve(
        string text,
        int maximumRuneCount,
        int maximumRunCount)
    {
        RuneBuffer input = Decode(text);
        return VerticalOrientationResolver.Resolve(
            input,
            Segment(input),
            UnicodeData.LoadDefault(),
            maximumRuneCount,
            maximumRunCount);
    }

    static RuneBuffer Decode(string text)
    {
        return Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
    }

    static GraphemeClusterMap Segment(RuneBuffer input)
    {
        return GraphemeSegmenter.Segment(
            input,
            UnicodeData.LoadDefault(),
            input.GetCount(),
            input.GetCount());
    }

    static void AssertRun(
        VerticalOrientationMap result,
        int runIndex,
        int clusterStart,
        int clusterLength,
        int runeStart,
        int runeLength,
        int utf16Start,
        int utf16Length,
        VerticalOrientation orientation)
    {
        Assert.That(result.GetRunClusterStart(runIndex),
            Is.EqualTo(clusterStart));
        Assert.That(result.GetRunClusterLength(runIndex),
            Is.EqualTo(clusterLength));
        Assert.That(result.GetRunRuneStart(runIndex), Is.EqualTo(runeStart));
        Assert.That(result.GetRunRuneLength(runIndex), Is.EqualTo(runeLength));
        Assert.That(result.GetRunUtf16Start(runIndex), Is.EqualTo(utf16Start));
        Assert.That(result.GetRunUtf16Length(runIndex), Is.EqualTo(utf16Length));
        Assert.That(result.GetRunOrientation(runIndex), Is.EqualTo(orientation));
    }

    static SfntLayoutTable CreateLayoutTable(int[] featureTags)
    {
        SfntLayoutFeature[] features =
            new SfntLayoutFeature[featureTags.Length];
        SfntLayoutLookup[] lookups =
            new SfntLayoutLookup[featureTags.Length];
        int[] featureIndices = new int[featureTags.Length];
        int index = 0;
        while (index < featureTags.Length)
        {
            features[index] = new SfntLayoutFeature(
                featureTags[index],
                0,
                0,
                new int[] { index });
            lookups[index] = new SfntLayoutLookup(
                1,
                0,
                new int[0],
                new int[0],
                0);
            featureIndices[index] = index;
            index++;
        }

        SfntLayoutLanguageSystem language = new SfntLayoutLanguageSystem(
            0,
            0xFFFF,
            featureIndices);
        SfntLayoutScript script = new SfntLayoutScript(
            DefaultScript,
            language,
            new SfntLayoutLanguageSystem[0]);
        return new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(new SfntLayoutScript[] { script }),
            new SfntLayoutFeatureList(features),
            new SfntLayoutLookupList(lookups),
            FontData.Create(new byte[0]).GetSlice(0, 0));
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
