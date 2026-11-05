using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonFormattingCompatibilityTests
{
    [Test]
    public void Compatibility_PostReleaseSpellingsNormalizeToDotPost()
    {
        AssertPythonFormat("1.0-1", "1.0.post1");
        AssertPythonFormat("1.0.post", "1.0.post0");
        AssertPythonFormat("1.0rev0002", "1.0.post2");
        AssertPythonFormat("1.0_r3", "1.0.post3");
    }

    [Test]
    public void Compatibility_DevelopmentReleaseSpellingsNormalizeToDotDev()
    {
        AssertPythonFormat("1.0dev", "1.0.dev0");
        AssertPythonFormat("1.0.dev0004", "1.0.dev4");
        AssertPythonFormat("1.0-dev_5", "1.0.dev5");
    }

    [Test]
    public void Compatibility_LocalVersionSeparatorsCaseAndNumericSegmentsNormalize()
    {
        AssertPythonFormat("1.0+ABC-001_def002", "1.0+abc.1.def002");
        AssertPythonFormat("1.0+0100", "1.0+100");
        AssertPythonFormat("1.0+Foo0100", "1.0+foo0100");
    }

    [Test]
    public void Compatibility_EpochAndReleaseIntegersNormalize()
    {
        AssertPythonFormat("0001!001.002", "1!1.2");
        AssertPythonFormat("0!01.000.5", "1.0.5");
        AssertPythonFormat("0002!000", "2!0");
    }

    [Test]
    public void Compatibility_PreReleaseAliasesAndSeparatorsNormalize()
    {
        AssertPythonFormat("1.0alpha1", "1.0a1");
        AssertPythonFormat("1.0beta-2", "1.0b2");
        AssertPythonFormat("1.0c_3", "1.0rc3");
        AssertPythonFormat("1.0PREVIEW4", "1.0rc4");
    }

    [Test]
    public void Compatibility_UniversalVersionPreservesOriginalDisplayWhileFormattingCanonicalPython()
    {
        string originalText = "v0002!01.0PREVIEW-0004.post_0005.dev-0006+LOCAL_0007";
        UniversalVersion version = RequireVersion(PythonVersions.Parse(originalText));

        Assert.That(version.ToOriginalString(), Is.EqualTo(originalText));
        Assert.That(version.ToDisplayString(), Is.EqualTo(originalText));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
    }

    [Test]
    public void Compatibility_InvalidSerializerStatesAreRejected()
    {
        Assert.That(
            delegate
            {
                new PythonVersion(
                    "",
                    "0",
                    new string[] { "1" },
                    true,
                    "a",
                    "",
                    false,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[0]);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonVersion(
                    "",
                    "0",
                    new string[] { "1" },
                    false,
                    "",
                    "",
                    true,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[0]);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonVersion(
                    "",
                    "0",
                    new string[] { "1" },
                    false,
                    "",
                    "",
                    false,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                PythonVersionFormatter.FormatCanonical(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static void AssertPythonFormat(string sourceText, string expectedText)
    {
        PythonVersion nativeVersion = PythonVersionParser.ParseVersion(sourceText);
        UniversalVersion universalVersion = RequireVersion(PythonVersions.Parse(sourceText));

        Assert.That(nativeVersion.ToNormalizedString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(nativeVersion.ToCanonicalString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(universalVersion.ToNormalizedString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(universalVersion.ToCanonicalString(), Is.EqualTo(expectedText), sourceText);
    }

    static UniversalVersion RequireVersion(VersionParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.True, result.GetSourceText());
        return result.GetVersion();
    }
}
