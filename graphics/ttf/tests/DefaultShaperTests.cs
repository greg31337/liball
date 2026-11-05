namespace ttf.Tests;

public class DefaultShaperTests
{
    static int CharacterComposition = Tag("ccmp");
    static int ContextualLigatures = Tag("clig");
    static int Denominators = Tag("dnom");
    static int Fractions = Tag("frac");
    static int Kerning = Tag("kern");
    static int Ligatures = Tag("liga");
    static int LocalizedForms = Tag("locl");
    static int MarkPositioning = Tag("mark");
    static int MarkToMarkPositioning = Tag("mkmk");
    static int Numerators = Tag("numr");
    static int RequiredLigatures = Tag("rlig");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void BuildsDefaultStagesInDeterministicExecutionOrder()
    {
        DefaultShaperPlan plan = BuildPlan("abc", new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetStageCount(), Is.EqualTo(8));
            Assert.That(plan.GetTextLength(), Is.EqualTo(3));
            AssertStage(plan, 0, DefaultShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 1,
                DefaultShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 2, DefaultShaperStageKind.Fractions,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 3, DefaultShaperStageKind.Ligatures,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 4, DefaultShaperStageKind.UserSubstitution,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 5, DefaultShaperStageKind.Kerning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 6, DefaultShaperStageKind.Marks,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 7, DefaultShaperStageKind.UserPositioning,
                DefaultShaperTableKind.Positioning);
            Assert.That(plan.FindStage(DefaultShaperStageKind.Kerning),
                Is.SameAs(plan.GetStage(5)));
        });
    }

    [Test]
    public void AssignsMandatoryAndDefaultFeaturePolicies()
    {
        DefaultShaperPlan plan = BuildPlan("abcd", new SfntUserFeature[]
        {
            Feature(LocalizedForms, 0, 0, 4),
            Feature(CharacterComposition, 0, 0, 4),
            Feature(RequiredLigatures, 0, 0, 4),
            Feature(Ligatures, 0, 1, 3),
            Feature(Kerning, 0, 2, 4),
            Feature(MarkPositioning, 0, 3, 4)
        });
        SfntLayoutFeaturePlan localization = plan.GetStage(0)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan composition = plan.GetStage(1)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan ligatures = plan.GetStage(3)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan kerning = plan.GetStage(5)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan marks = plan.GetStage(6)
            .GetFeaturePlan();

        Assert.Multiple(() =>
        {
            AssertEntry(localization, LocalizedForms,
                SfntLayoutFeatureCategory.Mandatory, 1, 1);
            AssertEntry(composition, CharacterComposition,
                SfntLayoutFeatureCategory.Mandatory, 1, 1);
            AssertEntry(ligatures, RequiredLigatures,
                SfntLayoutFeatureCategory.Mandatory, 1, 1);
            AssertEntry(ligatures, Ligatures,
                SfntLayoutFeatureCategory.Default, 1, 1);
            Assert.That(ligatures.FindEntry(Ligatures)!.GetValueAt(1),
                Is.Zero);
            AssertEntry(ligatures, ContextualLigatures,
                SfntLayoutFeatureCategory.Default, 1, 1);
            AssertEntry(kerning, Kerning,
                SfntLayoutFeatureCategory.Default, 1, 0);
            AssertEntry(marks, MarkPositioning,
                SfntLayoutFeatureCategory.Default, 1, 0);
            AssertEntry(marks, MarkToMarkPositioning,
                SfntLayoutFeatureCategory.Default, 1, 1);
        });
    }

    [Test]
    public void DetectsAsciiAndUnicodeDigitFractions()
    {
        DefaultShaperPlan ascii = BuildPlan(
            "a12/34b",
            new SfntUserFeature[0]);
        DefaultShaperPlan arabicIndic = BuildPlan(
            "\u0661\u0662\u2044\u0663",
            new SfntUserFeature[0]);
        SfntLayoutFeaturePlan asciiFractions = ascii.GetStage(2)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan unicodeFractions = arabicIndic.GetStage(2)
            .GetFeaturePlan();

        Assert.Multiple(() =>
        {
            AssertValues(asciiFractions.FindEntry(Fractions),
                new int[] { 0, 1, 1, 1, 1, 1, 0 });
            AssertValues(asciiFractions.FindEntry(Numerators),
                new int[] { 0, 1, 1, 0, 0, 0, 0 });
            AssertValues(asciiFractions.FindEntry(Denominators),
                new int[] { 0, 0, 0, 0, 1, 1, 0 });
            AssertValues(unicodeFractions.FindEntry(Fractions),
                new int[] { 1, 1, 1, 1 });
            AssertValues(unicodeFractions.FindEntry(Numerators),
                new int[] { 1, 1, 0, 0 });
            AssertValues(unicodeFractions.FindEntry(Denominators),
                new int[] { 0, 0, 0, 1 });
        });
    }

    [Test]
    public void UserFractionRangesOverrideAutomaticRanges()
    {
        DefaultShaperPlan plan = BuildPlan("12/34", new SfntUserFeature[]
        {
            Feature(Numerators, 0, 1, 2),
            Feature(Denominators, 3, 3, 4),
            Feature(Fractions, 0, 2, 3)
        });
        SfntLayoutFeaturePlan fractions = plan.GetStage(2)
            .GetFeaturePlan();

        Assert.Multiple(() =>
        {
            AssertValues(fractions.FindEntry(Fractions),
                new int[] { 1, 1, 0, 1, 1 });
            AssertValues(fractions.FindEntry(Numerators),
                new int[] { 1, 0, 0, 0, 0 });
            AssertValues(fractions.FindEntry(Denominators),
                new int[] { 0, 0, 0, 3, 1 });
        });
    }

    [Test]
    public void RoutesCustomUserFeaturesToBothLayoutTablesOnly()
    {
        DefaultShaperPlan plan = BuildPlan("abcd", new SfntUserFeature[]
        {
            Feature(StylisticAlternates, 2, 1, 3),
            Feature(StylisticAlternates, 0, 2, 3),
            Feature(Ligatures, 0, 0, 1),
            Feature(Kerning, 0, 0, 1)
        });
        SfntLayoutFeaturePlan substitution = plan.GetStage(4)
            .GetFeaturePlan();
        SfntLayoutFeaturePlan positioning = plan.GetStage(7)
            .GetFeaturePlan();

        Assert.Multiple(() =>
        {
            Assert.That(substitution.GetEntryCount(), Is.EqualTo(1));
            Assert.That(positioning.GetEntryCount(), Is.EqualTo(1));
            AssertValues(substitution.FindEntry(StylisticAlternates),
                new int[] { 0, 2, 0, 0 });
            AssertValues(positioning.FindEntry(StylisticAlternates),
                new int[] { 0, 2, 0, 0 });
            Assert.That(substitution.FindEntry(Ligatures), Is.Null);
            Assert.That(positioning.FindEntry(Kerning), Is.Null);
            Assert.That(plan.GetStage(3).GetFeaturePlan()
                .FindEntry(Ligatures), Is.Not.Null);
            Assert.That(plan.GetStage(5).GetFeaturePlan()
                .FindEntry(Kerning), Is.Not.Null);
        });
    }

    [Test]
    public void IgnoresSlashesWithoutDigitsOnBothSides()
    {
        DefaultShaperPlan plan = BuildPlan(
            "a/1 2/b /",
            new SfntUserFeature[0]);
        SfntLayoutFeaturePlan fractions = plan.GetStage(2)
            .GetFeaturePlan();

        Assert.Multiple(() =>
        {
            Assert.That(fractions.FindEntry(Fractions)!.GetRangeCount(),
                Is.Zero);
            Assert.That(fractions.FindEntry(Numerators)!.GetRangeCount(),
                Is.Zero);
            Assert.That(fractions.FindEntry(Denominators)!.GetRangeCount(),
                Is.Zero);
            Assert.That(fractions.GetSelectionCount(), Is.Zero);
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArguments()
    {
        DefaultShaperPlan empty = BuildPlan("", new SfntUserFeature[0]);
        ShapingBuffer buffer = CreateBuffer("a");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetTextLength(), Is.Zero);
            Assert.That(empty.GetStage(0).GetFeaturePlan()
                .GetSelectionCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                DefaultShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                DefaultShaper.BuildPlan(buffer, null!));
            Assert.Throws<ArgumentException>(() => DefaultShaper.BuildPlan(
                buffer,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetStage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetStage(8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.FindStage((DefaultShaperStageKind)8));
            Assert.Throws<ArgumentNullException>(() =>
                empty.GetStage(0).CreateSelection(null!, 0, 0));
        });
    }

    static DefaultShaperPlan BuildPlan(
        string text,
        SfntUserFeature[] features)
    {
        return DefaultShaper.BuildPlan(CreateBuffer(text), features);
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
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
        DefaultShaperPlan plan,
        int index,
        DefaultShaperStageKind kind,
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
        int firstValue,
        int lastValue)
    {
        SfntLayoutFeaturePlanEntry? entry = plan.FindEntry(tag);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetCategory(), Is.EqualTo(category));
        Assert.That(entry.GetValueAt(0), Is.EqualTo(firstValue));
        Assert.That(entry.GetValueAt(3), Is.EqualTo(lastValue));
    }

    static void AssertValues(
        SfntLayoutFeaturePlanEntry? entry,
        int[] expected)
    {
        Assert.That(entry, Is.Not.Null);
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(entry!.GetValueAt(index),
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
