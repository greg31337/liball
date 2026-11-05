namespace ttf.Tests;

internal class UniversalShaperTests
{
    static UnicodeData UnicodeData = UnicodeData.LoadDefault();

    [Test]
    public void MapsPinnedUnicodePropertiesToUseCategories()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Category(0x1B13),
                Is.EqualTo(UniversalCategory.Base));
            Assert.That(Category(0x1B44),
                Is.EqualTo(UniversalCategory.Halant));
            Assert.That(Category(0xA9BA),
                Is.EqualTo(UniversalCategory.VowelPre));
            Assert.That(Category(0x11052),
                Is.EqualTo(UniversalCategory.BaseNumber));
            Assert.That(Category(0x1107F),
                Is.EqualTo(UniversalCategory.HalantNumber));
            Assert.That(Category(0x11A3A),
                Is.EqualTo(UniversalCategory.ConsonantWithStacker));
            Assert.That(Category(0x13000),
                Is.EqualTo(UniversalCategory.Hieroglyph));
            Assert.That(Category(0x13430),
                Is.EqualTo(UniversalCategory.HieroglyphJoiner));
        });
    }

    [Test]
    public void RecognizesStandardBrokenNumericAndHieroglyphClusters()
    {
        UniversalSyllableMap standard = Recognize("\uA98F\uA9BA");
        UniversalSyllableMap broken = Recognize("\uA9BA");
        UniversalSyllableMap number = Recognize("\U00011052\U0001107F");
        UniversalSyllableMap numeral = Recognize(
            "\U00011052\U0001107F\U00011053");
        UniversalSyllableMap hieroglyph = Recognize(
            "\U00013000\U00013430\U00013001");

        Assert.Multiple(() =>
        {
            AssertSyllable(standard, 0, 0, 2,
                UniversalSyllableType.Standard);
            AssertSyllable(broken, 0, 0, 1,
                UniversalSyllableType.Broken);
            Assert.That(broken.HasBrokenSyllable(), Is.True);
            AssertSyllable(number, 0, 0, 2,
                UniversalSyllableType.NumberJoinerTerminated);
            AssertSyllable(numeral, 0, 0, 3,
                UniversalSyllableType.Numeral);
            AssertSyllable(hieroglyph, 0, 0, 3,
                UniversalSyllableType.Hieroglyph);
        });
    }

    [Test]
    public void KeepsTransparentJoinersInsideTheSurroundingCluster()
    {
        UniversalSyllableMap map = Recognize("\uA98F\u200D\uA9BA");

        Assert.Multiple(() =>
        {
            AssertSyllable(map, 0, 0, 3, UniversalSyllableType.Standard);
            Assert.That(map.GetCategory(1),
                Is.EqualTo(UniversalCategory.CombiningJoiner));
            Assert.That(map.GetRuneSyllableIndex(2), Is.Zero);
        });
    }

    [Test]
    public void ReordersPrebaseVowelAndPreservesClusterSafety()
    {
        ShapingBuffer buffer = CreateBuffer("\uA98F\uA9BA");
        UniversalShaperPreparation preparation = UniversalShaper.Prepare(
            buffer,
            UnicodeData,
            10);

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0xA9BA, 0xA98F });
            Assert.That(preparation.GetSourceIndex(0), Is.EqualTo(1));
            Assert.That(preparation.GetSourceIndex(1), Is.Zero);
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(1), Is.EqualTo(2));
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
            Assert.That(preparation.IsReorderingApplied(), Is.True);
        });
    }

    [Test]
    public void AssignsTopographicalFormsAcrossAdjacentClusters()
    {
        UniversalShaperPreparation two = UniversalShaper.Prepare(
            CreateBuffer("\uA98F\uA990"),
            UnicodeData,
            10);
        UniversalShaperPreparation separated = UniversalShaper.Prepare(
            CreateBuffer("\uA98F\U00013000\uA990"),
            UnicodeData,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(two.GetJoiningForm(0),
                Is.EqualTo(UniversalJoiningForm.Initial));
            Assert.That(two.GetJoiningForm(1),
                Is.EqualTo(UniversalJoiningForm.Final));
            Assert.That(separated.GetJoiningForm(0),
                Is.EqualTo(UniversalJoiningForm.Isolated));
            Assert.That(separated.GetJoiningForm(1),
                Is.EqualTo(UniversalJoiningForm.None));
            Assert.That(separated.GetJoiningForm(2),
                Is.EqualTo(UniversalJoiningForm.Isolated));
        });
    }

    [Test]
    public void BuildsUseFeatureStagesAndSelectiveMasks()
    {
        UniversalShaperPreparation preparation = UniversalShaper.Prepare(
            CreateBuffer("\uA98F\uA990"),
            UnicodeData,
            10);
        int scriptTag = Tag("java");
        UniversalShaperPlan plan = UniversalShaper.BuildPlan(
            preparation,
            scriptTag,
            new SfntUserFeature[]
            {
                new SfntUserFeature(Tag("salt"), 2, 1, 2)
            });

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetScriptTag(), Is.EqualTo(scriptTag));
            Assert.That(plan.GetStageCount(), Is.EqualTo(15));
            Assert.That(plan.GetStage(0).IsReorderingBarrier(), Is.True);
            Assert.That(plan.GetStage(6).IsReorderingBarrier(), Is.True);
            Assert.That(plan.GetStage(8).IsReorderingBarrier(), Is.True);
            Assert.That(Plan(plan, 1).FindEntry(Tag("locl")), Is.Not.Null);
            Assert.That(Plan(plan, 7).FindEntry(Tag("rkrf")), Is.Not.Null);
            Assert.That(Plan(plan, 7).FindEntry(Tag("cjct")), Is.Not.Null);
            AssertFeature(Plan(plan, 9), Tag("init"),
                new int[] { 1, 0 });
            AssertFeature(Plan(plan, 9), Tag("fina"),
                new int[] { 0, 1 });
            AssertFeature(Plan(plan, 11), Tag("salt"),
                new int[] { 0, 2 });
            AssertFeature(Plan(plan, 14), Tag("salt"),
                new int[] { 0, 2 });
            Assert.That(Plan(plan, 13).FindEntry(Tag("mark")), Is.Not.Null);
            Assert.That(Plan(plan, 13).FindEntry(Tag("mkmk")), Is.Not.Null);
            AssertFeature(Plan(plan, 12), Tag("kern"),
                new int[] { 1, 1 });
        });
    }

    [Test]
    public void ValidatesLimitsArgumentsAndPreparationState()
    {
        ShapingBuffer buffer = CreateBuffer("\uA98F");
        UniversalShaperPreparation preparation = UniversalShaper.Prepare(
            buffer,
            UnicodeData,
            1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                UniversalSyllableRecognizer.Recognize(null!, UnicodeData, 1));
            Assert.Throws<ArgumentNullException>(() =>
                UniversalSyllableRecognizer.Recognize(buffer, null!, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UniversalSyllableRecognizer.Recognize(
                    buffer,
                    UnicodeData,
                    -1));
            Assert.Throws<FontLimitException>(() =>
                UniversalSyllableRecognizer.Recognize(
                    buffer,
                    UnicodeData,
                    0));
            Assert.Throws<ArgumentNullException>(() =>
                UniversalShaper.BuildPlan(null!, Tag("java"),
                    new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                UniversalShaper.BuildPlan(preparation, Tag("java"), null!));
            Assert.Throws<ArgumentException>(() =>
                UniversalShaper.BuildPlan(preparation, Tag("java"),
                    new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetCategory(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UniversalCategoryData.GetCategory(UnicodeData, 0x110000));
        });

        preparation.ApplyReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyReordering(buffer));
        Assert.Throws<InvalidOperationException>(() =>
            preparation.RecordPrebaseSubstitution(0));
    }

    static UniversalCategory Category(int codePoint)
    {
        return UniversalCategoryData.GetCategory(UnicodeData, codePoint);
    }

    static UniversalSyllableMap Recognize(string text)
    {
        return UniversalSyllableRecognizer.Recognize(
            CreateBuffer(text),
            UnicodeData,
            20);
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
        UniversalShaperPlan plan,
        int stageIndex)
    {
        return plan.GetStage(stageIndex).GetFeaturePlan();
    }

    static void AssertSyllable(
        UniversalSyllableMap map,
        int index,
        int start,
        int length,
        UniversalSyllableType type)
    {
        Assert.That(map.GetSyllableStart(index), Is.EqualTo(start));
        Assert.That(map.GetSyllableLength(index), Is.EqualTo(length));
        Assert.That(map.GetSyllableType(index), Is.EqualTo(type));
    }

    static void AssertRunes(ShapingBuffer buffer, int[] expected)
    {
        Assert.That(buffer.GetCount(), Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(buffer.GetScalarValue(index),
                Is.EqualTo(expected[index]));
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
            Assert.That(entry!.GetValueAt(index), Is.EqualTo(expected[index]));
            index++;
        }
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
