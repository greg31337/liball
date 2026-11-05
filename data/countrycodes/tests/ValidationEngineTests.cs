using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class ValidationEngineTests
{
    [Test]
    public void SyntaxOnlyAcceptsEveryCountryCodeShapeWithoutCatalogMembership()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertValid(
            validator.ValidateCountryAlpha2("ab", ValidationPolicy.SyntaxOnly),
            "AB");
        AssertValid(
            validator.ValidateCountryAlpha3("abc", ValidationPolicy.SyntaxOnly),
            "ABC");
        AssertValid(
            validator.ValidateCountryNumericCode("000", ValidationPolicy.SyntaxOnly),
            "000");
        AssertValid(
            validator.ValidateCountryAlpha4("abcd", ValidationPolicy.SyntaxOnly),
            "ABCD");
        AssertValid(
            validator.ValidateAnyCountryCode("abc", ValidationPolicy.SyntaxOnly),
            "ABC");
    }

    [Test]
    public void CountryPoliciesDistinguishAssignedReservedHistoricalUserAndUnknownCodes()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertValid(
            validator.ValidateCountryAlpha2("us", ValidationPolicy.AssignedOnly),
            "US");
        AssertFailure(
            validator.ValidateCountryAlpha2("UK", ValidationPolicy.AssignedOnly),
            "UK",
            ValidationFailureKind.Reserved);
        AssertFailure(
            validator.ValidateCountryAlpha2("BU", ValidationPolicy.AssignedOnly),
            "BU",
            ValidationFailureKind.FormerlyAssigned);
        AssertFailure(
            validator.ValidateCountryAlpha2("AA", ValidationPolicy.AssignedOnly),
            "AA",
            ValidationFailureKind.UserAssignedDisallowed);
        AssertFailure(
            validator.ValidateCountryAlpha2("AB", ValidationPolicy.AnyKnownIso),
            "AB",
            ValidationFailureKind.Unknown);

        AssertValid(
            validator.ValidateCountryAlpha2("UK", ValidationPolicy.AnyKnownIso),
            "UK");
        AssertValid(
            validator.ValidateCountryAlpha3("QMA", ValidationPolicy.AnyKnownIso),
            "QMA");
        AssertValid(
            validator.ValidateCountryNumericCode("900", ValidationPolicy.AllowUserAssigned),
            "900");
        AssertValid(
            validator.ValidateCountryAlpha4("bumm", ValidationPolicy.AllowHistorical),
            "BUMM");
        AssertValid(
            validator.ValidateAnyCountryCode("YU", ValidationPolicy.AllowHistorical),
            "YU");
    }

    [Test]
    public void CountrySyntaxFailuresAreStructuredAndPointAtInvalidCharacters()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertFailure(
            validator.ValidateCountryAlpha2(null!, ValidationPolicy.SyntaxOnly),
            null,
            ValidationFailureKind.NullOrEmpty);
        AssertFailure(
            validator.ValidateCountryAlpha3("US", ValidationPolicy.SyntaxOnly),
            null,
            ValidationFailureKind.InvalidLength);
        AssertPositionedFailure(
            validator.ValidateCountryNumericCode("0A4", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            1);
        AssertPositionedFailure(
            validator.ValidateCountryAlpha4("AB!D", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            2);
        AssertFailure(
            validator.ValidateAnyCountryCode("A1", ValidationPolicy.SyntaxOnly),
            null,
            ValidationFailureKind.InvalidShape);
        AssertPositionedFailure(
            validator.ValidateAnyCountryCode("A!", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            1);
    }

    [Test]
    public void SubdivisionValidationSeparatesShapePrefixMembershipAndDeprecation()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertValid(
            validator.ValidateSubdivisionCode("us-ca", ValidationPolicy.AssignedOnly),
            "US-CA");
        AssertValid(
            validator.ValidateSubdivisionCode("AB-XY", ValidationPolicy.SyntaxOnly),
            "AB-XY");
        AssertFailure(
            validator.ValidateSubdivisionCode("AB-XY", ValidationPolicy.AssignedOnly),
            "AB-XY",
            ValidationFailureKind.UnknownCountryPrefix);
        AssertFailure(
            validator.ValidateSubdivisionCode("US-ZZZ", ValidationPolicy.AssignedOnly),
            "US-ZZZ",
            ValidationFailureKind.Unknown);
        AssertFailure(
            validator.ValidateSubdivisionCode("PL-MA", ValidationPolicy.AssignedOnly),
            "PL-MA",
            ValidationFailureKind.Deprecated);
        AssertValid(
            validator.ValidateSubdivisionCode("PL-MA", ValidationPolicy.AllowHistorical),
            "PL-MA");

        AssertPositionedFailure(
            validator.ValidateSubdivisionCode("US_CA", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidShape,
            2);
        AssertPositionedFailure(
            validator.ValidateSubdivisionCode("U!-CA", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            1);
    }

    [Test]
    public void LocaleValidationSeparatesBcp47ShapeFromCatalogSupport()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertValid(
            validator.ValidateLocaleTag("EN", ValidationPolicy.AssignedOnly),
            "en");
        AssertValid(
            validator.ValidateLocaleTag("zz-ZZ", ValidationPolicy.SyntaxOnly),
            "zz-ZZ");
        AssertFailure(
            validator.ValidateLocaleTag("zz-ZZ", ValidationPolicy.AnyKnownIso),
            "zz-ZZ",
            ValidationFailureKind.UnsupportedLocale);
        AssertFailure(
            validator.ValidateLocaleTag("e-US", ValidationPolicy.SyntaxOnly),
            null,
            ValidationFailureKind.InvalidShape);
        AssertPositionedFailure(
            validator.ValidateLocaleTag("en_US", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            2);
    }

    [Test]
    public void NameValidationHandlesCurrentHistoricalDeprecatedAndUnknownNames()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertValid(
            validator.ValidateNameInput("United States", ValidationPolicy.AssignedOnly),
            "united states");
        AssertValid(
            validator.ValidateNameInput("Atlantis", ValidationPolicy.SyntaxOnly),
            "atlantis");
        AssertFailure(
            validator.ValidateNameInput("Atlantis", ValidationPolicy.AssignedOnly),
            "atlantis",
            ValidationFailureKind.Unknown);
        AssertFailure(
            validator.ValidateNameInput("Antigua", ValidationPolicy.AssignedOnly),
            "antigua",
            ValidationFailureKind.Deprecated);
        AssertFailure(
            validator.ValidateNameInput(
                "Netherlands Antilles",
                ValidationPolicy.AssignedOnly),
            "netherlands antilles",
            ValidationFailureKind.FormerlyAssigned);
        AssertValid(
            validator.ValidateNameInput(
                "Netherlands Antilles",
                ValidationPolicy.AllowHistorical),
            "netherlands antilles");
    }

    [Test]
    public void NameValidationReportsMalformedUtf16AndEmptyNormalizedInput()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        AssertPositionedFailure(
            validator.ValidateNameInput("A\ud800B", ValidationPolicy.SyntaxOnly),
            ValidationFailureKind.InvalidCharacter,
            1);
        AssertFailure(
            validator.ValidateNameInput("   ", ValidationPolicy.SyntaxOnly),
            null,
            ValidationFailureKind.InvalidShape);
    }

    [Test]
    public void ExpectedInvalidInputsReturnStableIssuesWithoutThrowing()
    {
        ICountryCodeValidator validator = new CountryCodeService();

        ValidationResult first = null!;
        Assert.DoesNotThrow(() =>
        {
            first = validator.ValidateCountryAlpha2("U1", ValidationPolicy.AssignedOnly);
        });
        ValidationResult second =
            validator.ValidateCountryAlpha2("U1", ValidationPolicy.AssignedOnly);

        Assert.That(first.IsValid, Is.False);
        Assert.That(first.Issues, Has.Length.EqualTo(1));
        Assert.That(first.Issues[0].Message, Is.EqualTo("The input contains an invalid character."));
        Assert.That(second.Issues[0].Message, Is.EqualTo(first.Issues[0].Message));
    }

    private static void AssertValid(ValidationResult result, string expectedNormalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NormalizedValue, Is.EqualTo(expectedNormalized));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    private static void AssertFailure(
        ValidationResult result,
        string? expectedNormalized,
        ValidationFailureKind expectedKind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.NormalizedValue, Is.EqualTo(expectedNormalized));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Issues[0].HasPosition, Is.False);
            Assert.That(result.Issues[0].Message, Is.Not.Empty);
        });
    }

    private static void AssertPositionedFailure(
        ValidationResult result,
        ValidationFailureKind expectedKind,
        int expectedPosition)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.NormalizedValue, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Issues[0].HasPosition, Is.True);
            Assert.That(result.Issues[0].Position, Is.EqualTo(expectedPosition));
            Assert.That(result.Issues[0].Length, Is.EqualTo(1));
            Assert.That(result.Issues[0].Message, Is.Not.Empty);
        });
    }
}
