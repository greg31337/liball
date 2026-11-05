using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetFloatingVersionParserTests
{
    [Test]
    public void Parse_StarMatchesStableVersionsByDefault()
    {
        VersionRange range = ParseRange("*");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsText("0.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("999.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.3.0-alpha", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.3.0-alpha", VersionDialect.NuGet, true), Is.True);
    }

    [Test]
    public void Parse_NumericFloatingPatternsMatchStablePrefix()
    {
        VersionRange major = ParseRange("6.*");
        VersionRange minor = ParseRange("6.0.*");

        Assert.That(major.ContainsText("5.9.9", VersionDialect.NuGet), Is.False);
        Assert.That(major.ContainsText("6.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(major.ContainsText("6.9.9", VersionDialect.NuGet), Is.True);
        Assert.That(major.ContainsText("7.0.0", VersionDialect.NuGet), Is.False);
        Assert.That(major.ContainsText("6.1.0-alpha", VersionDialect.NuGet), Is.False);

        Assert.That(minor.ContainsText("6.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(minor.ContainsText("6.0.99", VersionDialect.NuGet), Is.True);
        Assert.That(minor.ContainsText("6.0.0.1", VersionDialect.NuGet), Is.True);
        Assert.That(minor.ContainsText("6.1.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_PrereleaseDashStarIncludesPrereleases()
    {
        VersionRange range = ParseRange("*-*");

        Assert.That(range.ContainsText("1.1.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.1.2-alpha", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.3.0-beta", VersionDialect.NuGet), Is.True);
    }

    [Test]
    public void Parse_NumericPrereleaseDashStarIncludesPrereleasesWithinPrefix()
    {
        VersionRange range = ParseRange("1.1.*-*");

        Assert.That(range.ContainsText("1.1.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.1.1", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.1.2-alpha", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.1.2-beta", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.0-alpha", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_PrereleaseLabelFloatingMatchesLabelPrefixAndStableCore()
    {
        VersionRange range = ParseRange("1.2.0-rc.*");

        Assert.That(range.ContainsText("1.2.0-rc.1", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.0-RC.2", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.2.0-beta.1", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsText("1.2.1-rc.1", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_ResultCarriesNuGetSyntaxAndFloatingRange()
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse("6.0.*");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().GetSetCount(), Is.EqualTo(1));
        Assert.That(result.GetRange().GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(result.GetRange().ContainsText("6.0.1", VersionDialect.NuGet), Is.True);
        Assert.That(result.GetRange().ContainsText("6.1.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_AllowsOuterWhitespaceAroundFloatingPattern()
    {
        VersionRange range = ParseRange("  4.*  ");

        Assert.That(range.ContainsText("4.0.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("4.9.9", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("5.0.0", VersionDialect.NuGet), Is.False);
    }

    [Test]
    public void Parse_RejectsInvalidFloatingPatterns()
    {
        AssertFailure("1.*.0", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.*+build", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.*-rc.*", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.0-rc*", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.0-*.1", NuGetRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.0 -rc.*", NuGetRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void Parse_RejectsFloatingPatternsInsideIntervals()
    {
        AssertFailure("[1.*,2.0)", NuGetRangeDiagnosticCodes.InvalidVersion);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
