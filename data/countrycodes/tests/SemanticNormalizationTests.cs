using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SemanticNormalizationTests
{
    [Test]
    public void CurrentAssignmentsRemainInTheirRequestedCodeForm()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("DE", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "DE",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha3("deu", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha3,
            "DEU",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryNumericCode("276", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Numeric3,
            "276",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryNumericValue(4),
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            "004",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryNumericCode("104", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Numeric3,
            "104",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());

        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("BY", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "BY",
            CodeAssignmentStatus.Assigned,
            Array.Empty<string>());
    }

    [Test]
    public void FormerIdentifiersWithOneSuccessorReportReplacement()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("bu", false),
            NormalizationStatus.Replaced,
            CountryCodeKind.Alpha2,
            "BU",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "MM" });
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha3("BUR", false),
            NormalizationStatus.Replaced,
            CountryCodeKind.Alpha3,
            "BUR",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "MM" });
        AssertCountryResult(
            normalizer.NormalizeCountryNumericCode("278", false),
            NormalizationStatus.Replaced,
            CountryCodeKind.Numeric3,
            "278",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "DE" });
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha4("BUMM", false),
            NormalizationStatus.Replaced,
            CountryCodeKind.Alpha4,
            "BUMM",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "MM" });
    }

    [Test]
    public void FormerSplitsAndMultiStepChangesReturnEveryCandidate()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("AN", false),
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Alpha2,
            "AN",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "BQ", "CW", "SX" });
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("CS", false),
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Alpha2,
            "CS",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "CZ", "ME", "RS", "SK" });
        AssertCountryResult(
            normalizer.NormalizeCountryNumericCode("891", false),
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Numeric3,
            "891",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "ME", "RS" });
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha4("YUCS", false),
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Alpha4,
            "YUCS",
            CodeAssignmentStatus.FormerlyAssigned,
            new[] { "ME", "RS" });
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("YU", false),
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Alpha2,
            "YU",
            CodeAssignmentStatus.TransitionallyReserved,
            new[] { "ME", "RS" });
    }

    [Test]
    public void ReservedUserAssignedAndUnassignedCodesRemainUnchanged()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("UK", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "UK",
            CodeAssignmentStatus.ExceptionallyReserved,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("AA", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "AA",
            CodeAssignmentStatus.UserAssigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha3("qma", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha3,
            "QMA",
            CodeAssignmentStatus.UserAssigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryNumericCode("900", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Numeric3,
            "900",
            CodeAssignmentStatus.UserAssigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha2("AB", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "AB",
            CodeAssignmentStatus.Unassigned,
            Array.Empty<string>());
        AssertCountryResult(
            normalizer.NormalizeCountryAlpha4("ABCD", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha4,
            "ABCD",
            CodeAssignmentStatus.Unassigned,
            Array.Empty<string>());
    }

    [Test]
    public void DeprecatedSubdivisionAliasesPreserveOneOrManyReplacements()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("pl-ma", false),
            NormalizationStatus.Replaced,
            "PL-MA",
            new[] { "PL-12" });
        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("OM-BA", false),
            NormalizationStatus.Ambiguous,
            "OM-BA",
            new[] { "OM-BJ", "OM-BS" });
        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("CN-71", false),
            NormalizationStatus.Replaced,
            "CN-71",
            new[] { "TW" });
        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("US-CA", false),
            NormalizationStatus.AlreadyCanonical,
            "US-CA",
            Array.Empty<string>());
        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("US-AS", false),
            NormalizationStatus.AlreadyCanonical,
            "US-AS",
            Array.Empty<string>());
        AssertSubdivisionResult(
            normalizer.NormalizeSubdivisionCode("US-ZZZ", false),
            NormalizationStatus.AlreadyCanonical,
            "US-ZZZ",
            Array.Empty<string>());
    }

    [Test]
    public void SemanticResultsHaveStringSpanParityAndDetachedCandidates()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertEquivalent(
            normalizer.NormalizeAnyCountryIdentifier(" cs ", true),
            normalizer.NormalizeAnyCountryIdentifierSpan(" cs ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeSubdivisionCode(" om_ba ", true),
            normalizer.NormalizeSubdivisionCodeSpan(" om_ba ".AsSpan(), true));

        CodeNormalizationResult first =
            normalizer.NormalizeCountryAlpha2("AN", false);
        first.ReplacementCandidates[0] = "changed";
        Assert.That(
            normalizer.NormalizeCountryAlpha2("AN", false).ReplacementCandidates,
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));
    }

    private static void AssertCountryResult(
        CodeNormalizationResult result,
        NormalizationStatus expectedStatus,
        CountryCodeKind expectedKind,
        string expectedCanonical,
        CodeAssignmentStatus expectedAssignment,
        string[] expectedReplacements)
    {
        Assert.That(result.Status, Is.EqualTo(expectedStatus));
        Assert.That(result.Kind, Is.EqualTo(expectedKind));
        Assert.That(result.HasAssignmentStatus, Is.True);
        Assert.That(result.AssignmentStatus, Is.EqualTo(expectedAssignment));
        Assert.That(result.CanonicalValue, Is.EqualTo(expectedCanonical));
        Assert.That(result.ReplacementCandidates, Is.EqualTo(expectedReplacements));
    }

    private static void AssertSubdivisionResult(
        CodeNormalizationResult result,
        NormalizationStatus expectedStatus,
        string expectedCanonical,
        string[] expectedReplacements)
    {
        Assert.That(result.Status, Is.EqualTo(expectedStatus));
        Assert.That(result.Kind, Is.EqualTo(CountryCodeKind.Subdivision));
        Assert.That(result.HasAssignmentStatus, Is.False);
        Assert.That(result.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.Unassigned));
        Assert.That(result.CanonicalValue, Is.EqualTo(expectedCanonical));
        Assert.That(result.ReplacementCandidates, Is.EqualTo(expectedReplacements));
    }

    private static void AssertEquivalent(
        CodeNormalizationResult left,
        CodeNormalizationResult right)
    {
        Assert.That(right.Status, Is.EqualTo(left.Status));
        Assert.That(right.Kind, Is.EqualTo(left.Kind));
        Assert.That(right.HasAssignmentStatus, Is.EqualTo(left.HasAssignmentStatus));
        Assert.That(right.AssignmentStatus, Is.EqualTo(left.AssignmentStatus));
        Assert.That(right.CanonicalValue, Is.EqualTo(left.CanonicalValue));
        Assert.That(right.ReplacementCandidates, Is.EqualTo(left.ReplacementCandidates));
    }
}
