namespace ttf.Tests;

internal class ThaiLaoShaperTests
{
    static int CharacterComposition = Tag("ccmp");
    static int ContextualLigatures = Tag("clig");
    static int Kerning = Tag("kern");
    static int MarkPositioning = Tag("mark");
    static int MarkToMarkPositioning = Tag("mkmk");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void DecomposesAndReordersThaiSaraAmAcrossAboveMarks()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E14\u0E4B\u0E33");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            false,
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetDecompositionCount(), Is.EqualTo(1));
            Assert.That(buffer.GetCount(), Is.EqualTo(4));
            AssertSources(preparation, new int[] { 0, 2, 1, 3 });
            Assert.That(buffer.GetClusterStart(2), Is.EqualTo(2));
            Assert.That(buffer.GetClusterStart(3), Is.EqualTo(2));
        });

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[]
            {
                0x0E14,
                0x0E4D,
                0x0E4B,
                0x0E32
            });
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(3));
            Assert.That(buffer.IsUnsafeToBreak(3), Is.True);
        });
    }

    [Test]
    public void DecomposesAndReordersLaoSaraAmAcrossAboveMarks()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E94\u0ECB\u0EB3");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Lao,
            ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
            false,
            new int[] { 0xF705 });

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetDecompositionCount(), Is.EqualTo(1));
            Assert.That(preparation.WasLegacyFallbackAttempted(), Is.False);
            AssertSources(preparation, new int[] { 0, 2, 1, 3 });
        });

        preparation.ApplyReordering(buffer);
        AssertRunes(buffer, new int[]
        {
            0x0E94,
            0x0ECD,
            0x0ECB,
            0x0EB2
        });
    }

    [Test]
    public void DoesNotReorderAnExplicitNikhahit()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E14\u0E4B\u0E4D");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            false,
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.GetDecompositionCount(), Is.Zero);
            AssertSources(preparation, new int[] { 0, 1, 2 });
        });

        preparation.ApplyReordering(buffer);
        AssertRunes(buffer, new int[] { 0x0E14, 0x0E4B, 0x0E4D });
    }

    [Test]
    public void PlacesThaiBelowVowelsBeforePhinthu()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E01\u0E3A\u0E38\u0E39");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            false,
            new int[0]);

        AssertSources(preparation, new int[] { 0, 2, 3, 1 });
        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[]
            {
                0x0E01,
                0x0E38,
                0x0E39,
                0x0E3A
            });
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(4));
        });
    }

    [Test]
    public void UsesWindowsPuaBeforeMacPuaForLegacyThaiFallback()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E1B\u0E48");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
            false,
            new int[] { 0xF705, 0xF88C });

        Assert.Multiple(() =>
        {
            Assert.That(preparation.WasLegacyFallbackAttempted(), Is.True);
            Assert.That(preparation.GetLegacyPuaReplacementCount(),
                Is.EqualTo(1));
        });

        preparation.ApplyReordering(buffer);
        Assert.Multiple(() =>
        {
            AssertRunes(buffer, new int[] { 0x0E1B, 0xF705 });
            Assert.That(buffer.IsUnsafeToBreak(0), Is.True);
            Assert.That(buffer.IsUnsafeToBreak(1), Is.True);
        });
    }

    [Test]
    public void UsesMacPuaWhenTheWindowsAlternativeIsUnavailable()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E1B\u0E48");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
            false,
            new int[] { 0xF88C });

        preparation.ApplyReordering(buffer);

        AssertRunes(buffer, new int[] { 0x0E1B, 0xF88C });
    }

    [TestCase("\u0E01\u0E48", 1, 0xF70A)]
    [TestCase("\u0E0E\u0E38", 1, 0xF718)]
    [TestCase("\u0E0D\u0E38", 0, 0xF70F)]
    [TestCase("\u0E1B\u0E34\u0E48", 1, 0xF701)]
    [TestCase("\u0E1B\u0E34\u0E48", 2, 0xF713)]
    public void AppliesLegacyThaiStateMachineMappings(
        string text,
        int replacementIndex,
        int expectedReplacement)
    {
        ShapingBuffer buffer = CreateBuffer(text);
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
            false,
            new int[] { 0xF70A, 0xF718, 0xF70F, 0xF701, 0xF713 });

        preparation.ApplyReordering(buffer);

        Assert.That(buffer.GetScalarValue(replacementIndex),
            Is.EqualTo(expectedReplacement));
    }

    [Test]
    public void LeavesTextUnchangedWhenLegacyGlyphsAreUnavailable()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E1B\u0E48");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
            false,
            new int[0]);

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.WasLegacyFallbackAttempted(), Is.True);
            Assert.That(preparation.GetLegacyPuaReplacementCount(), Is.Zero);
            AssertRunes(buffer, new int[] { 0x0E1B, 0x0E48 });
        });
    }

    [TestCase(ThaiLegacyPuaPolicy.Disabled, false)]
    [TestCase(
        ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable,
        true)]
    public void SkipsLegacyFallbackWhenDisabledOrGsubIsAvailable(
        ThaiLegacyPuaPolicy policy,
        bool hasScriptSubstitution)
    {
        ShapingBuffer buffer = CreateBuffer("\u0E1B\u0E48");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            policy,
            hasScriptSubstitution,
            new int[] { 0xF705 });

        preparation.ApplyReordering(buffer);

        Assert.Multiple(() =>
        {
            Assert.That(preparation.WasLegacyFallbackAttempted(), Is.False);
            Assert.That(preparation.GetLegacyPuaReplacementCount(), Is.Zero);
            AssertRunes(buffer, new int[] { 0x0E1B, 0x0E48 });
        });
    }

    [TestCase(ThaiLaoScript.Thai, "thai")]
    [TestCase(ThaiLaoScript.Lao, "lao ")]
    public void BuildsThaiAndLaoFeatureStages(
        ThaiLaoScript script,
        string expectedScriptTag)
    {
        ShapingBuffer buffer = CreateBuffer(
            script == ThaiLaoScript.Thai ? "\u0E01" : "\u0E81");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            script,
            ThaiLegacyPuaPolicy.Disabled,
            true,
            new int[0]);
        ThaiLaoShaperPlan plan = ThaiLaoShaper.BuildPlan(
            preparation,
            new SfntUserFeature[0]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetScript(), Is.EqualTo(script));
            Assert.That(plan.GetScriptTag(),
                Is.EqualTo(Tag(expectedScriptTag)));
            Assert.That(plan.GetStageCount(), Is.EqualTo(7));
            AssertStage(plan, 0, ThaiLaoShaperStageKind.LocalizedForms,
                DefaultShaperTableKind.Substitution);
            AssertStage(
                plan,
                1,
                ThaiLaoShaperStageKind.CompositionAndDecomposition,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 2, ThaiLaoShaperStageKind.Ligatures,
                DefaultShaperTableKind.Substitution);
            AssertStage(plan, 4, ThaiLaoShaperStageKind.Kerning,
                DefaultShaperTableKind.Positioning);
            AssertStage(plan, 5, ThaiLaoShaperStageKind.MarkPositioning,
                DefaultShaperTableKind.Positioning);
            Assert.That(Plan(plan, 1).FindEntry(CharacterComposition),
                Is.Not.Null);
            Assert.That(Plan(plan, 5).FindEntry(MarkPositioning),
                Is.Not.Null);
            Assert.That(Plan(plan, 5).FindEntry(MarkToMarkPositioning),
                Is.Not.Null);
        });
    }

    [Test]
    public void AppliesFeaturePoliciesAndRoutesCustomFeatures()
    {
        ShapingBuffer buffer = CreateBuffer("\u0E01\u0E02\u0E03");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            true,
            new int[0]);
        ThaiLaoShaperPlan plan = ThaiLaoShaper.BuildPlan(
            preparation,
            new SfntUserFeature[]
            {
                Feature(CharacterComposition, 0, 0, 3),
                Feature(ContextualLigatures, 0, 1, 3),
                Feature(MarkPositioning, 0, 0, 3),
                Feature(StylisticAlternates, 2, 1, 3)
            });

        Assert.Multiple(() =>
        {
            AssertFeature(Plan(plan, 1), CharacterComposition,
                new int[] { 1, 1, 1 });
            AssertFeature(Plan(plan, 2), ContextualLigatures,
                new int[] { 1, 0, 0 });
            AssertFeature(Plan(plan, 5), MarkPositioning,
                new int[] { 1, 1, 1 });
            AssertFeature(Plan(plan, 3), StylisticAlternates,
                new int[] { 0, 2, 2 });
            AssertFeature(Plan(plan, 6), StylisticAlternates,
                new int[] { 0, 2, 2 });
        });
    }

    [Test]
    public void SupportsEmptyInputAndValidatesArgumentsAndState()
    {
        ThaiLegacyPuaCoverage emptyCoverage =
            new ThaiLegacyPuaCoverage(new int[0]);
        ShapingBuffer emptyBuffer = CreateBuffer("");
        ThaiLaoShaperPreparation empty = ThaiLaoShaper.Prepare(
            emptyBuffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            false,
            emptyCoverage);
        ThaiLaoShaperPlan emptyPlan = ThaiLaoShaper.BuildPlan(
            empty,
            new SfntUserFeature[0]);
        ShapingBuffer buffer = CreateBuffer("\u0E01");
        ThaiLaoShaperPreparation preparation = Prepare(
            buffer,
            ThaiLaoScript.Thai,
            ThaiLegacyPuaPolicy.Disabled,
            false,
            new int[0]);
        ShapingBuffer other = CreateBuffer("\u0E01");

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetCount(), Is.Zero);
            Assert.That(emptyPlan.GetStageCount(), Is.EqualTo(7));
            Assert.Throws<ArgumentNullException>(() => ThaiLaoShaper.Prepare(
                null!,
                ThaiLaoScript.Thai,
                ThaiLegacyPuaPolicy.Disabled,
                false,
                emptyCoverage));
            Assert.Throws<ArgumentNullException>(() => ThaiLaoShaper.Prepare(
                buffer,
                ThaiLaoScript.Thai,
                ThaiLegacyPuaPolicy.Disabled,
                false,
                null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThaiLaoShaper.Prepare(
                    buffer,
                    (ThaiLaoScript)2,
                    ThaiLegacyPuaPolicy.Disabled,
                    false,
                    emptyCoverage));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThaiLaoShaper.Prepare(
                    buffer,
                    ThaiLaoScript.Thai,
                    (ThaiLegacyPuaPolicy)2,
                    false,
                    emptyCoverage));
            Assert.Throws<ArgumentNullException>(() =>
                ThaiLaoShaper.BuildPlan(null!, new SfntUserFeature[0]));
            Assert.Throws<ArgumentNullException>(() =>
                ThaiLaoShaper.BuildPlan(preparation, null!));
            Assert.Throws<ArgumentException>(() => ThaiLaoShaper.BuildPlan(
                preparation,
                new SfntUserFeature[] { null! }));
            Assert.Throws<ArgumentException>(() =>
                preparation.ApplyReordering(other));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                preparation.GetSourceIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.GetStage(7));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                emptyPlan.FindStage((ThaiLaoShaperStageKind)7));
            Assert.Throws<ArgumentNullException>(() =>
                new ThaiLegacyPuaCoverage(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThaiLegacyPuaCoverage(new int[] { 0x110000 }));
        });

        empty.ApplyReordering(emptyBuffer);
        preparation.ApplyReordering(buffer);
        Assert.Throws<InvalidOperationException>(() =>
            preparation.ApplyReordering(buffer));
    }

    static ThaiLaoShaperPreparation Prepare(
        ShapingBuffer buffer,
        ThaiLaoScript script,
        ThaiLegacyPuaPolicy policy,
        bool hasScriptSubstitution,
        int[] puaCodePoints)
    {
        return ThaiLaoShaper.Prepare(
            buffer,
            script,
            policy,
            hasScriptSubstitution,
            new ThaiLegacyPuaCoverage(puaCodePoints));
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
        ThaiLaoShaperPlan plan,
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
        ThaiLaoShaperPlan plan,
        int index,
        ThaiLaoShaperStageKind kind,
        DefaultShaperTableKind tableKind)
    {
        Assert.That(plan.GetStage(index).GetKind(), Is.EqualTo(kind));
        Assert.That(plan.GetStage(index).GetTableKind(),
            Is.EqualTo(tableKind));
    }

    static void AssertSources(
        ThaiLaoShaperPreparation preparation,
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
