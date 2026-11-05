using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfScriptRunTests
{
    [Test]
    public void GetScriptRuns_GroupsContiguousStrongScripts()
    {
        string text = "A\u0370\u0600";

        UnicodePdfScriptRun[] runs = new UnicodePdfText().GetScriptRuns(text);

        Assert.That(runs, Has.Length.EqualTo(3));
        AssertRun(runs[0], 0, 1, UnicodeScript.Latin);
        AssertRun(runs[1], 1, 2, UnicodeScript.Greek);
        AssertRun(runs[2], 2, 3, UnicodeScript.Arabic);
    }

    [Test]
    public void GetScriptRuns_AttachesDigitsAndSpacesToNearbyStrongScripts()
    {
        string text = "09A 0\u0370";

        UnicodePdfScriptRun[] runs = new UnicodePdfText().GetScriptRuns(text);

        Assert.That(runs, Has.Length.EqualTo(2));
        AssertRun(runs[0], 0, 5, UnicodeScript.Latin);
        AssertRun(runs[1], 5, 6, UnicodeScript.Greek);
    }

    [Test]
    public void GetScriptRuns_UsesCommonWhenTextHasOnlyNeutralScripts()
    {
        UnicodePdfScriptRun[] runs = new UnicodePdfText().GetScriptRuns("09 ");

        Assert.That(runs, Has.Length.EqualTo(1));
        AssertRun(runs[0], 0, 3, UnicodeScript.Common);
    }

    [Test]
    public void GetScriptRuns_AttachesCombiningMarksToPreviousStrongScript()
    {
        UnicodePdfScriptRun[] runs = new UnicodePdfText().GetScriptRuns("A\u0301B");

        Assert.That(runs, Has.Length.EqualTo(1));
        AssertRun(runs[0], 0, 3, UnicodeScript.Latin);
    }

    [Test]
    public void GetScriptRuns_ResolvesLeadingCommonAndInheritedTextForward()
    {
        UnicodePdfScriptRun[] runs = new UnicodePdfText().GetScriptRuns("(\u0301A) \u0370\u0301");

        Assert.That(runs, Has.Length.EqualTo(2));
        AssertRun(runs[0], 0, 5, UnicodeScript.Latin);
        AssertRun(runs[1], 5, 7, UnicodeScript.Greek);
    }

    [Test]
    public void GetScriptRuns_PreservesUtf16OffsetsForSupplementaryAndInvalidScalars()
    {
        UnicodePdfScriptRun[] supplementaryRuns = new UnicodePdfText().GetScriptRuns("\U0001F600A");
        UnicodePdfScriptRun[] invalidRuns = new UnicodePdfText().GetScriptRuns("\uD800A");

        Assert.That(supplementaryRuns, Has.Length.EqualTo(1));
        AssertRun(supplementaryRuns[0], 0, 3, UnicodeScript.Latin);
        Assert.That(invalidRuns, Has.Length.EqualTo(1));
        AssertRun(invalidRuns[0], 0, 2, UnicodeScript.Latin);
    }

    [Test]
    public void ResolveLayoutScripts_ReturnsPerScalarScripts()
    {
        UnicodeScalarRecord[] scalars = new UnicodeScalarReader().ReadAll("0A\u0370");

        Assert.That(
            UnicodePdfText.ResolveLayoutScripts(scalars),
            Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin, UnicodeScript.Latin, UnicodeScript.Greek }));
    }

    [Test]
    public void GetScriptRuns_ReturnsNoRunsForEmptyText()
    {
        Assert.That(new UnicodePdfText().GetScriptRuns(""), Is.Empty);
    }

    [Test]
    public void ScriptRunHelpers_RejectInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().GetScriptRuns(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.ResolveLayoutScripts(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.FindNearestStrongLayoutScript(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.FindNearestStrongLayoutScript(new UnicodeScalarReader().ReadAll("A"), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.FindNearestStrongLayoutScript(new UnicodeScalarReader().ReadAll("A"), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfScriptRun(-1, 0, UnicodeScript.Latin));
        Assert.Throws<ArgumentException>(() => new UnicodePdfScriptRun(2, 1, UnicodeScript.Latin));
    }

    static void AssertRun(
        UnicodePdfScriptRun run,
        int startUtf16Index,
        int endUtf16Index,
        UnicodeScript script)
    {
        Assert.That(run.StartUtf16Index, Is.EqualTo(startUtf16Index));
        Assert.That(run.EndUtf16Index, Is.EqualTo(endUtf16Index));
        Assert.That(run.Utf16Length, Is.EqualTo(endUtf16Index - startUtf16Index));
        Assert.That(run.Script, Is.EqualTo(script));
        Assert.That(run.Contains(startUtf16Index), Is.True);
        Assert.That(run.Contains(endUtf16Index), Is.False);
    }
}
