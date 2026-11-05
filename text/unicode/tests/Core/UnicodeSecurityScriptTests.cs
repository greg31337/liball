using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityScriptTests
{
    [Test]
    public void GetScriptSet_ReturnsDistinctNonNeutralScriptsInEncounterOrder()
    {
        Assert.That(
            new UnicodeSecurity().GetScriptSet("A\u0391\u0628"),
            Is.EqualTo(new UnicodeScript[]
            {
                UnicodeScript.Latin,
                UnicodeScript.Greek,
                UnicodeScript.Arabic,
            }));
    }

    [Test]
    public void GetScriptSet_IgnoresCommonInheritedAndUnknownScripts()
    {
        Assert.That(new UnicodeSecurity().GetScriptSet("A0\U0001F600"), Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin }));
        Assert.That(new UnicodeSecurity().GetScriptSet("0\U0001F600"), Is.Empty);
    }

    [Test]
    public void IsMixedScript_ReturnsTrueWhenMultipleNonNeutralScriptsAppear()
    {
        Assert.That(new UnicodeSecurity().IsMixedScript("A\u0391"), Is.True);
        Assert.That(new UnicodeSecurity().IsMixedScript("\u0391\u0628"), Is.True);
    }

    [Test]
    public void IsMixedScript_ReturnsFalseForSingleScriptOrNeutralOnlyText()
    {
        Assert.That(new UnicodeSecurity().IsMixedScript("Az09"), Is.False);
        Assert.That(new UnicodeSecurity().IsMixedScript("09"), Is.False);
        Assert.That(new UnicodeSecurity().IsMixedScript("\u306D\u30AC"), Is.False);
        Assert.That(new UnicodeSecurity().IsMixedScript("\u3006\u5207"), Is.False);
        Assert.That(new UnicodeSecurity().IsMixedScript(""), Is.False);
    }

    [Test]
    public void ScriptHelpers_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().GetScriptSet(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().IsMixedScript(null!));
    }
}
