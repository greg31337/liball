using ttf;

namespace ttf.Tests;

internal class SfntLayoutSelectionTests
{
    static int DefaultScript = Tag("DFLT");
    static int English = Tag("ENG ");
    static int Kern = Tag("kern");
    static int Latin = Tag("latn");
    static int Ligatures = Tag("liga");
    static int RequiredLigatures = Tag("rlig");
    static int StylisticAlternates = Tag("salt");

    [Test]
    public void SelectsExactScriptLanguageAndReportsAvailableFeatures()
    {
        SfntLayoutTable table = CreateTable(true);
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            table,
            Latin,
            English,
            new SfntUserFeature[]
            {
                Feature(Ligatures, 1, 0, 10),
                Feature(StylisticAlternates, 2, 0, 10),
                Feature(Kern, 1, 0, 10)
            },
            10);

        Assert.Multiple(() =>
        {
            Assert.That(selection.HasScript(), Is.True);
            Assert.That(selection.GetScript()!.GetScriptTag(),
                Is.EqualTo(Latin));
            Assert.That(selection.GetLanguageSystem()!.GetLanguageTag(),
                Is.EqualTo(English));
            Assert.That(selection.UsedDefaultScript(), Is.False);
            Assert.That(selection.UsedDefaultLanguageSystem(), Is.False);
            Assert.That(selection.HasFeature(Ligatures), Is.True);
            Assert.That(selection.HasFeature(RequiredLigatures), Is.False);
            Assert.That(selection.HasFeature(StylisticAlternates), Is.False);
            Assert.That(selection.HasFeature(Kern), Is.False);
            Assert.That(selection.GetLookupCount(), Is.EqualTo(2));
            Assert.That(selection.GetLookup(0).GetLookupIndex(),
                Is.EqualTo(1));
            Assert.That(selection.GetLookup(1).GetLookupIndex(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void FallsBackToDefaultScriptAndDefaultLanguageSystem()
    {
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            CreateTable(true),
            Tag("cyrl"),
            English,
            new SfntUserFeature[]
            {
                Feature(Kern, 1, 0, 5)
            },
            5);

        Assert.Multiple(() =>
        {
            Assert.That(selection.GetScript()!.GetScriptTag(),
                Is.EqualTo(DefaultScript));
            Assert.That(selection.GetLanguageSystem()!.GetLanguageTag(),
                Is.Zero);
            Assert.That(selection.UsedDefaultScript(), Is.True);
            Assert.That(selection.UsedDefaultLanguageSystem(), Is.True);
            Assert.That(selection.HasFeature(Kern), Is.True);
            Assert.That(selection.GetLookupCount(), Is.EqualTo(1));
            Assert.That(selection.GetLookup(0).GetLookupIndex(), Is.Zero);
        });
    }

    [Test]
    public void ReturnsEmptySelectionWhenNoScriptFallbackExists()
    {
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            CreateTable(false),
            Tag("cyrl"),
            0,
            new SfntUserFeature[]
            {
                Feature(Kern, 1, 0, 5)
            },
            5);

        Assert.Multiple(() =>
        {
            Assert.That(selection.HasScript(), Is.False);
            Assert.That(selection.HasLanguageSystem(), Is.False);
            Assert.That(selection.GetLookupCount(), Is.Zero);
            Assert.That(selection.HasFeature(Kern), Is.False);
        });
    }

    [Test]
    public void AppliesRequiredLookupsOnceAndCannotSuppressThem()
    {
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            CreateTable(true),
            Latin,
            0,
            new SfntUserFeature[]
            {
                Feature(RequiredLigatures, 0, 0, 10),
                Feature(Ligatures, 1, 0, 10)
            },
            10);
        SfntLayoutLookupSelection? shared = selection.FindLookup(1);

        Assert.Multiple(() =>
        {
            Assert.That(selection.GetLookupCount(), Is.EqualTo(3));
            Assert.That(selection.GetLookup(0).GetLookupIndex(),
                Is.EqualTo(1));
            Assert.That(selection.GetLookup(1).GetLookupIndex(),
                Is.EqualTo(2));
            Assert.That(selection.GetLookup(2).GetLookupIndex(),
                Is.EqualTo(3));
            Assert.That(shared!.GetRangeCount(), Is.EqualTo(1));
            Assert.That(shared.GetValueAt(0), Is.EqualTo(1));
            Assert.That(shared.GetValueAt(9), Is.EqualTo(1));
        });
    }

    [Test]
    public void CombinesSharedLookupRangesWithLastSettingPrecedence()
    {
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            CreateTable(true),
            Latin,
            0,
            new SfntUserFeature[]
            {
                Feature(Ligatures, 1, 0, 10),
                Feature(StylisticAlternates, 2, 2, 10),
                Feature(StylisticAlternates, 3, 4, 8),
                Feature(Ligatures, 0, 7, 9)
            },
            10);
        SfntLayoutLookupSelection? shared = selection.FindLookup(2);

        Assert.Multiple(() =>
        {
            Assert.That(shared!.GetValueAt(0), Is.EqualTo(1));
            Assert.That(shared.GetValueAt(2), Is.EqualTo(2));
            Assert.That(shared.GetValueAt(4), Is.EqualTo(3));
            Assert.That(shared.GetValueAt(7), Is.EqualTo(3));
            Assert.That(shared.GetValueAt(8), Is.EqualTo(2));
            Assert.That(shared.GetValueAt(9), Is.EqualTo(2));
            Assert.That(shared.GetRangeCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void SupportsDisabledGapsAndClipsRangesToTextLength()
    {
        SfntLayoutSelection selection = SfntLayoutSelector.Select(
            CreateTable(true),
            Latin,
            0,
            new SfntUserFeature[]
            {
                Feature(StylisticAlternates, 1, 0, 100),
                Feature(StylisticAlternates, 4, 3, 7),
                Feature(StylisticAlternates, 0, 5, 6)
            },
            10);
        SfntLayoutLookupSelection? alternate = selection.FindLookup(4);

        Assert.Multiple(() =>
        {
            Assert.That(alternate!.GetRangeCount(), Is.EqualTo(4));
            Assert.That(alternate.GetValueAt(2), Is.EqualTo(1));
            Assert.That(alternate.GetValueAt(3), Is.EqualTo(4));
            Assert.That(alternate.GetValueAt(5), Is.Zero);
            Assert.That(alternate.GetValueAt(6), Is.EqualTo(4));
            Assert.That(alternate.GetValueAt(9), Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidatesSettingsSelectionArgumentsAndLookupOffsets()
    {
        SfntLayoutTable table = CreateTable(true);
        SfntLayoutSelection empty = SfntLayoutSelector.Select(
            table,
            Latin,
            0,
            new SfntUserFeature[0],
            0);

        Assert.Multiple(() =>
        {
            Assert.That(empty.GetLookupCount(), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntUserFeature(Ligatures, -1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SfntUserFeature(Ligatures, 1, 2, 1));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutSelector.Select(
                    null!, Latin, 0, new SfntUserFeature[0], 1));
            Assert.Throws<ArgumentNullException>(() =>
                SfntLayoutSelector.Select(table, Latin, 0, null!, 1));
            Assert.Throws<ArgumentException>(() =>
                SfntLayoutSelector.Select(
                    table,
                    Latin,
                    0,
                    new SfntUserFeature[] { null! },
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutSelector.Select(
                    table, Latin, 0, new SfntUserFeature[0], -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntLayoutSelector.Select(
                    table,
                    Latin,
                    0,
                    new SfntUserFeature[]
                    {
                        Feature(Ligatures, 1, 0, 1)
                    },
                    1).GetLookup(0).GetValueAt(1));
        });
    }

    static SfntUserFeature Feature(
        int tag,
        int value,
        int start,
        int end)
    {
        return new SfntUserFeature(tag, value, start, end);
    }

    static SfntLayoutTable CreateTable(bool includeDefaultScript)
    {
        SfntLayoutFeature[] features = new SfntLayoutFeature[]
        {
            CreateFeature(Kern, new int[] { 0 }),
            CreateFeature(RequiredLigatures, new int[] { 3, 1 }),
            CreateFeature(Ligatures, new int[] { 2, 1 }),
            CreateFeature(StylisticAlternates, new int[] { 2, 4 })
        };
        SfntLayoutLanguageSystem latinDefault =
            new SfntLayoutLanguageSystem(
                0,
                1,
                new int[] { 2, 3 });
        SfntLayoutLanguageSystem english =
            new SfntLayoutLanguageSystem(
                English,
                0xFFFF,
                new int[] { 2 });
        SfntLayoutScript latin = new SfntLayoutScript(
            Latin,
            latinDefault,
            new SfntLayoutLanguageSystem[] { english });
        SfntLayoutScript[] scripts;

        if (includeDefaultScript)
        {
            SfntLayoutLanguageSystem defaultLanguage =
                new SfntLayoutLanguageSystem(
                    0,
                    0xFFFF,
                    new int[] { 0 });
            SfntLayoutScript defaults = new SfntLayoutScript(
                DefaultScript,
                defaultLanguage,
                new SfntLayoutLanguageSystem[0]);
            scripts = new SfntLayoutScript[] { defaults, latin };
        }
        else
        {
            scripts = new SfntLayoutScript[] { latin };
        }

        SfntLayoutLookup[] lookups = new SfntLayoutLookup[5];
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
            new SfntLayoutScriptList(scripts),
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
