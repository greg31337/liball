namespace ttf.Tests;

public class HebrewShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();
    static int CharacterComposition = Tag("ccmp");
    static int DiscretionaryLigatures = Tag("dlig");
    static int Kerning = Tag("kern");
    static int MarkPositioning = Tag("mark");
    static int MarkToMarkPositioning = Tag("mkmk");
    static int StylisticAlternates = Tag("salt");

    [TestCase("\u05D9\u05B4", 0xFB1D)]
    [TestCase("\u05F2\u05B7", 0xFB1F)]
    [TestCase("\u05D0\u05B7", 0xFB2E)]
    [TestCase("\u05D0\u05B8", 0xFB2F)]
    [TestCase("\u05D5\u05B9", 0xFB4B)]
    [TestCase("\u05D1\u05BC", 0xFB31)]
    [TestCase("\u05D1\u05BF", 0xFB4C)]
    [TestCase("\u05DB\u05BF", 0xFB4D)]
    [TestCase("\u05E4\u05BF", 0xFB4E)]
    public void ComposesLegacyPresentationFormsWhenMarksLackGpos(
        string text,
        int expected)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPresentationCompositionCount(),
                Is.EqualTo(1));
            Assert.That(result.GetMarkReorderCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(expected));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(buffer.GetSourceLength(), Is.EqualTo(2));
            Assert.That(buffer.GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(buffer.HasGlyphId(0), Is.False);
        });
    }

    [TestCase("\u05E9\u05C1\u05BC", 0xFB2C)]
    [TestCase("\u05E9\u05BC\u05C1", 0xFB2C)]
    [TestCase("\u05E9\u05C2\u05BC", 0xFB2D)]
    [TestCase("\u05E9\u05BC\u05C2", 0xFB2D)]
    public void ComposesChainedShinAndSinPresentationForms(
        string text,
        int expected)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPresentationCompositionCount(),
                Is.EqualTo(2));
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(expected));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
        });
    }

    [Test]
    public void PreservesMarksWhenGposMarkPositioningIsAvailable()
    {
        ShapingBuffer buffer = CreateBuffer("\u05D1\u05BC");
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPresentationCompositionCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x05D1));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x05BC));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
        });
    }

    [Test]
    public void LeavesUnsupportedPresentationPairsDecomposed()
    {
        ShapingBuffer buffer = CreateBuffer("\u05D7\u05BC");
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetPresentationCompositionCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x05D7));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x05BC));
        });
    }

    [TestCase(0x05BD)]
    [TestCase(0x0323)]
    public void ReordersHebrewMetegAndBelowMarksWithSafeClusters(
        int trailingMark)
    {
        string text = "\u05D0\u05B7\u05B4" +
            char.ConvertFromUtf32(trailingMark);
        ShapingBuffer buffer = CreateBuffer(text);
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetMarkReorderCount(), Is.EqualTo(1));
            Assert.That(buffer.GetCount(), Is.EqualTo(4));
            Assert.That(buffer.GetScalarValue(0), Is.EqualTo(0x05D0));
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x05B7));
            Assert.That(buffer.GetScalarValue(2), Is.EqualTo(trailingMark));
            Assert.That(buffer.GetScalarValue(3), Is.EqualTo(0x05B4));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(2));
            Assert.That(buffer.GetClusterStart(3), Is.EqualTo(2));
            Assert.That(buffer.GetClusterLength(2), Is.EqualTo(2));
            Assert.That(buffer.GetClusterLength(3), Is.EqualTo(2));
            Assert.That(buffer.GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
        });
    }

    [Test]
    public void ReordersEachHebrewMarkSequenceIndependently()
    {
        string marks = "\u05B7\u05B4\u05BD";
        ShapingBuffer buffer = CreateBuffer(
            "\u05D0" + marks + "\u05D1" + marks);
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetMarkReorderCount(), Is.EqualTo(2));
            Assert.That(buffer.GetScalarValue(2), Is.EqualTo(0x05BD));
            Assert.That(buffer.GetScalarValue(3), Is.EqualTo(0x05B4));
            Assert.That(buffer.GetScalarValue(6), Is.EqualTo(0x05BD));
            Assert.That(buffer.GetScalarValue(7), Is.EqualTo(0x05B4));
            Assert.That(buffer.GetClusterStart(2),
                Is.EqualTo(buffer.GetClusterStart(3)));
            Assert.That(buffer.GetClusterStart(6),
                Is.EqualTo(buffer.GetClusterStart(7)));
        });
    }

    [Test]
    public void DoesNotReorderOtherCanonicalMarkSequences()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u05D0\u05B4\u05B7\u05BD");
        HebrewShaperPreparation result = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetMarkReorderCount(), Is.Zero);
            Assert.That(buffer.GetScalarValue(1), Is.EqualTo(0x05B4));
            Assert.That(buffer.GetScalarValue(2), Is.EqualTo(0x05B7));
            Assert.That(buffer.GetScalarValue(3), Is.EqualTo(0x05BD));
            Assert.That(buffer.GetClusterStart(1), Is.EqualTo(1));
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(2));
            Assert.That(buffer.GetClusterStart(3), Is.EqualTo(3));
        });
    }

    [Test]
    public void BuildsHebrewFeaturesInDeterministicOrder()
    {
        HebrewShaperPlan plan = HebrewShaper.BuildPlan(
            CreateBuffer("\u05D0"),
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetStageCount(), Is.EqualTo(6));
            AssertStage(plan, 0,
                HebrewShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 1,
                HebrewShaperStageKind.DiscretionaryLigatures,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 2,
                HebrewShaperStageKind.UserSubstitution,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 3,
                HebrewShaperStageKind.Kerning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 4,
                HebrewShaperStageKind.MarkPositioning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 5,
                HebrewShaperStageKind.UserPositioning,
                DefaultShaperTableKind.Positioning);
            Assert.That(plan.FindStage(HebrewShaperStageKind.Kerning),
                Is.SameAs(plan.GetStage(3)));
        });
    }

    [Test]
    public void AppliesFeaturePoliciesAndRoutesUserFeatures()
    {
        HebrewShaperPlan plan = HebrewShaper.BuildPlan(
            CreateBuffer("\u05D0\u05D1\u05D2"),
            new SfntUserFeature[]
            {
                Feature(CharacterComposition, 0, 0, 3),
                Feature(DiscretionaryLigatures, 2, 0, 1),
                Feature(Kerning, 0, 1, 2),
                Feature(MarkPositioning, 0, 0, 3),
                Feature(MarkToMarkPositioning, 0, 2, 3),
                Feature(StylisticAlternates, 3, 1, 3)
            });

        Assert.Multiple(() =>
        {
            AssertEntry(Plan(plan, 0), CharacterComposition,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 1), DiscretionaryLigatures,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 2, 0, 0 });
            AssertEntry(Plan(plan, 3), Kerning,
                SfntLayoutFeatureCategory.Default,
                new int[] { 1, 0, 1 });
            AssertEntry(Plan(plan, 4), MarkPositioning,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 4), MarkToMarkPositioning,
                SfntLayoutFeatureCategory.Default,
                new int[] { 1, 1, 0 });
            AssertEntry(Plan(plan, 2), StylisticAlternates,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 0, 3, 3 });
            AssertEntry(Plan(plan, 5), StylisticAlternates,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 0, 3, 3 });
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArguments()
    {
        ShapingBuffer buffer = CreateBuffer("");
        HebrewShaperPreparation preparation = HebrewShaper.Prepare(
            buffer,
            UnicodeData,
            false);
        HebrewShaperPlan plan = HebrewShaper.BuildPlan(
            buffer,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetPresentationCompositionCount(),
                Is.Zero);
            Assert.That(preparation.GetMarkReorderCount(), Is.Zero);
            Assert.That(Plan(plan, 0).GetSelectionCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                HebrewShaper.Prepare(null!, UnicodeData, false));
            Assert.Throws<ArgumentNullException>(() =>
                HebrewShaper.Prepare(buffer, null!, false));
            Assert.Throws<ArgumentNullException>(() =>
                HebrewShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                HebrewShaper.BuildPlan(buffer, null!));
            Assert.Throws<ArgumentException>(() =>
                HebrewShaper.BuildPlan(
                    buffer,
                    new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plan.GetStage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plan.GetStage(6));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plan.FindStage((HebrewShaperStageKind)6));
            Assert.Throws<ArgumentNullException>(() =>
                plan.GetStage(0).CreateSelection(null!, 0, 0));
        });
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.RightToLeft);
    }

    static SfntLayoutFeaturePlan Plan(
        HebrewShaperPlan plan,
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
        HebrewShaperPlan plan,
        int index,
        HebrewShaperStageKind kind,
        DefaultShaperTableKind tableKind)
    {
        Assert.That(plan.GetStage(index).GetKind(), Is.EqualTo(kind));
        Assert.That(plan.GetStage(index).GetTableKind(),
            Is.EqualTo(tableKind));
    }

    static void AssertEntry(
        SfntLayoutFeaturePlan plan,
        int tag,
        SfntLayoutFeatureCategory category,
        int[] expected)
    {
        SfntLayoutFeaturePlanEntry? entry = plan.FindEntry(tag);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetCategory(), Is.EqualTo(category));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(entry.GetValueAt(index),
                Is.EqualTo(expected[index]),
                "feature value at index " + index);
            index++;
        }
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
