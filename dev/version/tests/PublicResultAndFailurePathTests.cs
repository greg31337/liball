using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicResultAndFailurePathTests
{
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-version")]
    public void VersionParsing_ReturnsDiagnosticsWhileThrowingParseRejectsInvalidText(string text)
    {
        IVersionParsing operations = new VersionService();
        VersionParseOptions options = ParseOptions(4096);

        VersionParseResult result = operations.TryParse(text, options);

        Assert.That(result.success, Is.False);
        Assert.That(result.versionPresent, Is.False);
        Assert.That(result.version, Is.Null);
        Assert.That(result.sourceText, Is.EqualTo(text));
        Assert.That(result.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.Parse(text, options), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void VersionParsing_DistinguishesNullAndOverLengthInput()
    {
        IVersionParsing operations = new VersionService();
        VersionParseOptions options = ParseOptions(3);

        Assert.That(() => operations.TryParse(null!, options), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.Parse("1.2.3", null!), Throws.TypeOf<System.ArgumentNullException>());

        VersionParseResult result = operations.TryParse("1.2.3", options);

        Assert.That(result.success, Is.False);
        Assert.That(result.version, Is.Null);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.diagnostics[0].code, Is.EqualTo(SemVerDiagnosticCodes.InputTooLong));
        Assert.That(() => operations.Parse("1.2.3", options), Throws.TypeOf<System.ArgumentException>());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("[1.0,2.0),")]
    public void RangeParsing_ReturnsDiagnosticsWhileThrowingParseRejectsInvalidText(string text)
    {
        IVersionRangeParsing operations = new VersionService();
        VersionRangeParseOptions options = RangeOptions(4096);

        VersionRangeParseResult result = operations.TryParseRange(text, options);

        Assert.That(result.success, Is.False);
        Assert.That(result.range, Is.Null);
        Assert.That(result.sourceText, Is.EqualTo(text));
        Assert.That(result.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.ParseRange(text, options), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void RangeParsing_DistinguishesNullAndOverLengthInput()
    {
        IVersionRangeParsing operations = new VersionService();
        VersionRangeParseOptions options = RangeOptions(3);

        Assert.That(() => operations.TryParseRange(null!, options), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.ParseRange("[1]", null!), Throws.TypeOf<System.ArgumentNullException>());

        VersionRangeParseResult result = operations.TryParseRange("[1.0]", options);

        Assert.That(result.success, Is.False);
        Assert.That(result.range, Is.Null);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.diagnostics[0].code, Is.EqualTo(NodeRangeDiagnosticCodes.InputTooLong));
        Assert.That(() => operations.ParseRange("[1.0]", options), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void MutatedParseOptions_AreRevalidatedAtEveryParseEntryPoint()
    {
        IVersionParsing operations = new VersionService();
        VersionParseOptions options = ParseOptions(4096);

        options.dialect = (VersionDialect)999;
        Assert.That(() => operations.TryParse("1.2.3", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = ParseOptions(4096);
        options.parseMode = (VersionParseMode)999;
        Assert.That(() => operations.Parse("1.2.3", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = ParseOptions(4096);
        options.maxInputLength = 0;
        Assert.That(() => operations.TryParse("1.2.3", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = ParseOptions(4096);
        options.prereleasePolicy = (PrereleasePolicy)999;
        Assert.That(() => operations.Parse("1.2.3", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void MutatedRangeOptions_AreRevalidatedAtEveryRangeParseEntryPoint()
    {
        IVersionRangeParsing operations = new VersionService();
        VersionRangeParseOptions options = RangeOptions(4096);

        options.rangeSyntax = (RangeSyntax)999;
        Assert.That(() => operations.TryParseRange("[1.0]", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = RangeOptions(4096);
        options.prereleasePolicy = (PrereleasePolicy)999;
        Assert.That(() => operations.ParseRange("[1.0]", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = RangeOptions(4096);
        options.maxInputLength = 0;
        Assert.That(() => operations.TryParseRange("[1.0]", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void MutatedComparisonOptions_AreRejectedByComparisonOrderingAndExtremaOperations()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        UniversalVersion version = ParseVersion(service, "1.0.0");
        UniversalVersion[] versions = new UniversalVersion[] { version };
        string[] texts = new string[] { "1.0.0" };
        VersionComparisonOptions options = ComparisonOptions();
        options.comparisonMode = (VersionComparisonMode)999;

        Assert.That(() => comparison.Compare("1.0.0", "2.0.0", options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => comparison.CompareVersions(version, version, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => comparison.SortVersionsAscending(versions, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => comparison.SortVersionsDescending(versions, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => comparison.SortTextsAscending(texts, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => comparison.SortTextsDescending(texts, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MinimumVersion(versions, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MaximumVersion(versions, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MinimumText(texts, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MaximumText(texts, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = ComparisonOptions();
        options.dialect = (VersionDialect)999;
        Assert.That(() => comparison.SortVersionsAscending(new UniversalVersion[0], options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = ComparisonOptions();
        options.invalidVersionOrdering = (InvalidVersionOrdering)999;
        Assert.That(() => selection.MinimumText(new string[0], options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void SelectionOperations_RejectInvalidPoliciesAndReturnNullForMissingResults()
    {
        VersionService service = new VersionService();
        IVersionSelection selection = service;
        VersionRange range = ParseRange(service, "[1.0,2.0)");
        VersionSelectionOptions options = new VersionSelectionOptions();
        options.prereleasePolicy = (PrereleasePolicy)999;
        UniversalVersion[] candidates = new UniversalVersion[0];

        Assert.That(() => selection.Filter(range, candidates, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MinSatisfying(range, candidates, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.MaxSatisfying(range, candidates, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => selection.BestSatisfying(range, candidates, options), Throws.TypeOf<System.ArgumentOutOfRangeException>());

        options = new VersionSelectionOptions();
        Assert.That(selection.Filter(range, candidates, options), Is.Empty);
        Assert.That(selection.MinSatisfying(range, candidates, options), Is.Null);
        Assert.That(selection.MaxSatisfying(range, candidates, options), Is.Null);
        Assert.That(selection.BestSatisfying(range, candidates, options), Is.Null);
        Assert.That(selection.MinimumVersion(candidates, ComparisonOptions()), Is.Null);
        Assert.That(selection.MaximumVersion(candidates, ComparisonOptions()), Is.Null);
        Assert.That(selection.MinimumText(new string[0], ComparisonOptions()), Is.Null);
        Assert.That(selection.MaximumText(new string[0], ComparisonOptions()), Is.Null);
    }

    [Test]
    public void CandidateOperations_RejectNullArrayElements()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionRange range = ParseRange(service, "[1.0,2.0)");
        UniversalVersion[] versions = new UniversalVersion[] { null! };
        string[] texts = new string[] { null! };
        VersionComparisonOptions comparisonOptions = ComparisonOptions();

        Assert.That(() => comparison.SortVersionsAscending(versions, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => comparison.SortVersionsDescending(versions, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => comparison.SortTextsAscending(texts, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => comparison.SortTextsDescending(texts, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => selection.MinimumVersion(versions, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => selection.MaximumVersion(versions, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => selection.MinimumText(texts, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => selection.MaximumText(texts, comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            () => selection.Filter(range, versions, new VersionSelectionOptions()),
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void FormattingAndInspection_RejectNullAndUnavailableValues()
    {
        VersionService service = new VersionService();
        IVersionFormatting formatting = service;
        IVersionInspection inspection = service;
        UniversalVersion version = ParseVersion(service, "1.2.3");

        Assert.That(() => formatting.FormatOriginal(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => formatting.FormatNormalized(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => formatting.FormatCanonical(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => formatting.FormatDisplay(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetOriginalText(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetDialect(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.IsStable(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.IsPrerelease(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.HasBuildMetadata(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetNumericComponentCount(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetNumericComponents(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetPrereleaseIdentifierCount(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetPrereleaseIdentifiers(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetBuildIdentifierCount(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetBuildIdentifiers(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetMissingComponentCount(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.HasLeadingVPrefix(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.HasRevisionComponent(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetRevisionComponent(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.HasPythonVersion(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetPythonVersion(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.EqualsByPrecedence(null!, version), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.EqualsByIdentity(version, null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.EqualsByNormalizedIdentity(null!, version), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetNumericComponent(version, -1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => inspection.GetPrereleaseIdentifier(version, 0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => inspection.GetBuildIdentifier(version, 0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(inspection.GetRevisionComponent(version), Is.Empty);
        Assert.That(() => inspection.GetPythonVersion(version), Throws.TypeOf<System.InvalidOperationException>());
    }

    private static VersionParseOptions ParseOptions(int maxInputLength)
    {
        return new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            maxInputLength,
            true,
            true,
            PrereleasePolicy.Default);
    }

    private static VersionRangeParseOptions RangeOptions(int maxInputLength)
    {
        return new VersionRangeParseOptions(
            RangeSyntax.Maven,
            PrereleasePolicy.Default,
            maxInputLength,
            true,
            true,
            true,
            true,
            true);
    }

    private static VersionComparisonOptions ComparisonOptions()
    {
        return new VersionComparisonOptions(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.Reject);
    }

    private static UniversalVersion ParseVersion(VersionService service, string text)
    {
        return service.Parse(text, ParseOptions(4096));
    }

    private static VersionRange ParseRange(VersionService service, string text)
    {
        return service.ParseRange(text, RangeOptions(4096));
    }
}
