using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetVersionNormalizationTests
{
    [Test]
    public void Normalize_RemovesLeadingZeroesAndFillsMissingComponents()
    {
        AssertNormalized("01", "1.0.0");
        AssertNormalized("01.002", "1.2.0");
        AssertNormalized("01.002.0003", "1.2.3");
    }

    [Test]
    public void Normalize_OmitsZeroRevisionAndKeepsNonZeroRevision()
    {
        AssertNormalized("1.2.3.0", "1.2.3");
        AssertNormalized("1.2.3.0000", "1.2.3");
        AssertNormalized("1.2.3.0004", "1.2.3.4");
    }

    [Test]
    public void Normalize_RemovesBuildMetadataFromPackageIdentity()
    {
        AssertNormalized("1.2.3+meta.7", "1.2.3");
        AssertNormalized("1.2.3-alpha.1+meta.7", "1.2.3-alpha.1");
        AssertNormalized("1+build.1", "1.0.0");
    }

    [Test]
    public void Normalize_IsUsedByUniversalVersionFormatting()
    {
        UniversalVersion version = NuGetVersionParser.Parse("01.002.0003.0000-alpha.1+build.5").GetVersion();

        Assert.That(NuGetVersionFormatter.Normalize(version), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("01.002.0003.0000-alpha.1+build.5"));
    }

    [Test]
    public void Normalize_ReturnsEmptyStringForInvalidText()
    {
        Assert.That(NuGetVersionFormatter.Normalize("not-a-version"), Is.EqualTo(""));
    }

    static void AssertNormalized(string text, string normalizedText)
    {
        Assert.That(NuGetVersionFormatter.Normalize(text), Is.EqualTo(normalizedText));
        Assert.That(NuGetVersionParser.Parse(text).GetVersion().ToNormalizedString(), Is.EqualTo(normalizedText));
    }
}
