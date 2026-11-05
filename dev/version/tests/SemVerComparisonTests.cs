using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerComparisonTests
{
    [Test]
    public void ComparePrecedence_OrdersCoreNumericComponents()
    {
        AssertPrecedence("1.0.0", "2.0.0", -1);
        AssertPrecedence("2.1.0", "2.2.0", -1);
        AssertPrecedence("2.2.9", "2.2.10", -1);
        AssertPrecedence("10.0.0", "2.0.0", 1);
    }

    [Test]
    public void ComparePrecedence_OrdersPrereleaseIdentifiers()
    {
        AssertPrecedence("1.0.0-alpha", "1.0.0-alpha.1", -1);
        AssertPrecedence("1.0.0-alpha.1", "1.0.0-alpha.beta", -1);
        AssertPrecedence("1.0.0-alpha.beta", "1.0.0-beta", -1);
        AssertPrecedence("1.0.0-beta.2", "1.0.0-beta.11", -1);
        AssertPrecedence("1.0.0-rc.1", "1.0.0", -1);
    }

    [Test]
    public void ComparePrecedence_IgnoresBuildMetadata()
    {
        UniversalVersion left = ParseVersion("1.2.3+build.1");
        UniversalVersion right = ParseVersion("1.2.3+build.2");

        Assert.That(SemVerVersionComparer.ComparePrecedence(left, right), Is.EqualTo(0));
        Assert.That(left.CompareToVersion(right, VersionComparisonMode.Precedence), Is.EqualTo(0));
        Assert.That(left.EqualsByPrecedence(right), Is.True);
    }

    [Test]
    public void IdentityEquality_ConsidersBuildMetadataText()
    {
        UniversalVersion buildOne = ParseVersion("1.2.3+1");
        UniversalVersion buildZeroOne = ParseVersion("1.2.3+01");
        UniversalVersion noBuild = ParseVersion("1.2.3");

        Assert.That(buildOne.EqualsByPrecedence(buildZeroOne), Is.True);
        Assert.That(buildOne.EqualsByIdentity(buildZeroOne), Is.False);
        Assert.That(buildOne.EqualsByNormalizedIdentity(buildZeroOne), Is.True);
        Assert.That(buildOne.EqualsByIdentity(noBuild), Is.False);
        Assert.That(buildOne.CompareToVersion(buildZeroOne, VersionComparisonMode.Identity), Is.Not.EqualTo(0));
        Assert.That(buildOne.CompareToVersion(buildZeroOne, VersionComparisonMode.NormalizedIdentity), Is.EqualTo(0));
    }

    [Test]
    public void IdentityEquality_RequiresSamePrereleaseAndCoreComponents()
    {
        UniversalVersion alphaOne = ParseVersion("1.2.3-alpha.1+build");
        UniversalVersion alphaTwo = ParseVersion("1.2.3-alpha.2+build");
        UniversalVersion patch = ParseVersion("1.2.4-alpha.1+build");

        Assert.That(alphaOne.EqualsByIdentity(ParseVersion("1.2.3-alpha.1+build")), Is.True);
        Assert.That(alphaOne.EqualsByIdentity(alphaTwo), Is.False);
        Assert.That(alphaOne.EqualsByNormalizedIdentity(alphaTwo), Is.False);
        Assert.That(alphaOne.EqualsByPrecedence(patch), Is.False);
    }

    static void AssertPrecedence(string leftText, string rightText, int expectedSign)
    {
        UniversalVersion left = ParseVersion(leftText);
        UniversalVersion right = ParseVersion(rightText);
        int comparison = SemVerVersionComparer.ComparePrecedence(left, right);

        Assert.That(comparison, Is.EqualTo(expectedSign));
        Assert.That(SemVerVersionComparer.ComparePrecedence(right, left), Is.EqualTo(0 - expectedSign));
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
