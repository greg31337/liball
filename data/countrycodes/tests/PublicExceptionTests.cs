using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class PublicExceptionTests
{
    [Test]
    public void InvalidCountryCodeExceptionCarriesNullInputAndFailureContext()
    {
        InvalidCountryCodeException exception = new InvalidCountryCodeException(
            null,
            CountryCodeKind.Alpha2,
            ValidationFailureKind.NullOrEmpty);

        Assert.That(exception.Input, Is.Null);
        Assert.That(exception.ExpectedKind, Is.EqualTo(CountryCodeKind.Alpha2));
        Assert.That(exception.FailureKind, Is.EqualTo(ValidationFailureKind.NullOrEmpty));
        Assert.That(
            exception.Message,
            Is.EqualTo(
                "Invalid country code '<null>' for expected kind Alpha2: NullOrEmpty."));
    }

    [Test]
    public void CountryNotFoundExceptionCarriesLookupContext()
    {
        CountryNotFoundException exception = new CountryNotFoundException(
            "ZZZ",
            CountryCodeKind.Alpha3);

        Assert.That(exception.Input, Is.EqualTo("ZZZ"));
        Assert.That(exception.Kind, Is.EqualTo(CountryCodeKind.Alpha3));
        Assert.That(
            exception.Message,
            Is.EqualTo("No country was found for Alpha3 code 'ZZZ'."));
    }

    [Test]
    public void AmbiguousCountryExceptionDetachesCandidateCodes()
    {
        string[] candidates = { "CD", "CG" };
        AmbiguousCountryException exception = new AmbiguousCountryException(
            "Congo",
            candidates);

        candidates[0] = "changed";

        Assert.That(exception.Input, Is.EqualTo("Congo"));
        Assert.That(exception.CandidateCodes, Is.EqualTo(new[] { "CD", "CG" }));
        Assert.That(
            exception.Message,
            Is.EqualTo("Country identifier 'Congo' matched 2 countries."));
    }

    [Test]
    public void InvalidSubdivisionCodeExceptionCarriesFailureContext()
    {
        InvalidSubdivisionCodeException exception = new InvalidSubdivisionCodeException(
            "US_CA",
            ValidationFailureKind.InvalidShape);

        Assert.That(exception.Input, Is.EqualTo("US_CA"));
        Assert.That(exception.FailureKind, Is.EqualTo(ValidationFailureKind.InvalidShape));
        Assert.That(
            exception.Message,
            Is.EqualTo("Invalid subdivision code 'US_CA': InvalidShape."));
    }

    [Test]
    public void SubdivisionNotFoundExceptionCarriesLookupContext()
    {
        SubdivisionNotFoundException exception = new SubdivisionNotFoundException("US-ZZ");

        Assert.That(exception.Input, Is.EqualTo("US-ZZ"));
        Assert.That(
            exception.Message,
            Is.EqualTo("No subdivision was found for code 'US-ZZ'."));
    }

    [Test]
    public void UnsupportedLocaleExceptionCarriesCanonicalLocale()
    {
        UnsupportedLocaleException exception = new UnsupportedLocaleException("zz-ZZ");

        Assert.That(exception.Locale, Is.EqualTo("zz-ZZ"));
        Assert.That(exception.Message, Is.EqualTo("Locale 'zz-ZZ' is not supported."));
    }
}
