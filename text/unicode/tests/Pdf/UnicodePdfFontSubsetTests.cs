using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfFontSubsetTests
{
    [Test]
    public void CollectScalars_ReturnsUniqueSortedScalars()
    {
        int[] scalars = new UnicodePdfFontSubset().CollectScalars("BAA\U0001F600\uD800");

        Assert.That(scalars, Is.EqualTo(new int[]
        {
            0x0041,
            0x0042,
            UnicodeScalar.ReplacementCharacter,
            0x1F600,
        }));
    }

    [Test]
    public void CollectScripts_ReturnsUniqueLayoutScriptsInEncounterOrder()
    {
        UnicodeScript[] scripts = new UnicodePdfFontSubset().CollectScripts("A0 \u0370");

        Assert.That(scripts, Is.EqualTo(new UnicodeScript[]
        {
            UnicodeScript.Latin,
            UnicodeScript.Greek,
        }));
    }

    [Test]
    public void CollectScriptScalarSets_BucketsScalarsByResolvedLayoutScript()
    {
        UnicodePdfScriptScalarSet[] sets = new UnicodePdfFontSubset().CollectScriptScalarSets("A0 \u0370\u0301");

        Assert.That(sets, Has.Length.EqualTo(2));
        Assert.That(sets[0].Script, Is.EqualTo(UnicodeScript.Latin));
        Assert.That(sets[0].Scalars, Is.EqualTo(new int[] { 0x0020, 0x0030, 0x0041 }));
        Assert.That(sets[1].Script, Is.EqualTo(UnicodeScript.Greek));
        Assert.That(sets[1].Scalars, Is.EqualTo(new int[] { 0x0301, 0x0370 }));
    }

    [Test]
    public void CollectScalarsForScript_ReturnsDefensiveScalarCopy()
    {
        int[] latinScalars = new UnicodePdfFontSubset().CollectScalarsForScript("A0 \u0370", UnicodeScript.Latin);
        latinScalars[0] = 0x005A;

        Assert.That(
            new UnicodePdfFontSubset().CollectScalarsForScript("A0 \u0370", UnicodeScript.Latin),
            Is.EqualTo(new int[] { 0x0020, 0x0030, 0x0041 }));
        Assert.That(new UnicodePdfFontSubset().CollectScalarsForScript("A0 \u0370", UnicodeScript.Cyrillic), Is.Empty);
    }

    [Test]
    public void ScriptScalarSet_StoresScriptAndDefensiveScalarCopy()
    {
        int[] scalars = new int[] { 0x0041, 0x0042 };

        UnicodePdfScriptScalarSet set = new UnicodePdfScriptScalarSet(UnicodeScript.Latin, scalars);
        scalars[0] = 0x005A;
        int[] copy = set.GetScalars();
        copy[1] = 0x0059;

        Assert.That(set.Script, Is.EqualTo(UnicodeScript.Latin));
        Assert.That(set.Count(), Is.EqualTo(2));
        Assert.That(set.ContainsScalar(0x0041), Is.True);
        Assert.That(set.ContainsScalar(0x005A), Is.False);
        Assert.That(set.Scalars, Is.EqualTo(new int[] { 0x0041, 0x0042 }));
    }

    [Test]
    public void ScalarAndScriptSetHelpers_AddAndFindValues()
    {
        int[] scalars = UnicodePdfFontSubset.AddScalar(new int[] { 0x0041, 0x0043 }, 0x0042);
        UnicodeScript[] scripts = UnicodePdfFontSubset.AddScript(new UnicodeScript[] { UnicodeScript.Latin }, UnicodeScript.Greek);

        Assert.That(scalars, Is.EqualTo(new int[] { 0x0041, 0x0042, 0x0043 }));
        Assert.That(UnicodePdfFontSubset.AddScalar(scalars, 0x0042), Is.EqualTo(scalars));
        Assert.That(UnicodePdfFontSubset.ContainsScalar(scalars, 0x0043), Is.True);
        Assert.That(UnicodePdfFontSubset.ContainsScalar(scalars, -1), Is.False);
        Assert.That(scripts, Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin, UnicodeScript.Greek }));
        Assert.That(UnicodePdfFontSubset.AddScript(scripts, UnicodeScript.Latin), Is.EqualTo(scripts));
        Assert.That(UnicodePdfFontSubset.ContainsScript(scripts, UnicodeScript.Greek), Is.True);
    }

    [Test]
    public void FontSubsetHelpers_ReturnEmptyCollectionsForEmptyText()
    {
        Assert.That(new UnicodePdfFontSubset().CollectScalars(""), Is.Empty);
        Assert.That(new UnicodePdfFontSubset().CollectScripts(""), Is.Empty);
        Assert.That(new UnicodePdfFontSubset().CollectScriptScalarSets(""), Is.Empty);
    }

    [Test]
    public void FontSubsetHelpers_RejectInvalidArguments()
    {
        System.Collections.Generic.List<UnicodePdfScriptScalarSet> sets = new System.Collections.Generic.List<UnicodePdfScriptScalarSet>();
        sets.Add(null!);

        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubset().CollectScalars(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubset().CollectScripts(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubset().CollectScriptScalarSets(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfFontSubset().CollectScalarsForScript(null!, UnicodeScript.Latin));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.AddScalar(null!, 0x0041));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfFontSubset.AddScalar(new int[] { }, -1));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.AddScript(null!, UnicodeScript.Latin));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.ContainsScalar(null!, 0x0041));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.ContainsScript(null!, UnicodeScript.Latin));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.FindScriptScalarSetIndex(null!, UnicodeScript.Latin));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.FindScriptScalarSetIndex(sets, UnicodeScript.Latin));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.ValidateScalars(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfFontSubset.ValidateScalars(new int[] { 0xD800 }));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.CopyScalars(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfFontSubset.CopyScripts(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfScriptScalarSet(UnicodeScript.Latin, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfScriptScalarSet(UnicodeScript.Latin, new int[] { -1 }));
    }
}
