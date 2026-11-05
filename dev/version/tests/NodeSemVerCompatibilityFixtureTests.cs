using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerCompatibilityFixtureTests
{
    [Test]
    public void Satisfies_IncludePrereleaseFixturesMatchNodeSemVerGating()
    {
        VersionSelectionOptions include = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);
        VersionSelectionOptions exclude = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Exclude);

        Assert.That(NodeSemVer.Satisfies("1.2.3-beta.2", ">=1.2.3-beta.1 <1.2.4"), Is.True);
        Assert.That(NodeSemVer.Satisfies("1.2.3-beta.2", ">=1.2.3-beta.1 <1.2.4", exclude), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.4-beta.1", ">=1.2.3-beta.1 <1.2.4"), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.4-beta.1", ">=1.2.3-beta.1 <1.2.4", include), Is.True);
        Assert.That(NodeSemVer.Satisfies("2.0.0-alpha.1", "*"), Is.False);
        Assert.That(NodeSemVer.Satisfies("2.0.0-alpha.1", "*", include), Is.True);
    }

    [Test]
    public void ValidRange_LooseRangeParsingFixturesNormalizeCompatibilityInput()
    {
        Assert.That(NodeSemVer.ValidRange("  >=  v01.002.0003   <  =v02.000.0000  "), Is.EqualTo(">=1.2.3 <2.0.0"));
        Assert.That(NodeSemVer.ValidRange("v1.2.3 || = 1.2.4"), Is.EqualTo("=1.2.3 || =1.2.4"));
        Assert.That(NodeSemVer.Satisfies("v01.002.0003", "= v1.2.3"), Is.True);
        Assert.That(NodeSemVer.Satisfies("01.002.0004", "= v1.2.3"), Is.False);
    }

    [Test]
    public void ValidRange_NormalizationFixturesCoverExpandedRangeForms()
    {
        Assert.That(NodeSemVer.ValidRange(""), Is.EqualTo("*"));
        Assert.That(NodeSemVer.ValidRange("1.2"), Is.EqualTo(">=1.2.0 <1.3.0-0"));
        Assert.That(NodeSemVer.ValidRange("1.2.3 - 2.3"), Is.EqualTo(">=1.2.3 <2.4.0-0"));
        Assert.That(NodeSemVer.ValidRange("~1.2.3 || ^0.2.3"), Is.EqualTo(">=1.2.3 <1.3.0-0 || >=0.2.3 <0.3.0-0"));
    }

    [Test]
    public void SimplifyRange_FixturesReturnFiniteMatchingCandidateUnion()
    {
        string[] candidates = new string[]
        {
            "1.0.0",
            "1.2.0",
            "1.2.1",
            "1.3.0",
            "2.0.0"
        };

        Assert.That(NodeSemVer.SimplifyRange(candidates, ">=1.2.0 <1.3.0"), Is.EqualTo("=1.2.0 || =1.2.1"));
        Assert.That(NodeSemVer.SimplifyRange(candidates, "<1.0.0"), Is.EqualTo(""));
        Assert.That(NodeSemVer.SimplifyRange(candidates, "*"), Is.EqualTo("=1.0.0 || =1.2.0 || =1.2.1 || =1.3.0 || =2.0.0"));
    }

    [Test]
    public void Subset_FixturesCoverUnionsUniversalAndDisjointRanges()
    {
        Assert.That(NodeSemVer.Subset("1.2.x || 2.0.x", ">=1.0.0 <3.0.0"), Is.True);
        Assert.That(NodeSemVer.Subset(">=1.0.0 <3.0.0", "1.2.x || 2.0.x"), Is.False);
        Assert.That(NodeSemVer.Subset("1.2.x", "*"), Is.True);
        Assert.That(NodeSemVer.Subset("*", "1.2.x"), Is.False);
        Assert.That(NodeSemVer.Subset(">2.0.0 <2.0.0", "1.2.x"), Is.True);
    }

    [Test]
    public void Intersects_FixturesCoverBoundaryUniversalAndDisjointRanges()
    {
        Assert.That(NodeSemVer.Intersects("*", "1.2.x"), Is.True);
        Assert.That(NodeSemVer.Intersects("<1.0.0 || >=2.0.0", ">=1.0.0 <2.0.0"), Is.False);
        Assert.That(NodeSemVer.Intersects("<1.0.0 || >=2.0.0", ">=1.5.0 <2.5.0"), Is.True);
        Assert.That(NodeSemVer.Intersects("<=1.2.3", ">=1.2.3"), Is.True);
        Assert.That(NodeSemVer.Intersects("<1.2.3", ">=1.2.3"), Is.False);
    }

    [Test]
    public void OutsideGtrLtr_FixturesCoverBoundariesDisjunctionsAndUnboundedRanges()
    {
        Assert.That(NodeSemVer.Gtr("1.2.4", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Gtr("1.2.3", "1.2.3"), Is.False);
        Assert.That(NodeSemVer.Ltr("1.2.2", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Ltr("1.2.3", "1.2.3"), Is.False);
        Assert.That(NodeSemVer.Gtr("999.0.0", ">1.0.0"), Is.False);
        Assert.That(NodeSemVer.Ltr("0.0.1", "<1.0.0"), Is.False);
        Assert.That(NodeSemVer.Outside("1.5.0", "<1.0.0 || >2.0.0", ">"), Is.False);
        Assert.That(NodeSemVer.Outside("1.5.0", "<1.0.0 || >2.0.0", "<"), Is.False);
    }
}
