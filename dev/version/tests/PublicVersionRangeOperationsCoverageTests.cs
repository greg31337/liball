using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicVersionRangeOperationsCoverageTests
{
    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, ">=1.0.0 <2.0.0")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, ">=1.0, <2.0")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "[1.0,2.0)")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "[1.0,2.0)")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, ">= 1.0, < 2.0")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, ">=1.0,<2.0")]
    public void ParsedAndTextMembership_WorkAcrossEverySupportedRangeSyntax(
        RangeSyntax syntax,
        VersionDialect dialect,
        string rangeText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, rangeText);
        VersionParseOptions options = ParseOptions(dialect, PrereleasePolicy.Default);
        UniversalVersion accepted = ParseVersion(dialect, "1.5.0");
        UniversalVersion below = ParseVersion(dialect, "0.9.9");
        UniversalVersion upperBoundary = ParseVersion(dialect, "2.0.0");

        Assert.That(operations.ContainsVersion(range, accepted), Is.True);
        Assert.That(operations.ContainsVersion(range, below), Is.False);
        Assert.That(operations.ContainsVersion(range, upperBoundary), Is.False);
        Assert.That(operations.ContainsText(range, "1.5.0", options), Is.True);
        Assert.That(operations.ContainsText(range, "0.9.9", options), Is.False);
        Assert.That(operations.ContainsText(range, "2.0.0", options), Is.False);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, ">=1.0.0 <2.0.0", "1.5.0-alpha.1")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, ">=1.0, <2.0", "1.5.0-alpha.1")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "[1.0,2.0)", "1.5.0-alpha.1")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, ">= 1.0, < 2.0", "1.5.a")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, ">=1.0,<2.0", "1.5a1")]
    public void ExplicitPrereleaseOverride_WorksAcrossEverySupportedRangeSyntax(
        RangeSyntax syntax,
        VersionDialect dialect,
        string rangeText,
        string prereleaseText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, rangeText);
        UniversalVersion prerelease = ParseVersion(dialect, prereleaseText);
        VersionParseOptions options = ParseOptions(dialect, PrereleasePolicy.Include);

        Assert.That(operations.ContainsVersion(range, prerelease), Is.True);
        Assert.That(operations.ContainsVersionWithPrerelease(range, prerelease, false), Is.False);
        Assert.That(operations.ContainsVersionWithPrerelease(range, prerelease, true), Is.True);
        Assert.That(
            operations.ContainsTextWithPrerelease(range, prereleaseText, options, false),
            Is.False);
        Assert.That(
            operations.ContainsTextWithPrerelease(range, prereleaseText, options, true),
            Is.True);
    }

    [Test]
    public void MavenQualifiers_UseMavenOrderingRatherThanGenericPrereleaseGating()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(RangeSyntax.Maven, "[1.0,2.0)");
        UniversalVersion qualifier = ParseVersion(VersionDialect.Maven, "1.5-alpha");

        Assert.That(operations.ContainsVersion(range, qualifier), Is.True);
        Assert.That(operations.ContainsVersionWithPrerelease(range, qualifier, false), Is.True);
        Assert.That(operations.ContainsVersionWithPrerelease(range, qualifier, true), Is.True);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "1.2.x")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, "1.2.*")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "1.2.*")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, "==1.2.*")]
    public void FloatingAndPrefixPatterns_RestrictTheExpectedVersionPrefix(
        RangeSyntax syntax,
        VersionDialect dialect,
        string rangeText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, rangeText);
        VersionParseOptions options = ParseOptions(dialect, PrereleasePolicy.Default);

        Assert.That(operations.ContainsText(range, "1.2.0", options), Is.True);
        Assert.That(operations.ContainsText(range, "1.2.99", options), Is.True);
        Assert.That(operations.ContainsText(range, "1.3.0", options), Is.False);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "<1.5.0 || >1.5.0")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "(,1.5),(1.5,)")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, ">= 1.0, < 2.0, != 1.5")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, ">=1.0,<2.0,!=1.5")]
    public void ExclusionForms_CreateMembershipHoles(
        RangeSyntax syntax,
        VersionDialect dialect,
        string rangeText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, rangeText);
        VersionParseOptions options = ParseOptions(dialect, PrereleasePolicy.Default);

        Assert.That(operations.ContainsText(range, "1.4.0", options), Is.True);
        Assert.That(operations.ContainsText(range, "1.5.0", options), Is.False);
        Assert.That(operations.ContainsText(range, "1.6.0", options), Is.True);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, ">=1.0.0 <2.0.0")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, ">=1.0, <2.0")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "[1.0,2.0)")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "[1.0,2.0)")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, ">= 1.0, < 2.0")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, ">=1.0,<2.0")]
    public void MatchExplanations_AreStructuredAcrossEverySupportedRangeSyntax(
        RangeSyntax syntax,
        VersionDialect dialect,
        string rangeText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, rangeText);
        UniversalVersion accepted = ParseVersion(dialect, "1.5.0");
        UniversalVersion rejected = ParseVersion(dialect, "2.0.0");

        VersionMatchExplanation match = operations.ExplainMatch(range, accepted);
        VersionMatchExplanation failure = operations.ExplainMatch(range, rejected);

        Assert.That(match.match, Is.True);
        Assert.That(match.version, Is.SameAs(accepted));
        Assert.That(match.matchedSetIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(match.failedSetIndex, Is.EqualTo(-1));
        Assert.That(match.failedComparator, Is.Null);
        Assert.That(match.message, Is.Not.Empty);
        Assert.That(failure.match, Is.False);
        Assert.That(failure.version, Is.SameAs(rejected));
        Assert.That(failure.matchedSetIndex, Is.EqualTo(-1));
        Assert.That(failure.failedSetIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(failure.message, Is.Not.Empty);
    }

    [Test]
    public void EmptyUniversalAndFloatingRanges_ProduceDistinctExplanations()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        UniversalVersion version = ParseVersion(VersionDialect.NuGet, "1.3.0");
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();
        VersionRange floating = ParseRange(RangeSyntax.NuGet, "1.2.*");

        VersionMatchExplanation emptyResult = operations.ExplainMatch(empty, version);
        VersionMatchExplanation universalResult = operations.ExplainMatch(universal, version);
        VersionMatchExplanation floatingResult = operations.ExplainMatch(floating, version);

        Assert.That(emptyResult.match, Is.False);
        Assert.That(emptyResult.failedSetIndex, Is.EqualTo(-1));
        Assert.That(emptyResult.message, Is.EqualTo("Range is empty."));
        Assert.That(universalResult.match, Is.True);
        Assert.That(universalResult.matchedSetIndex, Is.EqualTo(0));
        Assert.That(floatingResult.match, Is.False);
        Assert.That(floatingResult.failedSetIndex, Is.EqualTo(0));
        Assert.That(floatingResult.failedComparator, Is.Null);
        Assert.That(floatingResult.message, Is.EqualTo("Version does not satisfy NuGet floating version."));
    }

    [TestCase(RangeSyntax.NodeSemVer, ">=bad", NodeRangeDiagnosticCodes.InvalidVersion)]
    [TestCase(RangeSyntax.Cargo, "!=1.2.3", CargoRangeDiagnosticCodes.InvalidOperator)]
    [TestCase(RangeSyntax.NuGet, "(1.0)", NuGetRangeDiagnosticCodes.InvalidRange)]
    [TestCase(RangeSyntax.Maven, "(1.0)", MavenRangeDiagnosticCodes.InvalidRange)]
    [TestCase(RangeSyntax.RubyGems, ">= bad", RubyGemsRequirementDiagnosticCodes.InvalidVersion)]
    [TestCase(RangeSyntax.Python, "1.0", PythonSpecifierDiagnosticCodes.InvalidOperator)]
    public void FailedRangeParsing_PropagatesDiagnosticsAcrossEverySupportedSyntax(
        RangeSyntax syntax,
        string sourceText,
        string diagnosticCode)
    {
        IVersionRangeParsing parsing = new VersionService();
        VersionRangeParseResult result = parsing.TryParseRange(sourceText, RangeOptions(syntax));

        Assert.That(result.success, Is.False);
        Assert.That(result.sourceText, Is.EqualTo(sourceText));
        Assert.That(result.syntax, Is.EqualTo(syntax));
        Assert.That(result.range, Is.Null);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.diagnostics[0].code, Is.EqualTo(diagnosticCode));
        Assert.That(result.diagnostics[0].message, Is.Not.Empty);
        Assert.That(result.diagnostics[0].sourceText, Is.EqualTo(sourceText));
        Assert.That(result.diagnostics[0].index, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.diagnostics[0].lineNumber, Is.EqualTo(1));
        Assert.That(result.diagnostics[0].columnNumber, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Operations_RejectEveryNullInputAtThePublicBoundary()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = operations.CreateUniversalRange();
        UniversalVersion version = ParseVersion(VersionDialect.SemVer, "1.2.3");
        VersionParseOptions options = ParseOptions(VersionDialect.SemVer, PrereleasePolicy.Default);

        Assert.That(() => operations.IsEmpty(null!), Throws.ArgumentNullException);
        Assert.That(() => operations.IsUniversal(null!), Throws.ArgumentNullException);
        Assert.That(() => operations.ContainsVersion(null!, version), Throws.ArgumentNullException);
        Assert.That(() => operations.ContainsVersion(range, null!), Throws.ArgumentNullException);
        Assert.That(
            () => operations.ContainsVersionWithPrerelease(null!, version, true),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.ContainsVersionWithPrerelease(range, null!, true),
            Throws.ArgumentNullException);
        Assert.That(() => operations.ContainsText(null!, "1.2.3", options), Throws.ArgumentNullException);
        Assert.That(() => operations.ContainsText(range, null!, options), Throws.ArgumentNullException);
        Assert.That(() => operations.ContainsText(range, "1.2.3", null!), Throws.ArgumentNullException);
        Assert.That(
            () => operations.ContainsTextWithPrerelease(null!, "1.2.3", options, true),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.ContainsTextWithPrerelease(range, null!, options, true),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.ContainsTextWithPrerelease(range, "1.2.3", null!, true),
            Throws.ArgumentNullException);
        Assert.That(() => operations.ExplainMatch(null!, version), Throws.ArgumentNullException);
        Assert.That(() => operations.ExplainMatch(range, null!), Throws.ArgumentNullException);
        Assert.That(
            () => operations.ExplainMatchWithPrerelease(null!, version, true),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.ExplainMatchWithPrerelease(range, null!, true),
            Throws.ArgumentNullException);
    }

    [Test]
    public void PublicRangeParser_RejectsNullInputs()
    {
        IVersionRangeParsing parsing = new VersionService();
        VersionRangeParseOptions options = RangeOptions(RangeSyntax.NodeSemVer);

        Assert.That(() => parsing.ParseRange(null!, options), Throws.ArgumentNullException);
        Assert.That(() => parsing.ParseRange("*", null!), Throws.ArgumentNullException);
        Assert.That(() => parsing.TryParseRange(null!, options), Throws.ArgumentNullException);
        Assert.That(() => parsing.TryParseRange("*", null!), Throws.ArgumentNullException);
    }

    static VersionRange ParseRange(RangeSyntax syntax, string text)
    {
        IVersionRangeParsing parsing = new VersionService();
        return parsing.ParseRange(text, RangeOptions(syntax));
    }

    static UniversalVersion ParseVersion(VersionDialect dialect, string text)
    {
        IVersionParsing parsing = new VersionService();
        return parsing.Parse(text, ParseOptions(dialect, PrereleasePolicy.Include));
    }

    static VersionParseOptions ParseOptions(VersionDialect dialect, PrereleasePolicy prereleasePolicy)
    {
        return new VersionParseOptions(
            dialect,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            prereleasePolicy);
    }

    static VersionRangeParseOptions RangeOptions(RangeSyntax syntax)
    {
        return new VersionRangeParseOptions(
            syntax,
            PrereleasePolicy.Default,
            4096,
            true,
            true,
            true,
            true,
            true);
    }
}
