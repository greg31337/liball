using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodePrereleaseGatingTests
{
    [Test]
    public void ContainsText_NodeSemVerExcludesPrereleaseByDefault()
    {
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");

        Assert.That(range.ContainsText("1.5.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.5.0-alpha.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha.1", VersionDialect.NodeSemVer, true), Is.True);
    }

    [Test]
    public void ContainsText_NodeSemVerAllowsPrereleaseWhenComparatorMentionsSameCore()
    {
        VersionRange range = ParseRange(">=1.2.3-alpha.1 <2.0.0");

        Assert.That(range.ContainsText("1.2.3-alpha.1", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.3-alpha.2", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.4-alpha.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.2.4-alpha.1", VersionDialect.NodeSemVer, true), Is.True);
    }

    [Test]
    public void ContainsText_NodeSemVerChecksPrereleasePermissionPerDisjunctionSet()
    {
        VersionRange range = ParseRange(">=1.2.3-alpha.1 <1.2.4 || >=2.0.0 <3.0.0");

        Assert.That(range.ContainsText("1.2.3-beta.1", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.1.0-alpha.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("2.1.0-alpha.1", VersionDialect.NodeSemVer, true), Is.True);
    }

    [Test]
    public void ContainsText_UniversalNodeRangeExcludesPrereleaseByDefault()
    {
        VersionRange range = NodeRangeParser.Parse("*").GetRange();

        Assert.That(range.ContainsText("1.0.0", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.0.0-alpha.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.0.0-alpha.1", VersionDialect.NodeSemVer, true), Is.True);
    }

    [Test]
    public void ContainsVersion_IncludePrereleaseControlsDirectVersionChecks()
    {
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion prerelease = ParseNodeVersion("1.5.0-alpha.1");

        Assert.That(range.ContainsVersion(prerelease), Is.True);
        Assert.That(range.ContainsVersionWithPrerelease(prerelease, false), Is.False);
        Assert.That(range.ContainsVersionWithPrerelease(prerelease, true), Is.True);
    }

    [Test]
    public void ContainsText_NonNodeDialectsUseExistingBehavior()
    {
        VersionRange range = VersionRange.CreateSingleComparator(
            VersionComparator.CreateGreaterThanOrEqual(ParseSemVer("1.0.0"), VersionComparisonMode.Precedence));

        Assert.That(range.ContainsText("1.5.0-alpha.1", VersionDialect.SemVer), Is.True);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha.1", VersionDialect.SemVer, false), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha.1", VersionDialect.SemVer, true), Is.True);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasRange(), Is.True);
        return result.GetRange();
    }

    static UniversalVersion ParseNodeVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static UniversalVersion ParseSemVer(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
