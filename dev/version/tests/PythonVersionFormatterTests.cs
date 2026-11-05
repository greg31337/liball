using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonVersionFormatterTests
{
    [Test]
    public void FormatCanonical_NormalizesParsedPep440AlternateSpellings()
    {
        PythonVersion version = PythonVersionParser.ParseVersion("v01.002RC0003.post-0004.dev05+Ubuntu-001.foo0100");

        Assert.That(version.ToOriginalString(), Is.EqualTo("v01.002RC0003.post-0004.dev05+Ubuntu-001.foo0100"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(PythonVersions.FormatNormalized(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(PythonVersions.FormatCanonical(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
    }

    [Test]
    public void FormatCanonical_EmitsEpochAndImplicitSuffixNumbers()
    {
        PythonVersion version = PythonVersionParser.ParseVersion("0002!01.0a.post.dev+ABC_000");

        Assert.That(version.ToNormalizedString(), Is.EqualTo("2!1.0a0.post0.dev0+abc.0"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("2!1.0a0.post0.dev0+abc.0"));
    }

    [Test]
    public void FormatCanonical_NormalizesManuallyConstructedRepresentationState()
    {
        PythonVersion version = new PythonVersion(
            "",
            "000",
            new string[] { "01", "002", "000" },
            true,
            "b",
            "0003",
            true,
            "0004",
            true,
            "0005",
            new PythonLocalVersionSegment[]
            {
                new PythonLocalVersionSegment("Ubuntu", false),
                new PythonLocalVersionSegment("0007", true),
                new PythonLocalVersionSegment("foo0100", false)
            });

        Assert.That(version.ToOriginalString(), Is.EqualTo(""));
        Assert.That(version.ToDisplayString(), Is.EqualTo("1.2.0b3.post4.dev5+ubuntu.7.foo0100"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.0b3.post4.dev5+ubuntu.7.foo0100"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2.0b3.post4.dev5+ubuntu.7.foo0100"));
    }

    [Test]
    public void FormatCanonical_PreservesReleaseSegmentArityAndOmitsZeroEpoch()
    {
        AssertFormat("1", "1");
        AssertFormat("1.0", "1.0");
        AssertFormat("1.0.0", "1.0.0");
        AssertFormat("0!01.000.5", "1.0.5");
    }

    [Test]
    public void FormatDisplay_PreservesOriginalParserTextForRoundTripDisplay()
    {
        PythonVersion version = PythonVersionParser.ParseVersion("v1.0-1+Ubuntu_1");

        Assert.That(version.ToDisplayString(), Is.EqualTo("v1.0-1+Ubuntu_1"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.0.post1+ubuntu.1"));
    }

    [Test]
    public void FormatCanonical_RoundTripsThroughPythonParserAndComparer()
    {
        AssertCanonicalRoundTrip("1!02.000rc01.post02.dev003+Local-0004");
        AssertCanonicalRoundTrip("1.0.dev");
        AssertCanonicalRoundTrip("1.0.post");
        AssertCanonicalRoundTrip("1.0+abc.01.def002");
    }

    static void AssertFormat(string sourceText, string expectedText)
    {
        PythonVersion version = PythonVersionParser.ParseVersion(sourceText);

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedText), sourceText);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedText), sourceText);
    }

    static void AssertCanonicalRoundTrip(string sourceText)
    {
        PythonVersion version = PythonVersionParser.ParseVersion(sourceText);
        string canonicalText = version.ToCanonicalString();
        PythonVersion reparsed = PythonVersionParser.ParseVersion(canonicalText);

        Assert.That(PythonVersionComparer.Compare(version, reparsed), Is.EqualTo(0), sourceText);
        Assert.That(reparsed.ToCanonicalString(), Is.EqualTo(canonicalText), sourceText);
    }
}
