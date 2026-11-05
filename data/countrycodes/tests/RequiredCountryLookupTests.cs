using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class RequiredCountryLookupTests
{
    [Test]
    public void RequiredLookupsReturnTheSameCountriesAsStructuredLookups()
    {
        ICountryCatalog catalog = new CountryCodeService();

        Assert.That(catalog.GetRequiredCountryByAlpha2("gB").Alpha2, Is.EqualTo("GB"));
        Assert.That(catalog.GetRequiredCountryByAlpha3("gBr").Alpha2, Is.EqualTo("GB"));
        Assert.That(catalog.GetRequiredCountryByNumericCode("826").Alpha2, Is.EqualTo("GB"));

        Country numericValue = catalog.GetRequiredCountryByNumericValue(4);
        Assert.That(numericValue.Alpha2, Is.EqualTo("AF"));
        Assert.That(numericValue.NumericText, Is.EqualTo("004"));

        Assert.That(
            catalog.GetRequiredCountryByName("Federal Republic of Germany").Alpha2,
            Is.EqualTo("DE"));
        Assert.That(
            catalog.GetRequiredCountryByAnyIdentifier("UK").Alpha2,
            Is.EqualTo("GB"));
    }

    [Test]
    public void RequiredLookupResultsRemainDetachedFromTheCatalog()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country first = catalog.GetRequiredCountryByAlpha2("CI");
        first.Alpha2 = "XX";
        first.Aliases[0] = "changed";
        first.LocalizedNames[0].Value = "changed";

        Country second = catalog.GetRequiredCountryByAlpha2("CI");

        Assert.That(second.Alpha2, Is.EqualTo("CI"));
        Assert.That(second.Aliases, Is.EqualTo(new[] { "Ivory Coast" }));
        Assert.That(second.LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
    }

    [Test]
    public void RequiredCodeLookupsDescribeInvalidInput()
    {
        ICountryCatalog catalog = new CountryCodeService();

        AssertInvalid(
            () => catalog.GetRequiredCountryByAlpha2(null!),
            null,
            CountryCodeKind.Alpha2,
            ValidationFailureKind.NullOrEmpty);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAlpha2("G"),
            "G",
            CountryCodeKind.Alpha2,
            ValidationFailureKind.InvalidLength);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAlpha2("G1"),
            "G1",
            CountryCodeKind.Alpha2,
            ValidationFailureKind.InvalidCharacter);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAlpha3("GB1"),
            "GB1",
            CountryCodeKind.Alpha3,
            ValidationFailureKind.InvalidCharacter);
        AssertInvalid(
            () => catalog.GetRequiredCountryByNumericCode("04"),
            "04",
            CountryCodeKind.Numeric3,
            ValidationFailureKind.InvalidLength);
        AssertInvalid(
            () => catalog.GetRequiredCountryByNumericCode("0A4"),
            "0A4",
            CountryCodeKind.Numeric3,
            ValidationFailureKind.InvalidCharacter);
        AssertInvalid(
            () => catalog.GetRequiredCountryByNumericValue(-1),
            "-1",
            CountryCodeKind.Numeric3,
            ValidationFailureKind.InvalidLength);
        AssertInvalid(
            () => catalog.GetRequiredCountryByNumericValue(1000),
            "1000",
            CountryCodeKind.Numeric3,
            ValidationFailureKind.InvalidLength);
    }

    [Test]
    public void RequiredNameAndAutomaticLookupsDescribeInvalidInput()
    {
        ICountryCatalog catalog = new CountryCodeService();

        AssertInvalid(
            () => catalog.GetRequiredCountryByName(string.Empty),
            string.Empty,
            CountryCodeKind.Unknown,
            ValidationFailureKind.NullOrEmpty);
        AssertInvalid(
            () => catalog.GetRequiredCountryByName("\uD800"),
            "\uD800",
            CountryCodeKind.Unknown,
            ValidationFailureKind.InvalidCharacter);
        AssertInvalid(
            () => catalog.GetRequiredCountryByName("---"),
            "---",
            CountryCodeKind.Unknown,
            ValidationFailureKind.InvalidShape);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAnyIdentifier("84"),
            "84",
            CountryCodeKind.Unknown,
            ValidationFailureKind.InvalidLength);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAnyIdentifier("8A0"),
            "8A0",
            CountryCodeKind.Unknown,
            ValidationFailureKind.InvalidShape);
        AssertInvalid(
            () => catalog.GetRequiredCountryByAnyIdentifier(null!),
            null,
            CountryCodeKind.Unknown,
            ValidationFailureKind.NullOrEmpty);
    }

    [Test]
    public void RequiredLookupsDescribeValidIdentifiersThatAreNotFound()
    {
        ICountryCatalog catalog = new CountryCodeService();

        AssertNotFound(
            () => catalog.GetRequiredCountryByAlpha2("ZZ"),
            "ZZ",
            CountryCodeKind.Alpha2);
        AssertNotFound(
            () => catalog.GetRequiredCountryByAlpha3("ZZZ"),
            "ZZZ",
            CountryCodeKind.Alpha3);
        AssertNotFound(
            () => catalog.GetRequiredCountryByNumericCode("000"),
            "000",
            CountryCodeKind.Numeric3);
        AssertNotFound(
            () => catalog.GetRequiredCountryByNumericValue(0),
            "000",
            CountryCodeKind.Numeric3);
        AssertNotFound(
            () => catalog.GetRequiredCountryByName("Unknown Republic"),
            "Unknown Republic",
            CountryCodeKind.Unknown);
        AssertNotFound(
            () => catalog.GetRequiredCountryByAnyIdentifier("ZZZ"),
            "ZZZ",
            CountryCodeKind.Alpha3);
    }

    [Test]
    public void RequiredLookupReportsEveryAmbiguousCandidateInResultOrder()
    {
        GeneratedCountryRow[] rows =
        {
            CountryIndex.FindCountryByAlpha2("DE")!,
            CountryIndex.FindCountryByAlpha2("FR")!
        };
        CountryLookupResult result = CountryLookupEngine.CreateCandidateResult(rows);

        AmbiguousCountryException exception = Assert.Throws<AmbiguousCountryException>(
            () => RequiredCountryLookup.TranslateResult(
                result,
                "shared name",
                CountryCodeKind.Unknown,
                ValidationFailureKind.InvalidShape))!;

        Assert.That(exception.Input, Is.EqualTo("shared name"));
        Assert.That(exception.CandidateCodes, Is.EqualTo(new[] { "DE", "FR" }));
    }

    private static void AssertInvalid(
        TestDelegate action,
        string? input,
        CountryCodeKind expectedKind,
        ValidationFailureKind failureKind)
    {
        InvalidCountryCodeException exception =
            Assert.Throws<InvalidCountryCodeException>(action)!;
        Assert.That(exception.Input, Is.EqualTo(input));
        Assert.That(exception.ExpectedKind, Is.EqualTo(expectedKind));
        Assert.That(exception.FailureKind, Is.EqualTo(failureKind));
    }

    private static void AssertNotFound(
        TestDelegate action,
        string input,
        CountryCodeKind kind)
    {
        CountryNotFoundException exception = Assert.Throws<CountryNotFoundException>(action)!;
        Assert.That(exception.Input, Is.EqualTo(input));
        Assert.That(exception.Kind, Is.EqualTo(kind));
    }
}
