using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRoundTripFormattingTests
{
    [Test]
    public void SemVerStrict_RoundTripsOriginalNormalizedCanonicalAndDisplayText()
    {
        VersionParseResult result = SemVerParser.ParseStrict("1.2.3-alpha.1+build.5");
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToOriginalString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("1.2.3-alpha.1+build.5"));

        AssertStrictSemVerRoundTrip(version.ToNormalizedString(), version.ToNormalizedString());
        AssertStrictSemVerRoundTrip(version.ToCanonicalString(), version.ToNormalizedString());
        AssertStrictSemVerRoundTrip(version.ToDisplayString(), version.ToNormalizedString());
    }

    [Test]
    public void NodeSemVerLoose_RoundTripsNormalizedCanonicalAndDisplayText()
    {
        string originalText = "= v01.002.0003-alpha.01+build.0007";
        VersionParseResult result = SemVerParser.ParseLoose(originalText);
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToOriginalString(), Is.EqualTo(originalText));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.7"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("v1.2.3-alpha.1+build.7"));
        Assert.That(version.ToDisplayString(), Is.EqualTo(originalText));

        AssertNodeSemVerRoundTrip(version.ToNormalizedString(), version.ToNormalizedString());
        AssertNodeSemVerRoundTrip(version.ToCanonicalString(), version.ToNormalizedString());
        AssertNodeSemVerRoundTrip(version.ToDisplayString(), version.ToNormalizedString());
    }

    [Test]
    public void NuGet_RoundTripsNormalizedCanonicalAndDisplayText()
    {
        string originalText = "01.002.0003.0000-alpha.01+build.5";
        VersionParseResult result = NuGetVersionParser.Parse(originalText);
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToOriginalString(), Is.EqualTo(originalText));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(version.ToDisplayString(), Is.EqualTo(originalText));

        AssertNuGetRoundTrip(version.ToNormalizedString(), version.ToNormalizedString());
        AssertNuGetRoundTrip(version.ToCanonicalString(), version.ToNormalizedString());
        AssertNuGetRoundTrip(version.ToDisplayString(), version.ToNormalizedString());
    }

    [Test]
    public void Go_RoundTripsCanonicalAndDisplayTextUnderGoParser()
    {
        string originalText = "v1.2.3-alpha.1+build.5";
        VersionParseResult result = GoVersionParser.Parse(originalText);
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToOriginalString(), Is.EqualTo(originalText));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("v1.2.3-alpha.1"));
        Assert.That(version.ToDisplayString(), Is.EqualTo(originalText));

        AssertGoRoundTrip(version.ToCanonicalString(), version.ToCanonicalString());
        AssertGoRoundTrip(version.ToDisplayString(), version.ToCanonicalString());
        AssertStrictSemVerRoundTrip(version.ToNormalizedString(), version.ToNormalizedString());
    }

    [Test]
    public void Cargo_RoundTripsOriginalNormalizedCanonicalAndDisplayText()
    {
        VersionParseResult result = CargoVersions.Parse("1.2.3-alpha.1+build.5");
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToOriginalString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("1.2.3-alpha.1+build.5"));

        AssertCargoRoundTrip(version.ToNormalizedString(), version.ToNormalizedString());
        AssertCargoRoundTrip(version.ToCanonicalString(), version.ToNormalizedString());
        AssertCargoRoundTrip(version.ToDisplayString(), version.ToNormalizedString());
    }

    [Test]
    public void DisplayText_RoundTripsOriginalParserTextForAdditionalDialects()
    {
        AssertDisplayRoundTrip(MavenVersions.Parse("1.0-alpha"), "1.0-alpha");
        AssertDisplayRoundTrip(RubyGemsVersions.Parse("1.0.a"), "1.0.a");
        AssertDisplayRoundTrip(PythonVersions.Parse("1.0rc1"), "1.0rc1");
    }

    static UniversalVersion RequireVersion(VersionParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.True, result.GetSourceText());
        return result.GetVersion();
    }

    static void AssertStrictSemVerRoundTrip(string text, string expectedNormalizedText)
    {
        UniversalVersion version = RequireVersion(SemVerParser.ParseStrict(text));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedNormalizedText), text);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedNormalizedText), text);
    }

    static void AssertNodeSemVerRoundTrip(string text, string expectedNormalizedText)
    {
        UniversalVersion version = RequireVersion(SemVerParser.ParseLoose(text));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedNormalizedText), text);
    }

    static void AssertNuGetRoundTrip(string text, string expectedNormalizedText)
    {
        UniversalVersion version = RequireVersion(NuGetVersionParser.Parse(text));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedNormalizedText), text);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedNormalizedText), text);
    }

    static void AssertGoRoundTrip(string text, string expectedCanonicalText)
    {
        UniversalVersion version = RequireVersion(GoVersionParser.Parse(text));

        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedCanonicalText), text);
    }

    static void AssertCargoRoundTrip(string text, string expectedNormalizedText)
    {
        UniversalVersion version = RequireVersion(CargoVersions.Parse(text));

        Assert.That(version.ToNormalizedString(), Is.EqualTo(expectedNormalizedText), text);
        Assert.That(version.ToCanonicalString(), Is.EqualTo(expectedNormalizedText), text);
    }

    static void AssertDisplayRoundTrip(VersionParseResult result, string expectedDisplayText)
    {
        UniversalVersion version = RequireVersion(result);

        Assert.That(version.ToDisplayString(), Is.EqualTo(expectedDisplayText));
        Assert.That(version.ToOriginalString(), Is.EqualTo(expectedDisplayText));
    }
}
