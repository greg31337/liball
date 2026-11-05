using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerRangeFacadeHelperTests
{
    [Test]
    public void Satisfies_UsesNodeRangeParsingAndPrereleasePolicyOptions()
    {
        Assert.That(NodeSemVer.Satisfies("1.2.3", "^1.2.0"), Is.True);
        Assert.That(NodeSemVer.Satisfies("2.0.0", "^1.2.0"), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.3-alpha.1", ">=1.2.0"), Is.False);

        VersionSelectionOptions includePrerelease = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);
        VersionSelectionOptions excludePrerelease = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Exclude);

        Assert.That(NodeSemVer.Satisfies("1.2.3-alpha.1", ">=1.2.0", includePrerelease), Is.True);
        Assert.That(NodeSemVer.Satisfies("1.2.3-alpha.1", ">=1.2.0", excludePrerelease), Is.False);
    }

    [Test]
    public void Satisfies_ReturnsFalseForInvalidInputs()
    {
        Assert.That(NodeSemVer.Satisfies(null, "^1.2.0"), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.3", null), Is.False);
        Assert.That(NodeSemVer.Satisfies("bad", "^1.2.0"), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.3", ">=bad"), Is.False);
    }

    [Test]
    public void ToComparators_ReturnsComparatorSetsForRangeDisjunctions()
    {
        string[][] comparatorSets = NodeSemVer.ToComparators("^1.2.3 || ~2.0");

        Assert.That(comparatorSets.Length, Is.EqualTo(2));
        Assert.That(comparatorSets[0], Is.EqualTo(new string[] { ">=1.2.3", "<2.0.0-0" }));
        Assert.That(comparatorSets[1], Is.EqualTo(new string[] { ">=2.0.0", "<2.1.0-0" }));
    }

    [Test]
    public void ToComparators_ReturnsEmptyComparatorSetForUniversalRange()
    {
        string[][] comparatorSets = NodeSemVer.ToComparators("*");

        Assert.That(comparatorSets.Length, Is.EqualTo(1));
        Assert.That(comparatorSets[0], Is.EqualTo(new string[0]));
    }

    [Test]
    public void MinVersion_ReturnsMinimumRepresentableSatisfyingVersion()
    {
        Assert.That(NodeSemVer.MinVersion(">=1.2.3 <2.0.0"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.MinVersion(">1.2.3 <2.0.0"), Is.EqualTo("1.2.4"));
        Assert.That(NodeSemVer.MinVersion("<1.0.0"), Is.EqualTo("0.0.0"));
        Assert.That(NodeSemVer.MinVersion("1.2.3 || >=2.0.0"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.MinVersion(">1.2.3 <1.2.4"), Is.Null);
    }

    [Test]
    public void ValidRange_ReturnsNormalizedComparatorText()
    {
        Assert.That(NodeSemVer.ValidRange("^1.2.3"), Is.EqualTo(">=1.2.3 <2.0.0-0"));
        Assert.That(NodeSemVer.ValidRange("1.2.x || >=2.0.0 <3.0.0"), Is.EqualTo(">=1.2.0 <1.3.0-0 || >=2.0.0 <3.0.0"));
        Assert.That(NodeSemVer.ValidRange("*"), Is.EqualTo("*"));
        Assert.That(NodeSemVer.ValidRange(">=bad"), Is.Null);
        Assert.That(NodeSemVer.ValidRange(null), Is.Null);
    }

    [Test]
    public void OutsideGtrAndLtr_ClassifyVersionsOutsideBoundedRanges()
    {
        Assert.That(NodeSemVer.Outside("2.0.0", ">=1.0.0 <2.0.0", ">"), Is.True);
        Assert.That(NodeSemVer.Gtr("2.0.0", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(NodeSemVer.Outside("0.9.9", ">=1.0.0 <2.0.0", "<"), Is.True);
        Assert.That(NodeSemVer.Ltr("0.9.9", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(NodeSemVer.Gtr("1.5.0", ">=1.0.0 <2.0.0"), Is.False);
        Assert.That(NodeSemVer.Ltr("1.5.0", ">=1.0.0 <2.0.0"), Is.False);
        Assert.That(NodeSemVer.Gtr("4.0.0", "<2.0.0 || >3.0.0"), Is.False);
    }

    [Test]
    public void Outside_RejectsInvalidDirectionAndReturnsFalseForInvalidInputs()
    {
        Assert.That(NodeSemVer.Outside(null, ">=1.0.0", ">"), Is.False);
        Assert.That(NodeSemVer.Outside("1.2.3", null, ">"), Is.False);
        Assert.That(NodeSemVer.Outside("bad", ">=1.0.0", ">"), Is.False);

        Assert.That(
            delegate
            {
                NodeSemVer.Outside("1.2.3", ">=1.0.0", "~");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NodeSemVer.Outside("1.2.3", ">=1.0.0", null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }
}
