namespace ttf.Tests;

internal class IndicShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();
    static int AboveMarkPositioning = Tag("abvm");
    static int CharacterComposition = Tag("ccmp");
    static int HalfForms = Tag("half");
    static int InitialForms = Tag("init");
    static int LocalizedForms = Tag("locl");
    static int PrebaseForms = Tag("pref");
    static int RephForms = Tag("rphf");
    static int StylisticAlternates = Tag("salt");

    [TestCase(IndicShapingScript.Devanagari, "deva", "dev2", 0x094D)]
    [TestCase(IndicShapingScript.Bengali, "beng", "bng2", 0x09CD)]
    [TestCase(IndicShapingScript.Gurmukhi, "guru", "gur2", 0x0A4D)]
    [TestCase(IndicShapingScript.Gujarati, "gujr", "gjr2", 0x0ACD)]
    [TestCase(IndicShapingScript.Oriya, "orya", "ory2", 0x0B4D)]
    [TestCase(IndicShapingScript.Tamil, "taml", "tml2", 0x0BCD)]
    [TestCase(IndicShapingScript.Telugu, "telu", "tel2", 0x0C4D)]
    [TestCase(IndicShapingScript.Kannada, "knda", "knd2", 0x0CCD)]
    [TestCase(IndicShapingScript.Malayalam, "mlym", "mlm2", 0x0D4D)]
    public void ExposesOldAndNewScriptTagsAndViramas(
        IndicShapingScript script,
        string oldTag,
        string newTag,
        int virama)
    {
        IndicShaperPlan oldPlan = BuildPlan(
            ScriptSample(script),
            script,
            IndicShapingModel.Version1,
            new SfntUserFeature[0]);
        IndicShaperPlan newPlan = BuildPlan(
            ScriptSample(script),
            script,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(oldPlan.GetScriptTag(), Is.EqualTo(Tag(oldTag)));
            Assert.That(oldPlan.GetFallbackScriptTag(),
                Is.EqualTo(Tag(newTag)));
            Assert.That(newPlan.GetScriptTag(), Is.EqualTo(Tag(newTag)));
            Assert.That(newPlan.GetFallbackScriptTag(),
                Is.EqualTo(Tag(oldTag)));
            Assert.That(oldPlan.GetVirama(), Is.EqualTo(virama));
            Assert.That(newPlan.GetVirama(), Is.EqualTo(virama));
        });
    }

    [Test]
    public void PrefersVersion2TablesAndFallsBackToVersion1Tables()
    {
        SfntLayoutTable both = CreateLayoutTable(
            new int[] { Tag("dev2"), Tag("deva") });
        SfntLayoutTable oldOnly = CreateLayoutTable(
            new int[] { Tag("deva") });
        SfntLayoutTable neither = CreateLayoutTable(
            new int[] { Tag("latn") });

        Assert.Multiple(() =>
        {
            Assert.That(IndicShaper.SelectModel(
                both,
                IndicShapingScript.Devanagari),
                Is.EqualTo(IndicShapingModel.Version2));
            Assert.That(IndicShaper.SelectModel(
                oldOnly,
                IndicShapingScript.Devanagari),
                Is.EqualTo(IndicShapingModel.Version1));
            Assert.That(IndicShaper.SelectModel(
                neither,
                IndicShapingScript.Devanagari),
                Is.EqualTo(IndicShapingModel.Version2));
        });
    }

    [Test]
    public void SelectsTheLastConsonantAsBaseAndReordersLeftMatraInTwoPasses()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u0915\u094D\u0917\u093F");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetBaseSourceIndex(0), Is.EqualTo(2));
            Assert.That(preparation.GetInitialBaseIndex(0), Is.EqualTo(3));
            Assert.That(preparation.GetFinalBaseIndex(0), Is.EqualTo(3));
            AssertSources(preparation, true, new int[] { 3, 0, 1, 2 });
            AssertSources(preparation, false, new int[] { 0, 1, 3, 2 });
        });

        preparation.ApplyInitialReordering(buffer);
        AssertRunes(buffer, new int[] { 0x093F, 0x0915, 0x094D, 0x0917 });
        preparation.ApplyFinalReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer,
                new int[] { 0x0915, 0x094D, 0x093F, 0x0917 });
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(4));
            Assert.That(buffer.IsUnsafeToBreak(2), Is.True);
        });
    }

    [Test]
    public void KeepsLeftMatraBeforeAJoinerRequestedHalfForm()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u0915\u094D\u200D\u0937\u093F");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);

        preparation.ApplyInitialReordering(buffer);
        preparation.ApplyFinalReordering(buffer);

        AssertRunes(buffer, new int[]
        {
            0x093F,
            0x0915,
            0x094D,
            0x200D,
            0x0937
        });
    }

    [Test]
    public void ReordersImplicitRephAfterTheBaseBeforeMarks()
    {
        ShapingBuffer buffer = CreateBuffer(
            "\u0930\u094D\u0915\u093E\u0902");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);

        Assert.That(preparation.HasReph(0), Is.True);
        preparation.ApplyInitialReordering(buffer);
        preparation.ApplyFinalReordering(buffer);

        AssertRunes(buffer, new int[]
        {
            0x0915,
            0x0930,
            0x094D,
            0x093E,
            0x0902
        });
    }

    [Test]
    public void UsesExplicitRephForTeluguAndDoesNotInferItWithoutJoiner()
    {
        IndicShaperPreparation explicitReph = Prepare(
            CreateBuffer("\u0C30\u0C4D\u200D\u0C15\u0C3E"),
            IndicShapingScript.Telugu,
            IndicShapingModel.Version2);
        IndicShaperPreparation implicitSequence = Prepare(
            CreateBuffer("\u0C30\u0C4D\u0C15\u0C3E"),
            IndicShapingScript.Telugu,
            IndicShapingModel.Version2);
        IndicShaperPreparation devanagariJoiner = Prepare(
            CreateBuffer("\u0930\u094D\u200D\u0915"),
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(explicitReph.HasReph(0), Is.True);
            Assert.That(explicitReph.GetRephLength(0), Is.EqualTo(3));
            Assert.That(implicitSequence.HasReph(0), Is.False);
            Assert.That(devanagariJoiner.HasReph(0), Is.False);
        });
    }

    [Test]
    public void ReordersLogicalMalayalamRepha()
    {
        ShapingBuffer buffer = CreateBuffer("\u0D4E\u0D15\u0D3E");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Malayalam,
            IndicShapingModel.Version2);

        Assert.That(preparation.HasReph(0), Is.True);
        preparation.ApplyInitialReordering(buffer);
        preparation.ApplyFinalReordering(buffer);

        AssertRunes(buffer, new int[] { 0x0D15, 0x0D4E, 0x0D3E });
    }

    [Test]
    public void MovesHalantRaPairToThePrebasePosition()
    {
        ShapingBuffer buffer = CreateBuffer("\u0915\u094D\u0930");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.HasPrebaseConsonant(0), Is.True);
            Assert.That(preparation.GetBaseSourceIndex(0), Is.Zero);
        });

        preparation.ApplyInitialReordering(buffer);
        preparation.ApplyFinalReordering(buffer);
        AssertRunes(buffer, new int[] { 0x094D, 0x0930, 0x0915 });
    }

    [Test]
    public void BuildsBasicFeaturesAsSeparateStagesAroundReorderingBarriers()
    {
        IndicShaperPlan plan = BuildPlan(
            "\u0930\u094D\u0915\u094D\u0937\u093F",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetStageCount(), Is.EqualTo(28));
            AssertStage(plan, 0, IndicShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 1,
                IndicShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 2, IndicShaperStageKind.InitialReordering,
                DefaultShaperTableKind.Substitution, true);
            AssertStage(plan, 3, IndicShaperStageKind.NuktaForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 14, IndicShaperStageKind.FinalReordering,
                DefaultShaperTableKind.Substitution, true);
            AssertStage(plan, 15, IndicShaperStageKind.InitialForms,
                DefaultShaperTableKind.Substitution, false);
            AssertStage(plan, 22, IndicShaperStageKind.Distances,
                DefaultShaperTableKind.Positioning, false);
            AssertStage(plan, 27, IndicShaperStageKind.UserPositioning,
                DefaultShaperTableKind.Positioning, false);
            Assert.That(plan.FindStage(IndicShaperStageKind.HalfForms),
                Is.SameAs(plan.GetStage(10)));
            Assert.That(Plan(plan, 0).FindEntry(LocalizedForms),
                Is.Not.Null);
            Assert.That(Plan(plan, 1).FindEntry(CharacterComposition),
                Is.Not.Null);
            Assert.That(Plan(plan, 23).FindEntry(AboveMarkPositioning),
                Is.Not.Null);
        });
    }

    [Test]
    public void LimitsRephPrebaseHalfAndInitialFeaturesToEligibleRunes()
    {
        IndicShaperPlan reph = BuildPlan(
            "\u0930\u094D\u0915",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);
        IndicShaperPlan prebase = BuildPlan(
            "\u0915\u094D\u0930",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);
        IndicShaperPlan half = BuildPlan(
            "\u0915\u094D\u200D\u0937",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);
        IndicShaperPlan nonJoiner = BuildPlan(
            "\u0915\u094D\u200C",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);
        IndicShaperPlan initial = BuildPlan(
            "\u0915\u093F",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(initial.GetPreparation().GetFinalSourceIndex(0),
                Is.EqualTo(1));
            Assert.That(initial.GetPreparation().GetFinalCategory(0),
                Is.EqualTo(IndicSyllabicCategory.Matra));
            Assert.That(initial.GetPreparation().GetFinalPosition(0),
                Is.EqualTo(IndicPositionalCategory.Left));
            AssertValues(Plan(reph, 5).FindEntry(RephForms),
                new int[] { 1, 1, 0 });
            AssertValues(Plan(prebase, 7).FindEntry(PrebaseForms),
                new int[] { 0, 1, 1 });
            AssertValues(Plan(half, 10).FindEntry(HalfForms),
                new int[] { 1, 1, 1, 0 });
            AssertValues(Plan(nonJoiner, 10).FindEntry(HalfForms),
                new int[] { 0, 0, 0 });
            AssertValues(Plan(initial, 15).FindEntry(InitialForms),
                new int[] { 1, 0 });
        });
    }

    [Test]
    public void SupportsEmptyBuffersThroughBothReorderingBarriers()
    {
        ShapingBuffer buffer = CreateBuffer("");
        IndicShaperPreparation preparation = IndicShaper.Prepare(
            buffer,
            UnicodeData,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2,
            0);
        IndicShaperPlan plan = IndicShaper.BuildPlan(
            preparation,
            new SfntUserFeature[0]);

        preparation.ApplyInitialReordering(buffer);
        preparation.ApplyFinalReordering(buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetCount(), Is.Zero);
            Assert.That(preparation.GetSyllableCount(), Is.Zero);
            Assert.That(preparation.IsInitialReorderingApplied(), Is.True);
            Assert.That(preparation.IsFinalReorderingApplied(), Is.True);
            Assert.That(plan.GetStageCount(), Is.EqualTo(28));
            Assert.That(Plan(plan, 5).GetSelectionCount(), Is.Zero);
        });
    }

    [Test]
    public void RoutesCustomFeaturesAndKeepsIndicFeaturesMandatory()
    {
        IndicShaperPlan plan = BuildPlan(
            "\u0915\u0917\u0918",
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version1,
            new SfntUserFeature[]
            {
                Feature(LocalizedForms, 0, 0, 3),
                Feature(StylisticAlternates, 2, 1, 3)
            });

        Assert.Multiple(() =>
        {
            SfntLayoutFeaturePlanEntry? localized =
                Plan(plan, 0).FindEntry(LocalizedForms);
            Assert.That(localized!.GetCategory(),
                Is.EqualTo(SfntLayoutFeatureCategory.Mandatory));
            AssertValues(localized, new int[] { 1, 1, 1 });
            AssertValues(Plan(plan, 21).FindEntry(StylisticAlternates),
                new int[] { 0, 2, 2 });
            AssertValues(Plan(plan, 27).FindEntry(StylisticAlternates),
                new int[] { 0, 2, 2 });
        });
    }

    [Test]
    public void ValidatesArgumentsAndReorderingState()
    {
        ShapingBuffer buffer = CreateBuffer("\u0915");
        IndicShaperPreparation preparation = Prepare(
            buffer,
            IndicShapingScript.Devanagari,
            IndicShapingModel.Version2);
        ShapingBuffer other = CreateBuffer("\u0915");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => IndicShaper.Prepare(
                null!,
                UnicodeData,
                IndicShapingScript.Devanagari,
                IndicShapingModel.Version2,
                1));
            Assert.Throws<ArgumentNullException>(() => IndicShaper.Prepare(
                buffer,
                null!,
                IndicShapingScript.Devanagari,
                IndicShapingModel.Version2,
                1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                IndicShaper.Prepare(
                    buffer,
                    UnicodeData,
                    (IndicShapingScript)9,
                    IndicShapingModel.Version2,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                IndicShaper.Prepare(
                    buffer,
                    UnicodeData,
                    IndicShapingScript.Devanagari,
                    (IndicShapingModel)2,
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                IndicShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                IndicShaper.BuildPlan(preparation, null!));
            Assert.Throws<ArgumentException>(() => IndicShaper.BuildPlan(
                preparation,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentNullException>(() =>
                IndicShaper.SelectModel(
                    null!,
                    IndicShapingScript.Devanagari));
            Assert.Throws<InvalidOperationException>(() =>
                preparation.ApplyFinalReordering(buffer));
            Assert.Throws<ArgumentException>(() =>
                preparation.ApplyInitialReordering(other));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetBaseSourceIndex(-1));
        });

        preparation.ApplyInitialReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyInitialReordering(buffer));
        preparation.ApplyFinalReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyFinalReordering(buffer));
    }

    static IndicShaperPreparation Prepare(
        ShapingBuffer buffer,
        IndicShapingScript script,
        IndicShapingModel model)
    {
        return IndicShaper.Prepare(
            buffer,
            UnicodeData,
            script,
            model,
            buffer.GetCount() + 1);
    }

    static IndicShaperPlan BuildPlan(
        string text,
        IndicShapingScript script,
        IndicShapingModel model,
        SfntUserFeature[] features)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        IndicShaperPreparation preparation = Prepare(
            buffer,
            script,
            model);
        return IndicShaper.BuildPlan(preparation, features);
    }

    static ShapingBuffer CreateBuffer(string text)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        return new ShapingBuffer(input, ShapingDirection.LeftToRight);
    }

    static SfntLayoutTable CreateLayoutTable(int[] scriptTags)
    {
        SfntLayoutScript[] scripts =
            new SfntLayoutScript[scriptTags.Length];
        int index = 0;
        while (index < scripts.Length)
        {
            scripts[index] = new SfntLayoutScript(
                scriptTags[index],
                null,
                new SfntLayoutLanguageSystem[0]);
            index++;
        }

        return new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(scripts),
            null!,
            null!,
            null!);
    }

    static SfntLayoutFeaturePlan Plan(
        IndicShaperPlan plan,
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
        IndicShaperPlan plan,
        int index,
        IndicShaperStageKind kind,
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
        IndicShaperPreparation preparation,
        bool initial,
        int[] expected)
    {
        int index = 0;
        while (index < expected.Length)
        {
            int actual = initial ?
                preparation.GetInitialSourceIndex(index) :
                preparation.GetFinalSourceIndex(index);
            Assert.That(actual, Is.EqualTo(expected[index]),
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
                "feature value at " + index);
            index++;
        }
    }

    static string ScriptSample(IndicShapingScript script)
    {
        int codePoint = 0x0915 + (int)script * 0x80;
        return char.ConvertFromUtf32(codePoint);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
