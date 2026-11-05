using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class NormalizationEngineTests
{
    [Test]
    public void AlphaCodesNormalizeCaseAndOptionalOuterWhitespace()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeCountryAlpha2("US", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Alpha2,
            "US");
        AssertResult(
            normalizer.NormalizeCountryAlpha2("us", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha2,
            "US");
        AssertInvalid(normalizer.NormalizeCountryAlpha2(" us ", false));
        AssertResult(
            normalizer.NormalizeCountryAlpha2("\tus\n", true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha2,
            "US");

        AssertResult(
            normalizer.NormalizeCountryAlpha3("usa", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha3,
            "USA");
        AssertResult(
            normalizer.NormalizeCountryAlpha4("zzzz", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha4,
            "ZZZZ");
        AssertInvalid(normalizer.NormalizeCountryAlpha3("US1", true));
        AssertInvalid(normalizer.NormalizeCountryAlpha4(null!, true));
    }

    [Test]
    public void NumericNormalizationPreservesAndCreatesThreeDigitText()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeCountryNumericCode("004", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Numeric3,
            "004");
        AssertInvalid(normalizer.NormalizeCountryNumericCode("4", false));
        AssertResult(
            normalizer.NormalizeCountryNumericCode("4", true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            "004");
        AssertResult(
            normalizer.NormalizeCountryNumericCode(" 04 ", true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            "004");
        AssertResult(
            normalizer.NormalizeCountryNumericValue(4),
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            "004");
        AssertInvalid(normalizer.NormalizeCountryNumericCode("١٢٣", true));
        AssertInvalid(normalizer.NormalizeCountryNumericCode("1000", true));
        AssertInvalid(normalizer.NormalizeCountryNumericValue(-1));
        AssertInvalid(normalizer.NormalizeCountryNumericValue(1000));
    }

    [Test]
    public void AnyIdentifierDetectsEverySupportedCountryCodeShape()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeAnyCountryIdentifier("de", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha2,
            "DE");
        AssertResult(
            normalizer.NormalizeAnyCountryIdentifier("deu", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha3,
            "DEU");
        AssertResult(
            normalizer.NormalizeAnyCountryIdentifier("276", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Numeric3,
            "276");
        AssertResult(
            normalizer.NormalizeAnyCountryIdentifier("abcd", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Alpha4,
            "ABCD");
        AssertResult(
            normalizer.NormalizeAnyCountryIdentifier(" 4 ", true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            "004");
        AssertInvalid(normalizer.NormalizeAnyCountryIdentifier("D3", true));
        AssertInvalid(normalizer.NormalizeAnyCountryIdentifier("12", false));
        AssertInvalid(normalizer.NormalizeAnyCountryIdentifier(string.Empty, true));
    }

    [Test]
    public void SubdivisionNormalizationControlsWhitespaceAndSeparatorVariants()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeSubdivisionCode("US-CA", false),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Subdivision,
            "US-CA");
        AssertResult(
            normalizer.NormalizeSubdivisionCode("us-ca", false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Subdivision,
            "US-CA");
        AssertInvalid(normalizer.NormalizeSubdivisionCode("us_ca", false));
        AssertResult(
            normalizer.NormalizeSubdivisionCode(" us_ca ", true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Subdivision,
            "US-CA");
        AssertInvalid(normalizer.NormalizeSubdivisionCode("US-CALX", true));
        AssertInvalid(normalizer.NormalizeSubdivisionCode("US-C!", true));
    }

    [Test]
    public void StringAndSpanCodePathsHaveIdenticalResults()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertEquivalent(
            normalizer.NormalizeCountryAlpha2(" gb ", true),
            normalizer.NormalizeCountryAlpha2Span(" gb ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeCountryAlpha3(" gbr ", true),
            normalizer.NormalizeCountryAlpha3Span(" gbr ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeCountryNumericCode(" 44 ", true),
            normalizer.NormalizeCountryNumericCodeSpan(" 44 ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeCountryAlpha4(" yucs ", true),
            normalizer.NormalizeCountryAlpha4Span(" yucs ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeAnyCountryIdentifier(" 44 ", true),
            normalizer.NormalizeAnyCountryIdentifierSpan(" 44 ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeSubdivisionCode(" gb_wls ", true),
            normalizer.NormalizeSubdivisionCodeSpan(" gb_wls ".AsSpan(), true));
        AssertEquivalent(
            normalizer.NormalizeCountryAlpha2("G1", false),
            normalizer.NormalizeCountryAlpha2Span("G1".AsSpan(), false));
    }

    [Test]
    public void LocaleNormalizationAppliesCanonicalCasingAndPreferredValues()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeLocaleTag("en-US"),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Unknown,
            "en-US");
        AssertResult(
            normalizer.NormalizeLocaleTag("EN-us"),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "en-US");
        AssertResult(
            normalizer.NormalizeLocaleTag("iw-IL"),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "he-IL");
        AssertResult(
            normalizer.NormalizeLocaleTag("I-KLINGON"),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "tlh");
        AssertInvalid(normalizer.NormalizeLocaleTag("en_US"));
        AssertInvalid(normalizer.NormalizeLocaleTag(null!));
    }

    [Test]
    public void NameKeyNormalizationControlsDiacriticsAndPunctuation()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        AssertResult(
            normalizer.NormalizeNameKey("cote d ivoire", true, true),
            NormalizationStatus.AlreadyCanonical,
            CountryCodeKind.Unknown,
            "cote d ivoire");
        AssertResult(
            normalizer.NormalizeNameKey("  Côte-d’Ivoire  ", true, true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "cote d ivoire");
        AssertResult(
            normalizer.NormalizeNameKey("Côte-d’Ivoire", false, true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "côte d ivoire");
        AssertResult(
            normalizer.NormalizeNameKey("Côte-d’Ivoire", true, false),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "cote-d’ivoire");
        AssertResult(
            normalizer.NormalizeNameKey("Ｃａｎａｄａ", true, true),
            NormalizationStatus.Normalized,
            CountryCodeKind.Unknown,
            "canada");
        AssertInvalid(normalizer.NormalizeNameKey(string.Empty, true, true));
        AssertInvalid(normalizer.NormalizeNameKey("---", true, true));
        AssertInvalid(normalizer.NormalizeNameKey("\uD800", true, true));
        AssertInvalid(normalizer.NormalizeNameKey(null!, true, true));
    }

    [Test]
    public void CanonicalOutputsAreIdempotent()
    {
        ICountryCodeNormalizer normalizer = new CountryCodeService();

        CodeNormalizationResult alpha2 =
            normalizer.NormalizeCountryAlpha2(" de ", true);
        CodeNormalizationResult subdivision =
            normalizer.NormalizeSubdivisionCode(" gb_wls ", true);
        CodeNormalizationResult locale = normalizer.NormalizeLocaleTag("IW-il");
        CodeNormalizationResult name =
            normalizer.NormalizeNameKey("  Côte-d’Ivoire  ", true, true);

        Assert.That(
            normalizer.NormalizeCountryAlpha2(alpha2.CanonicalValue!, true).Status,
            Is.EqualTo(NormalizationStatus.AlreadyCanonical));
        Assert.That(
            normalizer.NormalizeSubdivisionCode(subdivision.CanonicalValue!, true).Status,
            Is.EqualTo(NormalizationStatus.AlreadyCanonical));
        Assert.That(
            normalizer.NormalizeLocaleTag(locale.CanonicalValue!).Status,
            Is.EqualTo(NormalizationStatus.AlreadyCanonical));
        Assert.That(
            normalizer.NormalizeNameKey(name.CanonicalValue!, true, true).Status,
            Is.EqualTo(NormalizationStatus.AlreadyCanonical));
    }

    private static void AssertResult(
        CodeNormalizationResult result,
        NormalizationStatus expectedStatus,
        CountryCodeKind expectedKind,
        string expectedValue)
    {
        Assert.That(result.Status, Is.EqualTo(expectedStatus));
        Assert.That(result.Kind, Is.EqualTo(expectedKind));
        Assert.That(result.CanonicalValue, Is.EqualTo(expectedValue));
        Assert.That(result.ReplacementCandidates, Is.Empty);
    }

    private static void AssertInvalid(CodeNormalizationResult result)
    {
        Assert.That(result.Status, Is.EqualTo(NormalizationStatus.Invalid));
        Assert.That(result.Kind, Is.EqualTo(CountryCodeKind.Unknown));
        Assert.That(result.HasAssignmentStatus, Is.False);
        Assert.That(result.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.Unassigned));
        Assert.That(result.CanonicalValue, Is.Null);
        Assert.That(result.ReplacementCandidates, Is.Empty);
    }

    private static void AssertEquivalent(
        CodeNormalizationResult stringResult,
        CodeNormalizationResult spanResult)
    {
        Assert.That(spanResult.Status, Is.EqualTo(stringResult.Status));
        Assert.That(spanResult.Kind, Is.EqualTo(stringResult.Kind));
        Assert.That(spanResult.HasAssignmentStatus, Is.EqualTo(stringResult.HasAssignmentStatus));
        Assert.That(spanResult.AssignmentStatus, Is.EqualTo(stringResult.AssignmentStatus));
        Assert.That(spanResult.CanonicalValue, Is.EqualTo(stringResult.CanonicalValue));
        Assert.That(spanResult.ReplacementCandidates, Is.EqualTo(stringResult.ReplacementCandidates));
    }
}
