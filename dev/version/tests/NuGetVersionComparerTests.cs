using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetVersionComparerTests
{
    [Test]
    public void Compare_FillsMissingComponentsAndComparesRevision()
    {
        AssertCompare("1", "1.0.0", 0);
        AssertCompare("1.0", "1.0.0.0", 0);
        AssertCompare("1.0.0.1", "1.0.0", 1);
        AssertCompare("1.0.1", "1.0.0.9", 1);
        AssertCompare("02.0.0", "1.999.999", 1);
    }

    [Test]
    public void Compare_OrdersPrereleaseBeforeStable()
    {
        AssertCompare("1.0.0-alpha", "1.0.0", -1);
        AssertCompare("1.0.0-alpha", "1.0.0-beta", -1);
        AssertCompare("1.0.0-alpha.2", "1.0.0-alpha.10", -1);
        AssertCompare("1.0.0-alpha", "1.0.0-alpha.1", -1);
    }

    [Test]
    public void Compare_UsesCaseInsensitivePrereleaseTextComparison()
    {
        AssertCompare("1.0.0-alpha", "1.0.0-ALPHA", 0);
        AssertCompare("1.0.0-Rc.1", "1.0.0-rc.1", 0);
        AssertCompare("1.0.0-alpha", "1.0.0-BETA", -1);
    }

    [Test]
    public void Compare_IgnoresBuildMetadata()
    {
        AssertCompare("1.0.0+build.1", "1.0.0+build.2", 0);
        AssertCompare("1.0.0-alpha+build.1", "1.0.0-alpha+build.2", 0);
    }

    [Test]
    public void UniversalVersion_CompareToVersion_UsesNuGetComparerForNuGetVersions()
    {
        UniversalVersion left = NuGetVersionParser.Parse("1.0.0-alpha").GetVersion();
        UniversalVersion right = NuGetVersionParser.Parse("1.0.0-ALPHA").GetVersion();

        Assert.That(left.CompareToVersion(right, VersionComparisonMode.Precedence), Is.EqualTo(0));
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(NuGetVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(NuGetVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }
}
