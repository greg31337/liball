namespace ttf.Tests;

internal class MyanmarShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();
    static int AbovebaseSubstitutions = Tag("abvs");
    static int BelowbaseForms = Tag("blwf");
    static int CharacterComposition = Tag("ccmp");
    static int LocalizedForms = Tag("locl");
    static int PostbaseForms = Tag("pstf");
    static int PrebaseForms = Tag("pref");
    static int PresentationForms = Tag("pres");
    static int RephForms = Tag("rphf");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void ReordersKinziMedialRaAndVowelsAroundTheBase()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1004\u103A\u1039\u1000\u103C\u1031" +
            "\u102F\u1032\u1037");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);

        Assert.Multiple(() =>
        {
            AssertSources(preparation,
                new int[] { 5, 4, 3, 0, 1, 2, 7, 6, 8 });
            Assert.That(preparation.HasKinzi(0), Is.True);
            Assert.That(preparation.GetBaseSourceIndex(0), Is.EqualTo(3));
            Assert.That(preparation.GetPosition(0),
                Is.EqualTo(MyanmarShapingPosition.PreMatra));
            Assert.That(preparation.GetPosition(1),
                Is.EqualTo(MyanmarShapingPosition.PreConsonant));
            Assert.That(preparation.GetPosition(6),
                Is.EqualTo(MyanmarShapingPosition.BeforeSubjoined));
            Assert.That(preparation.GetPosition(7),
                Is.EqualTo(MyanmarShapingPosition.BelowConsonant));
            Assert.That(preparation.GetPosition(8),
                Is.EqualTo(MyanmarShapingPosition.AfterSubjoined));
        });

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[]
            {
                0x1031,
                0x103C,
                0x1000,
                0x1004,
                0x103A,
                0x1039,
                0x1032,
                0x102F,
                0x1037
            });
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(9));
            Assert.That(buffer.IsUnsafeToBreak(8), Is.True);
        });
    }

    [Test]
    public void OrdersBelowVowelsAndAnusvaraAroundTheSubjoinedBoundary()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1000\u102F\u1030\u1032\u1037");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);

        preparation.ApplyReordering(buffer);

        AssertRunes(buffer, new int[]
        {
            0x1000,
            0x1032,
            0x102F,
            0x1030,
            0x1037
        });
    }

    [Test]
    public void ReversesMultipleLeftMatrasWithoutSeparatingSelectors()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1000\u1031\uFE00\u1031\uFE01");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);

        AssertSources(preparation, new int[] { 3, 4, 1, 2, 0 });
        preparation.ApplyReordering(buffer);
        AssertRunes(buffer, new int[]
        {
            0x1031,
            0xFE01,
            0x1031,
            0xFE00,
            0x1000
        });
    }

    [Test]
    public void InsertsDottedCircleAndPreservesTheBrokenSyllableUnit()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u1004\u103A\u1039\u1031");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.InsertForBrokenClusters);
        MyanmarSyllableMap map = preparation.GetSyllableMap();

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetInsertedDottedCircleCount(),
                Is.EqualTo(1));
            Assert.That(map.GetSyllableCount(), Is.EqualTo(1));
            Assert.That(map.GetSyllableLength(0), Is.EqualTo(5));
            Assert.That(map.GetSyllableType(0),
                Is.EqualTo(MyanmarSyllableType.Broken));
            Assert.That(map.GetCategory(0),
                Is.EqualTo(MyanmarSyllabicCategory.DottedCircle));
            AssertSources(preparation, new int[] { 4, 0, 1, 2, 3 });
        });

        preparation.ApplyReordering(buffer);
        AssertRunes(buffer, new int[]
        {
            0x1031,
            0x25CC,
            0x1004,
            0x103A,
            0x1039
        });
    }

    [Test]
    public void PreservesBrokenClustersWhenDottedCircleInsertionIsDisabled()
    {
        ShapingBuffer buffer = CreateBuffer("\u1031\u102D");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetInsertedDottedCircleCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(preparation.GetSyllableMap().HasBrokenSyllable(),
                Is.True);
        });
    }

    [Test]
    public void InsertsOneDottedCircleIntoEveryBrokenCluster()
    {
        ShapingBuffer buffer = CreateBuffer("\u1031A\u1039");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.InsertForBrokenClusters);
        MyanmarSyllableMap map = preparation.GetSyllableMap();

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetInsertedDottedCircleCount(),
                Is.EqualTo(2));
            Assert.That(buffer.GetCount(), Is.EqualTo(5));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x25CC));
            Assert.That(buffer.GetScalarValue(3), Is.EqualTo(0x25CC));
            Assert.That(map.GetSyllableCount(), Is.EqualTo(3));
            Assert.That(map.GetSyllableLength(0), Is.EqualTo(2));
            Assert.That(map.GetSyllableLength(1), Is.EqualTo(1));
            Assert.That(map.GetSyllableLength(2), Is.EqualTo(2));
        });
    }

    [Test]
    public void BuildsOrderedMyanmarFeatureStages()
    {
        MyanmarShaperPlan plan = BuildPlan(
            "\u1000\u1031",
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetScriptTag(), Is.EqualTo(Tag("mym2")));
            Assert.That(plan.GetStageCount(), Is.EqualTo(13));
            AssertStage(plan, 0, MyanmarShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 1,
                MyanmarShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 2, MyanmarShaperStageKind.Reordering,
                DefaultShaperTableKind.Substitution, true);
            AssertStage(plan, 3, MyanmarShaperStageKind.RephForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 4, MyanmarShaperStageKind.PrebaseForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 5, MyanmarShaperStageKind.BelowbaseForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 6, MyanmarShaperStageKind.PostbaseForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 7, MyanmarShaperStageKind.PresentationForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 9, MyanmarShaperStageKind.Kerning,
                DefaultShaperTableKind.Positioning, false);
            AssertStage(plan, 10, MyanmarShaperStageKind.Distances,
                DefaultShaperTableKind.Positioning, false);
            AssertStage(plan, 11, MyanmarShaperStageKind.MarkPositioning,
                DefaultShaperTableKind.Positioning, false);
            Assert.That(Plan(plan, 0).FindEntry(LocalizedForms), Is.Not.Null);
            Assert.That(Plan(plan, 1).FindEntry(CharacterComposition),
                Is.Not.Null);
            Assert.That(Plan(plan, 3).FindEntry(RephForms), Is.Not.Null);
            Assert.That(Plan(plan, 4).FindEntry(PrebaseForms), Is.Not.Null);
            Assert.That(Plan(plan, 5).FindEntry(BelowbaseForms), Is.Not.Null);
            Assert.That(Plan(plan, 6).FindEntry(PostbaseForms), Is.Not.Null);
            Assert.That(Plan(plan, 7).FindEntry(PresentationForms),
                Is.Not.Null);
            Assert.That(Plan(plan, 7).FindEntry(AbovebaseSubstitutions),
                Is.Not.Null);
        });
    }

    [Test]
    public void RoutesCustomFeaturesAndKeepsRequiredFeaturesEnabled()
    {
        MyanmarShaperPlan plan = BuildPlan(
            "\u1000\u1001\u1002",
            new SfntUserFeature[]
            {
                Feature(RephForms, 0, 0, 3),
                Feature(StylisticAlternates, 2, 1, 3)
            });

        Assert.Multiple(() =>
        {
            AssertFeature(Plan(plan, 3), RephForms,
                new int[] { 1, 1, 1 });
            AssertFeature(Plan(plan, 8), StylisticAlternates,
                new int[] { 0, 2, 2 });
            AssertFeature(Plan(plan, 12), StylisticAlternates,
                new int[] { 0, 2, 2 });
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArgumentsAndState()
    {
        ShapingBuffer emptyBuffer = CreateBuffer("");
        MyanmarShaperPreparation empty = MyanmarShaper.Prepare(
            emptyBuffer,
            UnicodeData,
            MyanmarDottedCirclePolicy.InsertForBrokenClusters,
            0);
        MyanmarShaperPlan emptyPlan = MyanmarShaper.BuildPlan(
            empty,
            new SfntUserFeature[0]);
        ShapingBuffer buffer = CreateBuffer("\u1000");
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);
        ShapingBuffer other = CreateBuffer("\u1000");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetSyllableMap().GetRuneCount(), Is.Zero);
            Assert.That(emptyPlan.GetStageCount(), Is.EqualTo(13));
            Assert.Throws<ArgumentNullException>(() => MyanmarShaper.Prepare(
                null!,
                UnicodeData,
                MyanmarDottedCirclePolicy.InsertForBrokenClusters,
                1));
            Assert.Throws<ArgumentNullException>(() => MyanmarShaper.Prepare(
                buffer,
                null!,
                MyanmarDottedCirclePolicy.InsertForBrokenClusters,
                1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MyanmarShaper.Prepare(
                    buffer,
                    UnicodeData,
                    (MyanmarDottedCirclePolicy)2,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MyanmarShaper.Prepare(
                    buffer,
                    UnicodeData,
                    MyanmarDottedCirclePolicy.InsertForBrokenClusters,
                    -1));
            Assert.Throws<ArgumentNullException>(() =>
                MyanmarShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                MyanmarShaper.BuildPlan(preparation, null!));
            Assert.Throws<ArgumentException>(() => MyanmarShaper.BuildPlan(
                preparation,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentException>(() =>
                preparation.ApplyReordering(other));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetSourceIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetBaseSourceIndex(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.GetStage(13));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.FindStage((MyanmarShaperStageKind)13));
        });

        empty.ApplyReordering(emptyBuffer);
        preparation.ApplyReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyReordering(buffer));
    }

    static MyanmarShaperPreparation Prepare(
        ShapingBuffer buffer,
        MyanmarDottedCirclePolicy policy)
    {
        return MyanmarShaper.Prepare(
            buffer,
            UnicodeData,
            policy,
            buffer.GetCount() + 1);
    }

    static MyanmarShaperPlan BuildPlan(
        string text,
        SfntUserFeature[] features)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        MyanmarShaperPreparation preparation = Prepare(
            buffer,
            MyanmarDottedCirclePolicy.PreserveBrokenClusters);
        return MyanmarShaper.BuildPlan(preparation, features);
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
        MyanmarShaperPlan plan,
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
        MyanmarShaperPlan plan,
        int index,
        MyanmarShaperStageKind kind,
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
        MyanmarShaperPreparation preparation,
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
