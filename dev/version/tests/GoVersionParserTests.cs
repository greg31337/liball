using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoVersionParserTests
{
    [Test]
    public void Parse_RequiresLeadingLowercaseVAndTagsGoDialect()
    {
        VersionParseResult result = GoVersionParser.Parse("v1.2.3");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.Go));

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetOriginalText(), Is.EqualTo("v1.2.3"));
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.Go));
        Assert.That(version.HasLeadingVPrefix(), Is.True);
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("1"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("2"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("3"));
        Assert.That(version.GetMissingComponentCount(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_AcceptsGoMajorAndMajorMinorShorthands()
    {
        AssertGoVersion("v1", "1.0.0", 2);
        AssertGoVersion("v1.2", "1.2.0", 1);
        AssertGoVersion("v1.2.3", "1.2.3", 0);
    }

    [Test]
    public void Parse_AcceptsPrereleaseAndBuildMetadataOnFullVersions()
    {
        VersionParseResult result = GoVersionParser.Parse("v1.2.3-alpha.1+build.5");

        Assert.That(result.IsSuccess(), Is.True);

        UniversalVersion version = result.GetVersion();
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.HasBuildMetadata(), Is.True);
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("build"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
    }

    [Test]
    public void Parse_RejectsMissingOrInvalidLeadingV()
    {
        AssertGoFailure("", GoDiagnosticCodes.MissingLeadingV);
        AssertGoFailure("1.2.3", GoDiagnosticCodes.MissingLeadingV);
        AssertGoFailure("V1.2.3", GoDiagnosticCodes.MissingLeadingV);
    }

    [Test]
    public void Parse_RejectsMalformedGoVersions()
    {
        AssertGoFailure("v", SemVerDiagnosticCodes.MissingMajor);
        AssertGoFailure("v01.2.3", SemVerDiagnosticCodes.IllegalCharacter);
        AssertGoFailure("v1.x", SemVerDiagnosticCodes.MissingMinor);
        AssertGoFailure("v1.2.3.4", SemVerDiagnosticCodes.TrailingContent);
        AssertGoFailure("v1.2-alpha", SemVerDiagnosticCodes.TrailingContent);
    }

    static void AssertGoVersion(string text, string normalizedText, int missingComponentCount)
    {
        VersionParseResult result = GoVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo(normalizedText));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(missingComponentCount));
        Assert.That(result.GetVersion().HasLeadingVPrefix(), Is.True);
    }

    static void AssertGoFailure(string text, string code)
    {
        VersionParseResult result = GoVersionParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(code));
    }
}
