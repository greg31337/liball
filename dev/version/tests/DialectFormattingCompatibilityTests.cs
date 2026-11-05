using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class DialectFormattingCompatibilityTests
{
    [Test]
    public void MavenFormatting_NormalizesQualifierAliases()
    {
        AssertMavenFormat("1-a", "1-alpha");
        AssertMavenFormat("1-b", "1-beta");
        AssertMavenFormat("1-m", "1-milestone");
        AssertMavenFormat("1-cr", "1-rc");
        AssertMavenFormat("1-RC", "1-rc");
        AssertMavenFormat("1-sp", "1-sp");
        AssertMavenFormat("1-GA", "1");
        AssertMavenFormat("1-final", "1");
        AssertMavenFormat("1-release", "1");
    }

    [Test]
    public void MavenFormatting_TrimsTrailingNullItems()
    {
        AssertMavenFormat("1.0", "1");
        AssertMavenFormat("1.0.0", "1");
        AssertMavenFormat("1-alpha.0", "1-alpha");
        AssertMavenFormat("1.0-ga.0", "1");
        AssertMavenFormat("0.0.0", "0");
    }

    [Test]
    public void RubyGemsFormatting_SplitsAndNormalizesAlphanumericSegments()
    {
        AssertRubyGemsFormat("1.0.a10", "1.0.a.10", "1.a.10");
        AssertRubyGemsFormat("001.002b003", "1.2.b.3", "1.2.b.3");
        AssertRubyGemsFormat("1.0.0.alpha.0005", "1.0.0.alpha.5", "1.alpha.5");
        AssertRubyGemsFormat("1.00000000000000000010", "1.10", "1.10");
    }

    [Test]
    public void RubyGemsFormatting_PreservesPrereleaseDisplayText()
    {
        UniversalVersion version = RequireVersion(RubyGemsVersions.Parse(" 1.0.a10 "));

        Assert.That(version.ToOriginalString(), Is.EqualTo("1.0.a10"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("1.0.a10"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.0.a.10"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.a.10"));
        Assert.That(RubyGemsVersionComparer.IsPrerelease(version.ToDisplayString()), Is.True);
    }

    [Test]
    public void PythonFormatting_EmitsCanonicalPep440Forms()
    {
        AssertPythonFormat("v1.0RC1", "1.0rc1");
        AssertPythonFormat("1!01.000.post0000", "1!1.0.post0");
        AssertPythonFormat("1.0.dev0001", "1.0.dev1");
        AssertPythonFormat("1.0+ABC.0005", "1.0+abc.5");
        AssertPythonFormat("1.0preview-2.post_3.dev-4+LOCAL_5", "1.0rc2.post3.dev4+local.5");
    }

    static void AssertMavenFormat(string sourceText, string expectedText)
    {
        UniversalVersion version = RequireVersion(MavenVersions.Parse(sourceText));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(MavenVersions.FormatNormalized(sourceText), Is.EqualTo(expectedText), sourceText);
        Assert.That(MavenVersions.FormatCanonical(sourceText), Is.EqualTo(expectedText), sourceText);
    }

    static void AssertRubyGemsFormat(string sourceText, string expectedNormalizedText, string expectedCanonicalText)
    {
        UniversalVersion version = RequireVersion(RubyGemsVersions.Parse(sourceText));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedNormalizedText), sourceText);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedCanonicalText), sourceText);
        Assert.That(RubyGemsVersions.FormatNormalized(sourceText), Is.EqualTo(expectedNormalizedText), sourceText);
        Assert.That(RubyGemsVersions.FormatCanonical(sourceText), Is.EqualTo(expectedCanonicalText), sourceText);
    }

    static void AssertPythonFormat(string sourceText, string expectedText)
    {
        UniversalVersion version = RequireVersion(PythonVersions.Parse(sourceText));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(PythonVersions.FormatNormalized(version), Is.EqualTo(expectedText), sourceText);
        Assert.That(PythonVersions.FormatCanonical(version), Is.EqualTo(expectedText), sourceText);
    }

    static UniversalVersion RequireVersion(VersionParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.True, result.GetSourceText());
        return result.GetVersion();
    }
}
