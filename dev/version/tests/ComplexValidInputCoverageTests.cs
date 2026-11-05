using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class ComplexValidInputCoverageTests
{
    [Test]
    public void NodeRange_CombinesHyphenPartialDisjunctionPrereleaseAndBuildMetadata()
    {
        VersionRange range = ParseNodeRange("1.2.3-alpha.1 - 1.2.3 || 2.0.x || >=3.0.0 <4.0.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.2.3-beta.1", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("1.2.4-alpha.1", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("2.0.99", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("2.1.0", VersionDialect.NodeSemVer), Is.False);
        Assert.That(range.ContainsText("3.5.0+build.7", VersionDialect.NodeSemVer), Is.True);
        Assert.That(range.ContainsText("4.0.0", VersionDialect.NodeSemVer), Is.False);
    }

    [Test]
    public void PythonSpecifier_CombinesEpochPrereleasePostReleaseExclusionAndUpperBound()
    {
        VersionRange range = ParsePythonSpecifier(">=1!2.0rc1, !=1!2.0.post1, <1!2.1");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.AllowsPrereleasesByDefault(), Is.True);
        Assert.That(range.ContainsText("1!2.0rc1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1!2.0", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1!2.0.post1", VersionDialect.Python), Is.False);
        Assert.That(range.ContainsText("1!2.0.post2+local.5", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1!2.1.dev1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1!2.1", VersionDialect.Python), Is.False);
    }

    [Test]
    public void MavenRange_CombinesQualifierBoundsAndMultipleUnionIntervals()
    {
        VersionRange range = ParseMavenRange("(,1.0-alpha],[1.0,2.0),(2.1,)");

        Assert.That(range.GetSetCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.0-alpha", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("1.0-beta", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("1.5-sp", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0-rc1", VersionDialect.Maven), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("2.1", VersionDialect.Maven), Is.False);
        Assert.That(range.ContainsText("2.1.1", VersionDialect.Maven), Is.True);
    }

    [Test]
    public void NuGetRange_CombinesNormalizedRevisionAndPrereleaseIntervalBounds()
    {
        VersionRange range = ParseNuGetRange("[01.002.0003.0000-alpha.1+build.5, 1.2.3.0-rc.1)");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("1.2.3-alpha.1", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.3-beta", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.3-rc.1", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.2.3", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.2.2.9", VersionDialect.NuGet), Is.False);
    }

    static VersionRange ParseNodeRange(string text)
    {
        VersionRangeParseResult result = NodeRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static VersionRange ParsePythonSpecifier(string text)
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static VersionRange ParseMavenRange(string text)
    {
        VersionRangeParseResult result = MavenRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static VersionRange ParseNuGetRange(string text)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }
}
