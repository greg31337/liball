namespace ttf.Tests;

internal class KhmerShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();
    static int AbovebaseForms = Tag("abvf");
    static int BelowbaseForms = Tag("blwf");
    static int CharacterComposition = Tag("ccmp");
    static int ConsonantAfterRo = Tag("cfar");
    static int ContextualLigatures = Tag("clig");
    static int LocalizedForms = Tag("locl");
    static int PostbaseForms = Tag("pstf");
    static int PrebaseForms = Tag("pref");
    static int PresentationForms = Tag("pres");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void ReordersCoengRoAndPreVowelBeforeTheBase()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1780\u17D2\u179A\u17D2\u1782\u17C1");
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);

        Assert.Multiple(() =>
        {
            AssertSources(preparation,
                new int[] { 5, 1, 2, 0, 3, 4 });
            AssertValues(preparation, 0,
                new bool[] { false, true, true, false, false, false });
            AssertValues(preparation, 4,
                new bool[] { true, false, false, false, true, true });
        });

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[]
            {
                0x17C1,
                0x17D2,
                0x179A,
                0x1780,
                0x17D2,
                0x1782
            });
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(6));
            Assert.That(buffer.IsUnsafeToBreak(5), Is.True);
        });
    }

    [Test]
    public void MovesAPlainPreVowelToTheSyllableStart()
    {
        ShapingBuffer buffer = CreateBuffer("\u1780\u17C1");
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);

        preparation.ApplyReordering(buffer);

        AssertRunes(buffer, new int[] { 0x17C1, 0x1780 });
    }

    [TestCase(0x17BE)]
    [TestCase(0x17BF)]
    [TestCase(0x17C0)]
    [TestCase(0x17C4)]
    [TestCase(0x17C5)]
    public void DecomposesAndReordersSplitVowels(int splitVowel)
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1780" + char.ConvertFromUtf32(splitVowel));
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetSplitVowelCount(), Is.EqualTo(1));
            Assert.That(buffer.GetCount(), Is.EqualTo(3));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x17C1));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(1));
        });

        preparation.ApplyReordering(buffer);
        AssertRunes(buffer, new int[] { 0x17C1, 0x1780, splitVowel });
    }

    [Test]
    public void InsertsDottedCircleForEachBrokenClusterWhenRequested()
    {
        ShapingBuffer buffer = CreateBuffer("\u17D2\u1780A\u17C1");
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.InsertForBrokenClusters);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetInsertedDottedCircleCount(),
                Is.EqualTo(2));
            Assert.That(buffer.GetCount(), Is.EqualTo(6));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x25CC));
            Assert.That(buffer.GetScalarValue(4), Is.EqualTo(0x25CC));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(1));
            Assert.That(preparation.GetSyllableMap().HasBrokenSyllable(),
                Is.False);
        });
    }

    [Test]
    public void PreservesBrokenClustersWhenDottedCircleInsertionIsDisabled()
    {
        ShapingBuffer buffer = CreateBuffer("\u17D2\u1780");
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetInsertedDottedCircleCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x17D2));
            Assert.That(preparation.GetSyllableMap().HasBrokenSyllable(),
                Is.True);
        });
    }

    [Test]
    public void BuildsKhmerFeatureStagesWithBasicFormsAppliedTogether()
    {
        KhmerShaperPlan plan = BuildPlan(
            "\u1780\u17D2\u179A\u17D2\u1782",
            new SfntUserFeature[0]);
        SfntLayoutFeaturePlan basic = Plan(plan, 3);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetScriptTag(), Is.EqualTo(Tag("khmr")));
            Assert.That(plan.GetStageCount(), Is.EqualTo(11));
            AssertStage(plan, 0, KhmerShaperStageKind.Reordering,
                DefaultShaperTableKind.Substitution, true);
            AssertStage(plan, 1, KhmerShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 2,
                KhmerShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 3, KhmerShaperStageKind.BasicForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 4, KhmerShaperStageKind.PresentationForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 7, KhmerShaperStageKind.Distances,
                DefaultShaperTableKind.Positioning, false);
            AssertStage(plan, 10, KhmerShaperStageKind.UserPositioning,
                DefaultShaperTableKind.Positioning, false);
            Assert.That(basic.GetEntryCount(), Is.EqualTo(5));
            Assert.That(basic.FindEntry(PrebaseForms), Is.Not.Null);
            Assert.That(basic.FindEntry(BelowbaseForms), Is.Not.Null);
            Assert.That(basic.FindEntry(AbovebaseForms), Is.Not.Null);
            Assert.That(basic.FindEntry(PostbaseForms), Is.Not.Null);
            Assert.That(basic.FindEntry(ConsonantAfterRo), Is.Not.Null);
            Assert.That(Plan(plan, 1).FindEntry(LocalizedForms),
                Is.Not.Null);
            Assert.That(Plan(plan, 2).FindEntry(CharacterComposition),
                Is.Not.Null);
            Assert.That(Plan(plan, 4).FindEntry(PresentationForms),
                Is.Not.Null);
            Assert.That(Plan(plan, 5).FindEntry(ContextualLigatures),
                Is.Not.Null);
        });
    }

    [Test]
    public void AssignsBasicFeatureRangesAfterReordering()
    {
        KhmerShaperPlan plan = BuildPlan(
            "\u1780\u17D2\u179A\u17D2\u1782\u17C1",
            new SfntUserFeature[0]);
        SfntLayoutFeaturePlan basic = Plan(plan, 3);

        Assert.Multiple(() =>
        {
            AssertFeature(basic, PrebaseForms,
                new int[] { 0, 1, 1, 0, 0, 0 });
            AssertFeature(basic, BelowbaseForms,
                new int[] { 1, 1, 1, 0, 1, 1 });
            AssertFeature(basic, AbovebaseForms,
                new int[] { 1, 1, 1, 0, 1, 1 });
            AssertFeature(basic, PostbaseForms,
                new int[] { 1, 1, 1, 0, 1, 1 });
            AssertFeature(basic, ConsonantAfterRo,
                new int[] { 1, 0, 0, 0, 1, 1 });
        });
    }

    [Test]
    public void RoutesCustomFeaturesAndKeepsRequiredKhmerFeaturesEnabled()
    {
        KhmerShaperPlan plan = BuildPlan(
            "\u1780\u1781\u1782",
            new SfntUserFeature[]
            {
                Feature(ContextualLigatures, 0, 0, 3),
                Feature(StylisticAlternates, 2, 1, 3)
            });

        Assert.Multiple(() =>
        {
            AssertFeature(Plan(plan, 5), ContextualLigatures,
                new int[] { 1, 1, 1 });
            AssertFeature(Plan(plan, 6), StylisticAlternates,
                new int[] { 0, 2, 2 });
            AssertFeature(Plan(plan, 10), StylisticAlternates,
                new int[] { 0, 2, 2 });
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArgumentsAndState()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        KhmerShaperPreparation empty = KhmerShaper.Prepare(
            emptyBuffer,
            UnicodeData,
            KhmerDottedCirclePolicy.InsertForBrokenClusters,
            0);
        KhmerShaperPlan emptyPlan = KhmerShaper.BuildPlan(
            empty,
            new SfntUserFeature[0]);
        ShapingBuffer buffer = CreateBuffer("\u1780");
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);
        ShapingBuffer other = CreateBuffer("\u1780");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetSyllableMap().GetRuneCount(), Is.Zero);
            Assert.That(emptyPlan.GetStageCount(), Is.EqualTo(11));
            Assert.Throws<ArgumentNullException>(() => KhmerShaper.Prepare(
                null!,
                UnicodeData,
                KhmerDottedCirclePolicy.InsertForBrokenClusters,
                1));
            Assert.Throws<ArgumentNullException>(() => KhmerShaper.Prepare(
                buffer,
                null!,
                KhmerDottedCirclePolicy.InsertForBrokenClusters,
                1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                KhmerShaper.Prepare(
                    buffer,
                    UnicodeData,
                    (KhmerDottedCirclePolicy)2,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                KhmerShaper.Prepare(
                    buffer,
                    UnicodeData,
                    KhmerDottedCirclePolicy.InsertForBrokenClusters,
                    -1));
            Assert.Throws<ArgumentNullException>(() =>
                KhmerShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                KhmerShaper.BuildPlan(preparation, null!));
            Assert.Throws<ArgumentException>(() => KhmerShaper.BuildPlan(
                preparation,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentException>(() =>
                preparation.ApplyReordering(other));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetSourceIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.GetStage(11));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.FindStage((KhmerShaperStageKind)11));
        });

        empty.ApplyReordering(emptyBuffer);
        preparation.ApplyReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyReordering(buffer));
    }

    static KhmerShaperPreparation Prepare(
        ShapingBuffer buffer,
        KhmerDottedCirclePolicy policy)
    {
        return KhmerShaper.Prepare(
            buffer,
            UnicodeData,
            policy,
            buffer.GetCount() + 1);
    }

    static KhmerShaperPlan BuildPlan(
        string text,
        SfntUserFeature[] features)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        KhmerShaperPreparation preparation = Prepare(
            buffer,
            KhmerDottedCirclePolicy.PreserveBrokenClusters);
        return KhmerShaper.BuildPlan(preparation, features);
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
    }

    static SfntLayoutFeaturePlan Plan(
        KhmerShaperPlan plan,
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
        KhmerShaperPlan plan,
        int index,
        KhmerShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        bool barrier)
    {
        Assert.That(plan.GetStage(index).GetKind(), Is.EqualTo(kind));
        Assert.That(plan.GetStage(index).GetTableKind(),
            Is.EqualTo(tableKind));
        Assert.That(plan.GetStage(index).IsReorderingBarrier(),
            Is.EqualTo(barrier));
    }

    static void AssertSources(
        KhmerShaperPreparation preparation,
        int[] expected)
    {
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(preparation.GetSourceIndex(index),
                Is.EqualTo(expected[index]),
                "source index at " + index);
            index++;
        }
    }

    static void AssertValues(
        KhmerShaperPreparation preparation,
        int kind,
        bool[] expected)
    {
        int index = 0;
        while (index < expected.Length)
        {
            bool actual;
            if (kind == 0)
            {
                actual = preparation.IsPrebaseFormEnabled(index);
            }
            else
            {
                actual = preparation.IsConsonantAfterRoEnabled(index);
            }

            Assert.That(actual, Is.EqualTo(expected[index]),
                "feature value at " + index);
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
