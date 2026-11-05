using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerPartialParserTests
{
    [Test]
    public void ParsePartial_ParsesMissingNumericComponents()
    {
        AssertPartial("1", "1.0.0", 2);
        AssertPartial("1.2", "1.2.0", 1);
        AssertPartial("1.2.3", "1.2.3", 0);
    }

    [Test]
    public void ParsePartial_ParsesWildcardComponents()
    {
        AssertPartial("*", "0.0.0", 3);
        AssertPartial("x", "0.0.0", 3);
        AssertPartial("X", "0.0.0", 3);
        AssertPartial("1.x", "1.0.0", 2);
        AssertPartial("1.2.*", "1.2.0", 1);
    }

    [Test]
    public void ParsePartial_ParsesFullVersionPrereleaseAndBuildMetadata()
    {
        VersionParseResult result = SemVerParser.ParsePartial("1.2.3-alpha.1+build.5");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetMissingComponentCount(), Is.EqualTo(0));
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.HasBuildMetadata(), Is.True);
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("build"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
    }

    [Test]
    public void ParsePartial_TrimsOuterWhitespace()
    {
        AssertPartial("  1.2.x  ", "1.2.0", 1);
    }

    [Test]
    public void ParsePartial_RejectsMalformedRangePartials()
    {
        AssertPartialFailure("");
        AssertPartialFailure("1..2");
        AssertPartialFailure("1.*.3");
        AssertPartialFailure("1.2.3.4");
        AssertPartialFailure("1.2-alpha");
        AssertPartialFailure("v1.2");
        AssertPartialFailure("01.2.3");
    }

    static void AssertPartial(string text, string normalizedText, int missingComponentCount)
    {
        VersionParseResult result = SemVerParser.ParsePartial(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo(normalizedText));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(missingComponentCount));
    }

    static void AssertPartialFailure(string text)
    {
        VersionParseResult result = SemVerParser.ParsePartial(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
    }
}
