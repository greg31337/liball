using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetVersionCompatibilityTests
{
    [Test]
    public void Compatibility_NormalizationCases()
    {
        AssertNormalized("1", "1.0.0");
        AssertNormalized("1.2", "1.2.0");
        AssertNormalized("01.002.0003", "1.2.3");
        AssertNormalized("01.002.0003-alpha.1", "1.2.3-alpha.1");
    }

    [Test]
    public void Compatibility_RevisionCases()
    {
        AssertNormalized("1.2.3.0", "1.2.3");
        AssertNormalized("1.2.3.0000", "1.2.3");
        AssertNormalized("1.2.3.4", "1.2.3.4");
        Assert.That(NuGetVersionComparer.Compare("1.2.3.4", "1.2.3"), Is.EqualTo(1));
    }

    [Test]
    public void Compatibility_SemVer2MetadataCases()
    {
        VersionParseResult result = NuGetVersionParser.Parse("1.2.3-alpha.1+sha.0007");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().HasBuildMetadata(), Is.True);
        Assert.That(result.GetVersion().GetBuildIdentifier(0).GetText(), Is.EqualTo("sha"));
        Assert.That(result.GetVersion().GetBuildIdentifier(1).GetText(), Is.EqualTo("0007"));
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(NuGetVersionComparer.Compare("1.2.3+sha.1", "1.2.3+sha.2"), Is.EqualTo(0));
    }

    [Test]
    public void Compatibility_PrereleaseOrderingCases()
    {
        AssertCompare("1.0.0-alpha", "1.0.0-alpha.1", -1);
        AssertCompare("1.0.0-alpha.1", "1.0.0-alpha.beta", -1);
        AssertCompare("1.0.0-beta.2", "1.0.0-beta.10", -1);
        AssertCompare("1.0.0-rc.1", "1.0.0", -1);
        AssertCompare("1.0.0-alpha", "1.0.0-ALPHA", 0);
    }

    static void AssertNormalized(string text, string normalizedText)
    {
        Assert.That(NuGetVersionFormatter.Normalize(text), Is.EqualTo(normalizedText));
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(NuGetVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(NuGetVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }
}
