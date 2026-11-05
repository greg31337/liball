using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonUniversalVersionFormattingTests
{
    [Test]
    public void UniversalVersion_UsesPythonFormatterForNormalizedAndCanonicalText()
    {
        string originalText = "v01.002RC0003.post-0004.dev05+Ubuntu-001.foo0100";
        UniversalVersion version = RequireVersion(PythonVersions.Parse(originalText));

        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.Python));
        Assert.That(version.HasPythonVersion(), Is.True);
        Assert.That(version.ToOriginalString(), Is.EqualTo(originalText));
        Assert.That(version.ToDisplayString(), Is.EqualTo(originalText));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(VersionFormatter.FormatNormalized(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(VersionFormatter.FormatCanonical(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
    }

    [Test]
    public void UniversalVersion_PreservesPythonEpochPostDevAndLocalSegments()
    {
        UniversalVersion version = RequireVersion(PythonVersions.Parse("0002!01.0a.post.dev+ABC_000"));
        PythonVersion pythonVersion = version.GetPythonVersion();

        Assert.That(version.ToNormalizedString(), Is.EqualTo("2!1.0a0.post0.dev0+abc.0"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("2!1.0a0.post0.dev0+abc.0"));
        Assert.That(pythonVersion.GetEpoch(), Is.EqualTo("2"));
        Assert.That(pythonVersion.HasPostRelease(), Is.True);
        Assert.That(pythonVersion.HasDevelopmentRelease(), Is.True);
        Assert.That(pythonVersion.GetLocalSegmentCount(), Is.EqualTo(2));
    }

    [Test]
    public void UniversalVersion_PythonCanonicalTextRoundTripsThroughParser()
    {
        AssertPythonCanonicalRoundTrip("1!02.000rc01.post02.dev003+Local-0004");
        AssertPythonCanonicalRoundTrip("1.0.post");
        AssertPythonCanonicalRoundTrip("1.0.dev");
        AssertPythonCanonicalRoundTrip("1.0+abc.01.def002");
    }

    [Test]
    public void UniversalVersion_ThrowsWhenPythonMetadataIsRequestedFromOtherDialects()
    {
        UniversalVersion version = RequireVersion(SemVerParser.ParseStrict("1.2.3"));

        Assert.That(version.HasPythonVersion(), Is.False);
        Assert.That(
            delegate
            {
                version.GetPythonVersion();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void UniversalVersion_RejectsPythonMetadataOnNonPythonDialect()
    {
        PythonVersion pythonVersion = PythonVersionParser.ParseVersion("1.0");

        Assert.That(
            delegate
            {
                new UniversalVersion(
                    "1.0",
                    VersionDialect.SemVer,
                    new string[] { "1", "0", "0" },
                    new VersionIdentifier[0],
                    new VersionIdentifier[0],
                    0,
                    false,
                    false,
                    "",
                    pythonVersion);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static void AssertPythonCanonicalRoundTrip(string sourceText)
    {
        UniversalVersion version = RequireVersion(PythonVersions.Parse(sourceText));
        string canonicalText = version.ToCanonicalString();
        UniversalVersion reparsed = RequireVersion(PythonVersions.Parse(canonicalText));

        Assert.That(PythonVersions.Compare(version.ToOriginalString(), reparsed.ToOriginalString()), Is.EqualTo(0), sourceText);
        Assert.That(reparsed.ToCanonicalString(), Is.EqualTo(canonicalText), sourceText);
    }

    static UniversalVersion RequireVersion(VersionParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.True, result.GetSourceText());
        return result.GetVersion();
    }
}
