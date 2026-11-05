namespace ttf.Tests;

public class ArabicShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();
    static int CharacterComposition = Tag("ccmp");
    static int ContextualAlternates = Tag("calt");
    static int ContextualLigatures = Tag("clig");
    static int CursivePositioning = Tag("curs");
    static int DiscretionaryLigatures = Tag("dlig");
    static int Final2Forms = Tag("fin2");
    static int Final3Forms = Tag("fin3");
    static int FinalForms = Tag("fina");
    static int InitialForms = Tag("init");
    static int IsolatedForms = Tag("isol");
    static int Kerning = Tag("kern");
    static int Ligatures = Tag("liga");
    static int LocalizedForms = Tag("locl");
    static int MarkPositioning = Tag("mark");
    static int MarkSubstitution = Tag("mset");
    static int MarkToMarkPositioning = Tag("mkmk");
    static int Medial2Forms = Tag("med2");
    static int MedialForms = Tag("medi");
    static int RequiredContextualAlternates = Tag("rclt");
    static int RequiredLigatures = Tag("rlig");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void ResolvesArabicJoiningTypesGroupsAndContextualForms()
    {
        ArabicJoiningAnalysis connected = Analyze(
            "\u0628\u0628\u0628",
            ArabicShapingScript.Arabic);
        ArabicJoiningAnalysis rightJoining = Analyze(
            "\u0627",
            ArabicShapingScript.Arabic);

        Assert.Multiple(() =>
        {
            Assert.That(connected.GetJoiningType(0),
                Is.EqualTo(ArabicJoiningType.DualJoining));
            Assert.That(connected.GetJoiningGroup(0), Is.EqualTo("Beh"));
            AssertActions(connected, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.Medial,
                ArabicJoiningAction.Final
            });
            Assert.That(rightJoining.GetJoiningType(0),
                Is.EqualTo(ArabicJoiningType.RightJoining));
            Assert.That(rightJoining.GetJoiningGroup(0),
                Is.EqualTo("Alef"));
            Assert.That(rightJoining.GetAction(0),
                Is.EqualTo(ArabicJoiningAction.Isolated));
        });
    }

    [Test]
    public void TreatsMarksAsTransparentAndHonorsJoinerControls()
    {
        ArabicJoiningAnalysis marked = Analyze(
            "\u0628\u064E\u062A",
            ArabicShapingScript.Arabic);
        ArabicJoiningAnalysis nonJoiner = Analyze(
            "\u0628\u200C\u062A",
            ArabicShapingScript.Arabic);
        ArabicJoiningAnalysis joiner = Analyze(
            "\u0628\u200D\u062A",
            ArabicShapingScript.Arabic);

        Assert.Multiple(() =>
        {
            Assert.That(marked.GetJoiningType(1),
                Is.EqualTo(ArabicJoiningType.Transparent));
            AssertActions(marked, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.None,
                ArabicJoiningAction.Final
            });
            Assert.That(nonJoiner.GetJoiningType(1),
                Is.EqualTo(ArabicJoiningType.NonJoining));
            AssertActions(nonJoiner, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Isolated,
                ArabicJoiningAction.None,
                ArabicJoiningAction.Isolated
            });
            Assert.That(joiner.GetJoiningType(1),
                Is.EqualTo(ArabicJoiningType.JoinCausing));
            AssertActions(joiner, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.Medial,
                ArabicJoiningAction.Final
            });
        });
    }

    [Test]
    public void AppliesSyriacAlaphAndDalathRishSpecialCases()
    {
        ArabicJoiningAnalysis final2 = Analyze(
            "\u0710\u0710",
            ArabicShapingScript.Syriac);
        ArabicJoiningAnalysis final3 = Analyze(
            "\u0715\u0710",
            ArabicShapingScript.Syriac);
        ArabicJoiningAnalysis medial2 = Analyze(
            "\u0712\u0710\u0715",
            ArabicShapingScript.Syriac);

        Assert.Multiple(() =>
        {
            Assert.That(final2.GetJoiningGroup(0),
                Is.EqualTo("Alaph"));
            AssertActions(final2, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Isolated,
                ArabicJoiningAction.Final2
            });
            Assert.That(final3.GetJoiningGroup(0),
                Is.EqualTo("Dalath_Rish"));
            AssertActions(final3, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Isolated,
                ArabicJoiningAction.Final3
            });
            AssertActions(medial2, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.Medial2,
                ArabicJoiningAction.Isolated
            });
        });
    }

    [Test]
    public void PropagatesMongolianActionsToVariationSelectorsOnly()
    {
        string text = "\u1820\u180B\u1821";
        ArabicJoiningAnalysis mongolian = Analyze(
            text,
            ArabicShapingScript.Mongolian);
        ArabicJoiningAnalysis arabic = Analyze(
            text,
            ArabicShapingScript.Arabic);

        Assert.Multiple(() =>
        {
            AssertActions(mongolian, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.Final
            });
            AssertActions(arabic, new ArabicJoiningAction[]
            {
                ArabicJoiningAction.Initial,
                ArabicJoiningAction.None,
                ArabicJoiningAction.Final
            });
            Assert.That(mongolian.GetJoiningType(1),
                Is.EqualTo(ArabicJoiningType.Transparent));
        });
    }

    [Test]
    public void BuildsOrderedSubstitutionAndPositioningStages()
    {
        ArabicShaperPlan plan = BuildPlan(
            "\u0628",
            ArabicShapingScript.Arabic,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetStageCount(), Is.EqualTo(19));
            Assert.That(plan.GetScript(),
                Is.EqualTo(ArabicShapingScript.Arabic));
            AssertStage(plan, 0,
                ArabicShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 1,
                ArabicShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 2,
                ArabicShaperStageKind.IsolatedForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 9,
                ArabicShaperStageKind.RequiredLigatures,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 14,
                ArabicShaperStageKind.UserSubstitution,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 15,
                ArabicShaperStageKind.CursivePositioning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 17,
                ArabicShaperStageKind.MarkPositioning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 18,
                ArabicShaperStageKind.UserPositioning,
                DefaultShaperTableKind.Positioning);
            Assert.That(plan.FindStage(
                ArabicShaperStageKind.CursivePositioning),
                Is.SameAs(plan.GetStage(15)));
        });
    }

    [Test]
    public void ConvertsJoiningActionsIntoExclusiveFeatureRanges()
    {
        ArabicShaperPlan plan = BuildPlan(
            "\u0628\u0628\u0628",
            ArabicShapingScript.Arabic,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            AssertValues(Plan(plan, 2).FindEntry(IsolatedForms),
                new int[] { 0, 0, 0 });
            AssertValues(Plan(plan, 3).FindEntry(FinalForms),
                new int[] { 0, 0, 1 });
            AssertValues(Plan(plan, 6).FindEntry(MedialForms),
                new int[] { 0, 1, 0 });
            AssertValues(Plan(plan, 8).FindEntry(InitialForms),
                new int[] { 1, 0, 0 });
            Assert.That(Plan(plan, 4).GetEntryCount(), Is.Zero);
            Assert.That(Plan(plan, 5).GetEntryCount(), Is.Zero);
            Assert.That(Plan(plan, 7).GetEntryCount(), Is.Zero);
        });
    }

    [Test]
    public void ExposesSyriacOnlyContextualFeatureRanges()
    {
        ArabicShaperPlan plan = BuildPlan(
            "\u0712\u0710\u0715\u0710",
            ArabicShapingScript.Syriac,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            AssertValues(Plan(plan, 4).FindEntry(Final2Forms),
                new int[] { 0, 0, 0, 0 });
            AssertValues(Plan(plan, 5).FindEntry(Final3Forms),
                new int[] { 0, 0, 0, 1 });
            AssertValues(Plan(plan, 7).FindEntry(Medial2Forms),
                new int[] { 0, 1, 0, 0 });
        });
    }

    [Test]
    public void AppliesArabicFeaturePoliciesAndRoutesUserFeatures()
    {
        ArabicShaperPlan plan = BuildPlan(
            "\u0628\u0628\u0628",
            ArabicShapingScript.Arabic,
            new SfntUserFeature[]
            {
                Feature(CharacterComposition, 0, 0, 3),
                Feature(Ligatures, 0, 1, 2),
                Feature(DiscretionaryLigatures, 2, 0, 1),
                Feature(CursivePositioning, 0, 0, 3),
                Feature(StylisticAlternates, 3, 1, 3)
            });

        Assert.Multiple(() =>
        {
            AssertEntry(Plan(plan, 0), CharacterComposition,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 1), LocalizedForms,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 9), RequiredLigatures,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 10), RequiredContextualAlternates,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 11), ContextualAlternates,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 12), Ligatures,
                SfntLayoutFeatureCategory.Default,
                new int[] { 1, 0, 1 });
            AssertEntry(Plan(plan, 12), ContextualLigatures,
                SfntLayoutFeatureCategory.Default,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 12), DiscretionaryLigatures,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 2, 0, 0 });
            AssertEntry(Plan(plan, 13), MarkSubstitution,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 15), CursivePositioning,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 16), Kerning,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 17), MarkPositioning,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 17), MarkToMarkPositioning,
                SfntLayoutFeatureCategory.Mandatory,
                new int[] { 1, 1, 1 });
            AssertEntry(Plan(plan, 14), StylisticAlternates,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 0, 3, 3 });
            AssertEntry(Plan(plan, 18), StylisticAlternates,
                SfntLayoutFeatureCategory.Discretionary,
                new int[] { 0, 3, 3 });
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArguments()
    {
        ShapingBuffer buffer = CreateBuffer("");
        ArabicShaperPlan empty = ArabicShaper.BuildPlan(
            buffer,
            UnicodeData,
            ArabicShapingScript.Arabic,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetJoiningAnalysis().GetCount(), Is.Zero);
            Assert.That(Plan(empty, 0).GetSelectionCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                ArabicJoiningAnalyzer.Analyze(
                    null!,
                    UnicodeData,
                    ArabicShapingScript.Arabic));
            Assert.Throws<ArgumentNullException>(() =>
                ArabicJoiningAnalyzer.Analyze(
                    buffer,
                    null!,
                    ArabicShapingScript.Arabic));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ArabicJoiningAnalyzer.Analyze(
                    buffer,
                    UnicodeData,
                    (ArabicShapingScript)3));
            Assert.Throws<ArgumentNullException>(() => ArabicShaper.BuildPlan(
                null!,
                UnicodeData,
                ArabicShapingScript.Arabic,
                new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() => ArabicShaper.BuildPlan(
                buffer,
                null!,
                ArabicShapingScript.Arabic,
                new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() => ArabicShaper.BuildPlan(
                buffer,
                UnicodeData,
                ArabicShapingScript.Arabic,
                null!));
            Assert.Throws<ArgumentException>(() => ArabicShaper.BuildPlan(
                buffer,
                UnicodeData,
                ArabicShapingScript.Arabic,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetStage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetStage(19));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.FindStage((ArabicShaperStageKind)19));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetJoiningAnalysis().GetAction(0));
            Assert.Throws<ArgumentNullException>(() =>
                empty.GetStage(0).CreateSelection(null!, 0, 0));
        });
    }

    static ArabicJoiningAnalysis Analyze(
        string text,
        ArabicShapingScript script)
    {
        return ArabicJoiningAnalyzer.Analyze(
            CreateBuffer(text),
            UnicodeData,
            script);
    }

    static ArabicShaperPlan BuildPlan(
        string text,
        ArabicShapingScript script,
        SfntUserFeature[] features)
    {
        return ArabicShaper.BuildPlan(
            CreateBuffer(text),
            UnicodeData,
            script,
            features);
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
        ArabicShaperPlan plan,
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
        ArabicShaperPlan plan,
        int index,
        ArabicShaperStageKind kind,
        DefaultShaperTableKind tableKind)
    {
        Assert.That(plan.GetStage(index).GetKind(), Is.EqualTo(kind));
        Assert.That(plan.GetStage(index).GetTableKind(),
            Is.EqualTo(tableKind));
    }

    static void AssertActions(
        ArabicJoiningAnalysis analysis,
        ArabicJoiningAction[] expected)
    {
        Assert.That(analysis.GetCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(analysis.GetAction(index),
                Is.EqualTo(expected[index]),
                "joining action at index " + index);
            index++;
        }
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
        AssertValues(entry, expected);
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
