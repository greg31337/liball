using ttf;

namespace ttf.Tests;

internal class SfntLayoutFeaturePlanTests
{
    static int CharacterComposition = Tag("ccmp");
    static int DefaultScript = Tag("DFLT");
    static int DiscretionaryLigatures = Tag("dlig");
    static int Kerning = Tag("kern");
    static int Latin = Tag("latn");
    static int Ligatures = Tag("liga");
    static int LocalizedForms = Tag("locl");
    static int StylisticAlternates = Tag("salt");
    static int StylisticSet = Tag("ss01");

    [Test]
    public void BuildsCategoriesInExplicitOrderAndRemovesDuplicateTags()
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[]
            {
                CharacterComposition,
                LocalizedForms,
                CharacterComposition
            },
            new int[] { Ligatures, Kerning, LocalizedForms },
            new int[]
            {
                DiscretionaryLigatures,
                StylisticAlternates,
                Ligatures
            },
            new SfntUserFeature[]
            {
                Feature(StylisticSet, 1, 0, 8),
                Feature(StylisticAlternates, 2, 0, 8),
                Feature(StylisticSet, 3, 2, 4)
            },
            8);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetEntryCount(), Is.EqualTo(7));
            AssertEntry(plan, 0, CharacterComposition,
                SfntLayoutFeatureCategory.Mandatory);
            AssertEntry(plan, 1, LocalizedForms,
                SfntLayoutFeatureCategory.Mandatory);
            AssertEntry(plan, 2, Ligatures,
                SfntLayoutFeatureCategory.Default);
            AssertEntry(plan, 3, Kerning,
                SfntLayoutFeatureCategory.Default);
            AssertEntry(plan, 4, DiscretionaryLigatures,
                SfntLayoutFeatureCategory.Discretionary);
            AssertEntry(plan, 5, StylisticAlternates,
                SfntLayoutFeatureCategory.Discretionary);
            AssertEntry(plan, 6, StylisticSet,
                SfntLayoutFeatureCategory.Discretionary);
        });
    }

    [Test]
    public void AppliesCategoryDefaultsAndOrderedRangeOverrides()
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[] { CharacterComposition },
            new int[] { Ligatures },
            new int[] { StylisticAlternates },
            new SfntUserFeature[]
            {
                Feature(CharacterComposition, 0, 0, 8),
                Feature(Ligatures, 0, 2, 5),
                Feature(Ligatures, 2, 3, 4),
                Feature(StylisticAlternates, 3, 1, 6),
                Feature(StylisticAlternates, 0, 2, 3)
            },
            8);
        SfntLayoutFeaturePlanEntry? mandatory =
            plan.FindEntry(CharacterComposition);
        SfntLayoutFeaturePlanEntry? defaults = plan.FindEntry(Ligatures);
        SfntLayoutFeaturePlanEntry? discretionary =
            plan.FindEntry(StylisticAlternates);

        Assert.Multiple(() =>
        {
            Assert.That(mandatory!.GetValueAt(0), Is.EqualTo(1));
            Assert.That(mandatory.GetValueAt(7), Is.EqualTo(1));
            Assert.That(defaults!.GetValueAt(1), Is.EqualTo(1));
            Assert.That(defaults.GetValueAt(2), Is.Zero);
            Assert.That(defaults.GetValueAt(3), Is.EqualTo(2));
            Assert.That(defaults.GetValueAt(4), Is.Zero);
            Assert.That(defaults.GetValueAt(5), Is.EqualTo(1));
            Assert.That(discretionary!.GetValueAt(0), Is.Zero);
            Assert.That(discretionary.GetValueAt(1), Is.EqualTo(3));
            Assert.That(discretionary.GetValueAt(2), Is.Zero);
            Assert.That(discretionary.GetValueAt(3), Is.EqualTo(3));
            Assert.That(discretionary.GetValueAt(6), Is.Zero);
        });
    }

    [Test]
    public void ClipsUserRangesAndPreservesFirstSeenUnknownTagOrder()
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[0],
            new int[0],
            new int[] { StylisticAlternates },
            new SfntUserFeature[]
            {
                Feature(StylisticSet, 2, 2, 100),
                Feature(DiscretionaryLigatures, 1, 0, 4),
                Feature(StylisticSet, 4, 3, 5)
            },
            5);

        Assert.Multiple(() =>
        {
            Assert.That(plan.GetEntryCount(), Is.EqualTo(3));
            Assert.That(plan.GetEntry(0).GetFeatureTag(),
                Is.EqualTo(StylisticAlternates));
            Assert.That(plan.GetEntry(1).GetFeatureTag(),
                Is.EqualTo(StylisticSet));
            Assert.That(plan.GetEntry(2).GetFeatureTag(),
                Is.EqualTo(DiscretionaryLigatures));
            Assert.That(plan.GetEntry(1).GetValueAt(1), Is.Zero);
            Assert.That(plan.GetEntry(1).GetValueAt(2), Is.EqualTo(2));
            Assert.That(plan.GetEntry(1).GetValueAt(3), Is.EqualTo(4));
            Assert.That(plan.GetEntry(1).GetValueAt(4), Is.EqualTo(4));
        });
    }

    [Test]
    public void CreatesLookupSelectionFromTheConstructedPlan()
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[] { CharacterComposition },
            new int[] { Ligatures },
            new int[] { StylisticAlternates },
            new SfntUserFeature[]
            {
                Feature(StylisticAlternates, 4, 2, 4),
                Feature(Ligatures, 0, 3, 5)
            },
            6);
        SfntLayoutSelection selection = plan.Select(
            CreateLayoutTable(),
            Latin,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(selection.GetLookupCount(), Is.EqualTo(3));
            Assert.That(selection.GetLookup(0).GetLookupIndex(),
                Is.EqualTo(1));
            Assert.That(selection.GetLookup(1).GetLookupIndex(),
                Is.EqualTo(2));
            Assert.That(selection.GetLookup(2).GetLookupIndex(),
                Is.EqualTo(3));
            Assert.That(selection.FindLookup(1)!.GetValueAt(1),
                Is.EqualTo(1));
            Assert.That(selection.FindLookup(1)!.GetValueAt(2),
                Is.EqualTo(4));
            Assert.That(selection.FindLookup(1)!.GetValueAt(4),
                Is.Zero);
            Assert.That(selection.FindLookup(2)!.GetValueAt(4),
                Is.EqualTo(1));
            Assert.That(selection.FindLookup(3)!.GetValueAt(2),
                Is.EqualTo(4));
            Assert.That(selection.FindLookup(3)!.GetValueAt(4),
                Is.Zero);
        });
    }

    [Test]
    public void ProducesTheSameOrderAcrossRepeatedConstruction()
    {
        int[] mandatory = new int[]
        {
            LocalizedForms,
            CharacterComposition
        };
        int[] defaults = new int[] { Kerning, Ligatures };
        int[] discretionary = new int[]
        {
            StylisticAlternates,
            DiscretionaryLigatures
        };
        SfntLayoutFeaturePlan first = SfntLayoutFeaturePlanBuilder.Build(
            mandatory,
            defaults,
            discretionary,
            new SfntUserFeature[0],
            4);
        SfntLayoutFeaturePlan second = SfntLayoutFeaturePlanBuilder.Build(
            mandatory,
            defaults,
            discretionary,
            new SfntUserFeature[0],
            4);

        Assert.Multiple(() =>
        {
            Assert.That(CopyTags(first), Is.EqualTo(new int[]
            {
                LocalizedForms,
                CharacterComposition,
                Kerning,
                Ligatures,
                StylisticAlternates,
                DiscretionaryLigatures
            }));
            Assert.That(CopyTags(second), Is.EqualTo(CopyTags(first)));
        });
    }

    [Test]
    public void ValidatesPlanInputsAndEmptyText()
    {
        SfntLayoutFeaturePlan empty = SfntLayoutFeaturePlanBuilder.Build(
            new int[] { CharacterComposition },
            new int[] { Ligatures },
            new int[] { StylisticAlternates },
            new SfntUserFeature[0],
            0);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetEntryCount(), Is.EqualTo(3));
            Assert.That(empty.GetSelectionCount(), Is.Zero);
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    null!,
                    new int[0],
                    new int[0],
                    new SfntUserFeature[0],
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    new int[0],
                    null!,
                    new int[0],
                    new SfntUserFeature[0],
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    new int[0],
                    new int[0],
                    null!,
                    new SfntUserFeature[0],
                    1));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    new int[0],
                    new int[0],
                    new int[0],
                    null!,
                    1));
            Assert.Throws<ArgumentException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    new int[0],
                    new int[0],
                    new int[0],
                    new SfntUserFeature[] { null! },
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutFeaturePlanBuilder.Build(
                    new int[0],
                    new int[0],
                    new int[0],
                    new SfntUserFeature[0],
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                empty.GetEntry(0).GetValueAt(0));
        });
    }

    static void AssertEntry(
        SfntLayoutFeaturePlan plan,
        int index,
        int featureTag,
        SfntLayoutFeatureCategory category)
    {
        Assert.That(plan.GetEntry(index).GetFeatureTag(),
            Is.EqualTo(featureTag));
        Assert.That(plan.GetEntry(index).GetCategory(),
            Is.EqualTo(category));
    }

    static int[] CopyTags(SfntLayoutFeaturePlan plan)
    {
        int[] tags = new int[plan.GetEntryCount()];
        int index = 0;

        while (index < tags.Length)
        {
            tags[index] = plan.GetEntry(index).GetFeatureTag();
            index += 1;
        }

        return tags;
    }

    static SfntUserFeature Feature(
        int tag,
        int value,
        int start,
        int end)
    {
        return new SfntUserFeature(tag, value, start, end);
    }

    static SfntLayoutTable CreateLayoutTable()
    {
        SfntLayoutFeature[] features = new SfntLayoutFeature[]
        {
            CreateFeature(CharacterComposition, new int[] { 2 }),
            CreateFeature(Ligatures, new int[] { 1, 2 }),
            CreateFeature(StylisticAlternates, new int[] { 1, 3 })
        };
        SfntLayoutLanguageSystem language = new SfntLayoutLanguageSystem(
            0,
            0xFFFF,
            new int[] { 0, 1, 2 });
        SfntLayoutScript script = new SfntLayoutScript(
            Latin,
            language,
            new SfntLayoutLanguageSystem[0]);
        SfntLayoutLookup[] lookups = new SfntLayoutLookup[4];
        int index = 0;

        while (index < lookups.Length)
        {
            lookups[index] = new SfntLayoutLookup(
                1,
                0,
                new int[0],
                new int[0],
                0);
            index += 1;
        }

        return new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(new SfntLayoutScript[]
            {
                new SfntLayoutScript(
                    DefaultScript,
                    language,
                    new SfntLayoutLanguageSystem[0]),
                script
            }),
            new SfntLayoutFeatureList(features),
            new SfntLayoutLookupList(lookups),
            FontData.Create(new byte[0]).GetSlice(0, 0));
    }

    static SfntLayoutFeature CreateFeature(int tag, int[] lookups)
    {
        return new SfntLayoutFeature(tag, 0, 0, lookups);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
