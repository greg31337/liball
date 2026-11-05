namespace ttf.Tests;

internal class HangulShaperTests
{
    static int CharacterComposition = Tag("ccmp");
    static int ContextualAlternates = Tag("calt");
    static int LeadingJamoForms = Tag("ljmo");
    static int MarkPositioning = Tag("mark");
    static int MarkToMarkPositioning = Tag("mkmk");
    static int StylisticAlternates = Tag("salt");
    static int TrailingJamoForms = Tag("tjmo");
    static int VowelJamoForms = Tag("vjmo");

    [Test]
    public void ComposesModernJamoWhenTheSyllableGlyphIsAvailable()
    {
        ShapingBuffer buffer = CreateBuffer("\u1100\u1161\u11A8");
        SetGlyphIds(buffer);

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[] { 0xAC01 },
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0xAC01 });
            Assert.That(buffer.HasGlyphId(0), Is.False);
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
            Assert.That(preparation.GetComposedSyllableCount(),
                Is.EqualTo(1));
            Assert.That(preparation.GetDecomposedSyllableCount(), Is.Zero);
            Assert.That(preparation.GetJamoFeature(0),
                Is.EqualTo(HangulJamoFeature.None));
        });
    }

    [Test]
    public void KeepsUnsupportedModernAndExtendedJamoForFormFeatures()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1100\u1161\u11A8\uA960\uD7B0\uD7CB");

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[]
            {
                0x1100,
                0x1161,
                0x11A8,
                0xA960,
                0xD7B0,
                0xD7CB
            });
            AssertJamoFeatures(preparation, new HangulJamoFeature[]
            {
                HangulJamoFeature.Leading,
                HangulJamoFeature.Vowel,
                HangulJamoFeature.Trailing,
                HangulJamoFeature.Leading,
                HangulJamoFeature.Vowel,
                HangulJamoFeature.Trailing
            });
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(buffer.GetClusterStart(2), Is.Zero);
            Assert.That(buffer.GetClusterStart(3), Is.EqualTo(3));
            Assert.That(buffer.GetClusterLength(5), Is.EqualTo(3));
        });
    }

    [Test]
    public void DecomposesAnUnsupportedPrecomposedSyllableWhenJamoExist()
    {
        ShapingBuffer buffer = CreateBuffer("\uAC01");
        buffer.SetGlyphId(0, 42);

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[] { 0x1100, 0x1161, 0x11A8 },
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0x1100, 0x1161, 0x11A8 });
            AssertJamoFeatures(preparation, new HangulJamoFeature[]
            {
                HangulJamoFeature.Leading,
                HangulJamoFeature.Vowel,
                HangulJamoFeature.Trailing
            });
            Assert.That(preparation.GetDecomposedSyllableCount(),
                Is.EqualTo(1));
            Assert.That(buffer.HasGlyphId(0), Is.False);
            Assert.That(buffer.GetClusterLength(2), Is.EqualTo(1));
            Assert.That(buffer.IsUnsafeToBreak(2), Is.True);
        });
    }

    [Test]
    public void LeavesAnUnsupportedSyllableWhenJamoGlyphsAreIncomplete()
    {
        ShapingBuffer buffer = CreateBuffer("\uAC01");

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[] { 0x1100, 0x1161 },
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0xAC01 });
            Assert.That(preparation.GetDecomposedSyllableCount(), Is.Zero);
            Assert.That(preparation.GetJamoFeature(0),
                Is.EqualTo(HangulJamoFeature.None));
        });
    }

    [Test]
    public void ChoosesCompositionOrJamoFallbackForLvPlusTrailingJamo()
    {
        ShapingBuffer composedBuffer = CreateBuffer("\uAC00\u11A8");
        HangulShaperPreparation composed = Prepare(
            composedBuffer,
            new int[] { 0xAC01 },
            new int[0]);
        ShapingBuffer decomposedBuffer = CreateBuffer("\uAC00\u11A8");
        HangulShaperPreparation decomposed = Prepare(
            decomposedBuffer,
            new int[] { 0xAC00, 0x1100, 0x1161, 0x11A8 },
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(composedBuffer, new int[] { 0xAC01 });
            Assert.That(composed.GetComposedSyllableCount(), Is.EqualTo(1));
            AssertRunes(decomposedBuffer,
                new int[] { 0x1100, 0x1161, 0x11A8 });
            AssertJamoFeatures(decomposed, new HangulJamoFeature[]
            {
                HangulJamoFeature.Leading,
                HangulJamoFeature.Vowel,
                HangulJamoFeature.Trailing
            });
            Assert.That(decomposed.GetDecomposedSyllableCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ReordersNonzeroToneBeforeTheWholeSyllable()
    {
        ShapingBuffer buffer = CreateBuffer("\u1100\u1161\u302E");

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0x302E, 0x1100, 0x1161 });
            Assert.That(preparation.GetReorderedToneCount(), Is.EqualTo(1));
            Assert.That(preparation.GetZeroWidthToneCount(), Is.Zero);
            Assert.That(buffer.GetClusterStart(2), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
            AssertJamoFeatures(preparation, new HangulJamoFeature[]
            {
                HangulJamoFeature.None,
                HangulJamoFeature.Leading,
                HangulJamoFeature.Vowel
            });
        });
    }

    [Test]
    public void LeavesAZeroWidthToneAfterItsSyllable()
    {
        ShapingBuffer buffer = CreateBuffer("\uAC00\u302F");

        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[] { 0xAC00, 0x302F },
            new int[] { 0x302F });

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0xAC00, 0x302F });
            Assert.That(preparation.GetReorderedToneCount(), Is.Zero);
            Assert.That(preparation.GetZeroWidthToneCount(), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(1));
            Assert.That(buffer.IsUnsafeToBreak(1), Is.True);
        });
    }

    [Test]
    public void AddsDottedCircleOnTheCorrectSideOfStandaloneTones()
    {
        ShapingBuffer spacingBuffer = CreateBuffer("\u302E");
        HangulShaperPreparation spacing = Prepare(
            spacingBuffer,
            new int[] { 0x25CC },
            new int[0]);
        ShapingBuffer zeroWidthBuffer = CreateBuffer("\u302F");
        HangulShaperPreparation zeroWidth = Prepare(
            zeroWidthBuffer,
            new int[] { 0x25CC, 0x302F },
            new int[] { 0x302F });
        ShapingBuffer unsupportedDottedCircle = CreateBuffer("\u302E");
        HangulShaperPreparation unchanged = Prepare(
            unsupportedDottedCircle,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            AssertRunes(spacingBuffer, new int[] { 0x302E, 0x25CC });
            Assert.That(spacing.GetInsertedDottedCircleCount(),
                Is.EqualTo(1));
            AssertRunes(zeroWidthBuffer, new int[] { 0x25CC, 0x302F });
            Assert.That(zeroWidth.GetInsertedDottedCircleCount(),
                Is.EqualTo(1));
            Assert.That(zeroWidth.GetZeroWidthToneCount(), Is.EqualTo(1));
            Assert.That(zeroWidthBuffer.GetClusterLength(0), Is.EqualTo(1));
            AssertRunes(unsupportedDottedCircle, new int[] { 0x302E });
            Assert.That(unchanged.GetInsertedDottedCircleCount(), Is.Zero);
        });
    }

    [Test]
    public void BuildsJamoAndStandardFeatureStages()
    {
        ShapingBuffer buffer = CreateBuffer("\u1100\u1161\u11A8A");
        HangulShaperPreparation preparation = Prepare(
            buffer,
            new int[0],
            new int[0]);
        HangulShaperPlan plan = HangulShaper.BuildPlan(
            preparation,
            new SfntUserFeature[]
            {
                Feature(ContextualAlternates, 1, 0, 4),
                Feature(StylisticAlternates, 2, 1, 4)
            });

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetScriptTag(), Is.EqualTo(Tag("hang")));
            Assert.That(plan.GetStageCount(), Is.EqualTo(10));
            AssertStage(plan, 0, HangulShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(
                plan,
                1,
                HangulShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 2, HangulShaperStageKind.LeadingJamoForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 3, HangulShaperStageKind.VowelJamoForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 4, HangulShaperStageKind.TrailingJamoForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 7, HangulShaperStageKind.Kerning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 8, HangulShaperStageKind.MarkPositioning,
                DefaultShaperTableKind.Positioning);
            AssertFeature(Plan(plan, 1), CharacterComposition,
                new int[] { 1, 1, 1, 1 });
            AssertFeature(Plan(plan, 2), LeadingJamoForms,
                new int[] { 1, 0, 0, 0 });
            AssertFeature(Plan(plan, 3), VowelJamoForms,
                new int[] { 0, 1, 0, 0 });
            AssertFeature(Plan(plan, 4), TrailingJamoForms,
                new int[] { 0, 0, 1, 0 });
            AssertFeature(Plan(plan, 5), ContextualAlternates,
                new int[] { 0, 0, 0, 1 });
            AssertFeature(Plan(plan, 6), StylisticAlternates,
                new int[] { 0, 2, 2, 2 });
            AssertFeature(Plan(plan, 9), StylisticAlternates,
                new int[] { 0, 2, 2, 2 });
            Assert.That(Plan(plan, 8).FindEntry(MarkPositioning),
                Is.Not.Null);
            Assert.That(Plan(plan, 8).FindEntry(MarkToMarkPositioning),
                Is.Not.Null);
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArguments()
    {
        HangulGlyphCoverage emptyCoverage =
            new HangulGlyphCoverage(new int[0], new int[0]);
        HangulShaperPreparation empty = HangulShaper.Prepare(
            CreateBuffer(""),
            emptyCoverage);
        HangulShaperPlan emptyPlan = HangulShaper.BuildPlan(
            empty,
            new SfntUserFeature[0]);
        ShapingBuffer buffer = CreateBuffer("\uAC00");
        HangulShaperPreparation preparation = HangulShaper.Prepare(
            buffer,
            emptyCoverage);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetCount(), Is.Zero);
            Assert.That(emptyPlan.GetStageCount(), Is.EqualTo(10));
            Assert.Throws<ArgumentNullException>(() =>
                HangulShaper.Prepare(null!, emptyCoverage));
            Assert.Throws<ArgumentNullException>(() =>
                HangulShaper.Prepare(buffer, null!));
            Assert.Throws<ArgumentNullException>(() =>
                HangulShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                HangulShaper.BuildPlan(preparation, null!));
            Assert.Throws<ArgumentException>(() => HangulShaper.BuildPlan(
                preparation,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetJamoFeature(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.IsContextualAlternatesEnabled(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.GetStage(10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.FindStage((HangulShaperStageKind)10));
            Assert.Throws<ArgumentNullException>(() =>
                new HangulGlyphCoverage(null!, new int[0]));
            Assert.Throws<ArgumentNullException>(() =>
                new HangulGlyphCoverage(new int[0], null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HangulGlyphCoverage(
                    new int[] { 0x110000 },
                    new int[0]));
            Assert.Throws<ArgumentException>(() =>
                new HangulGlyphCoverage(
                    new int[0],
                    new int[] { 0x302E }));
        });
    }

    static HangulShaperPreparation Prepare(
        ShapingBuffer buffer,
        int[] supportedCodePoints,
        int[] zeroWidthCodePoints)
    {
        return HangulShaper.Prepare(
            buffer,
            new HangulGlyphCoverage(
                supportedCodePoints,
                zeroWidthCodePoints));
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
    }

    static void SetGlyphIds(ShapingBuffer buffer)
    {
        int index = 0;
        while (index < buffer.GetCount())
        {
            buffer.SetGlyphId(index, index + 1);
            index++;
        }
    }

    static SfntLayoutFeaturePlan Plan(
        HangulShaperPlan plan,
        int stageIndex)
    {
        return plan.GetStage(stageIndex).GetFeaturePlan();
    }

    static SfntUserFeature Feature(
        int tag,
        int value,
        int start,
        int end)
    {
        return new SfntUserFeature(tag, value, start, end);
    }

    static void AssertStage(
        HangulShaperPlan plan,
        int index,
        HangulShaperStageKind kind,
        DefaultShaperTableKind tableKind)
    {
        Assert.That(plan.GetStage(index).GetKind(), Is.EqualTo(kind));
        Assert.That(plan.GetStage(index).GetTableKind(),
            Is.EqualTo(tableKind));
    }

    static void AssertJamoFeatures(
        HangulShaperPreparation preparation,
        HangulJamoFeature[] expected)
    {
        Assert.That(preparation.GetCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(preparation.GetJamoFeature(index),
                Is.EqualTo(expected[index]),
                "Jamo feature at " + index);
            index++;
        }
    }

    static void AssertRunes(ShapingBuffer buffer, int[] expected)
    {
        Assert.That(buffer.GetCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(buffer.GetScalarValue(index),
                Is.EqualTo(expected[index]),
                "rune at " + index);
            index++;
        }
    }

    static void AssertFeature(
        SfntLayoutFeaturePlan plan,
        int tag,
        int[] expected)
    {
        SfntLayoutFeaturePlanEntry? entry = plan.FindEntry(tag);
        Assert.That(entry, Is.Not.Null);
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(entry!.GetValueAt(index),
                Is.EqualTo(expected[index]),
                "feature value at " + index);
            index++;
        }
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
