using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryCatalogLookupTests
{
    [Test]
    public void AlphaLookupsNormalizeAsciiCaseAndDistinguishInvalidFromUnknown()
    {
        ICountryCatalog catalog = new CountryCodeService();

        CountryLookupResult alpha2 = catalog.FindCountryByAlpha2("gB");
        AssertFound(alpha2, "GB");
        CountryLookupResult alpha3 = catalog.FindCountryByAlpha3("gBr");
        AssertFound(alpha3, "GB");

        AssertStatus(catalog.FindCountryByAlpha2("ZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByAlpha3("ZZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByAlpha2("G"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAlpha2("G1"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAlpha3("GB"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAlpha3("GB1"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAlpha2(null!), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAlpha3(null!), LookupStatus.InvalidInput);
    }

    [Test]
    public void SpanAndStringAlphaLookupsHaveIdenticalOutcomes()
    {
        ICountryCatalog catalog = new CountryCodeService();
        AssertParity(
            catalog.FindCountryByAlpha2("fR"),
            catalog.FindCountryByAlpha2Span("fR"));
        AssertParity(
            catalog.FindCountryByAlpha2("ZZ"),
            catalog.FindCountryByAlpha2Span("ZZ"));
        AssertParity(
            catalog.FindCountryByAlpha2("F1"),
            catalog.FindCountryByAlpha2Span("F1"));
        AssertParity(
            catalog.FindCountryByAlpha3("fRa"),
            catalog.FindCountryByAlpha3Span("fRa"));
        AssertParity(
            catalog.FindCountryByAlpha3("ZZZ"),
            catalog.FindCountryByAlpha3Span("ZZZ"));
        AssertParity(
            catalog.FindCountryByAlpha3("FR1"),
            catalog.FindCountryByAlpha3Span("FR1"));
    }

    [Test]
    public void NumericLookupsPreserveCanonicalPaddingAndValidateBounds()
    {
        ICountryCatalog catalog = new CountryCodeService();

        CountryLookupResult numericText = catalog.FindCountryByNumericCode("004");
        AssertFound(numericText, "AF");
        Assert.That(numericText.Country!.NumericText, Is.EqualTo("004"));
        CountryLookupResult numericValue = catalog.FindCountryByNumericValue(4);
        AssertFound(numericValue, "AF");
        Assert.That(numericValue.Country!.NumericText, Is.EqualTo("004"));

        AssertStatus(catalog.FindCountryByNumericCode("000"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByNumericValue(0), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByNumericCode("04"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByNumericCode("0A4"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByNumericCode(null!), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByNumericValue(-1), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByNumericValue(1000), LookupStatus.InvalidInput);
    }

    [Test]
    public void NameLookupSupportsCanonicalOfficialAliasAndDeprecatedNamesExactly()
    {
        ICountryCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindCountryByName("germany"), "DE");
        AssertFound(catalog.FindCountryByName("Federal Republic of Germany"), "DE");
        AssertFound(catalog.FindCountryByName("ivory coast"), "CI");
        AssertFound(catalog.FindCountryByName("Czech Republic"), "CZ");

        AssertStatus(catalog.FindCountryByName("Allemagne"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByName(" Germany "), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByName("Cote d Ivoire"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByName("Unknown Republic"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByName(string.Empty), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByName(null!), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByName("\uD800"), LookupStatus.InvalidInput);
    }

    [Test]
    public void AnyIdentifierLookupDetectsSupportedCodeFormsAndNames()
    {
        ICountryCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindCountryByAnyIdentifier("us"), "US");
        AssertFound(catalog.FindCountryByAnyIdentifier("uSa"), "US");
        AssertFound(catalog.FindCountryByAnyIdentifier("840"), "US");
        AssertFound(catalog.FindCountryByAnyIdentifier("United States"), "US");
        AssertFound(catalog.FindCountryByAnyIdentifier("Ivory Coast"), "CI");
        AssertFound(catalog.FindCountryByAnyIdentifier("UK"), "GB");

        AssertStatus(catalog.FindCountryByAnyIdentifier("ZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByAnyIdentifier("ZZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByAnyIdentifier("000"), LookupStatus.NotFound);
        AssertStatus(catalog.FindCountryByAnyIdentifier("U1"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAnyIdentifier("8A0"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAnyIdentifier("84"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAnyIdentifier(string.Empty), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindCountryByAnyIdentifier(null!), LookupStatus.InvalidInput);
    }

    [Test]
    public void LookupResultsAndCandidatesAreDetachedFromLaterCalls()
    {
        ICountryCatalog catalog = new CountryCodeService();
        CountryLookupResult first = catalog.FindCountryByAlpha2("CI");
        first.Country!.Alpha2 = "XX";
        first.Country.Aliases[0] = "changed";
        first.Country.LocalizedNames[0].Value = "changed";

        CountryLookupResult second = catalog.FindCountryByAlpha2("CI");
        AssertFound(second, "CI");
        Assert.That(second.Country!.Aliases, Is.EqualTo(new[] { "Ivory Coast" }));
        Assert.That(second.Country.LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(second.Candidates, Is.Empty);

        GeneratedCountryRow[] ambiguousRows =
        {
            CountryIndex.FindCountryByAlpha2("DE")!,
            CountryIndex.FindCountryByAlpha2("FR")!
        };
        CountryLookupResult ambiguous =
            CountryLookupEngine.CreateCandidateResult(ambiguousRows);
        Assert.That(ambiguous.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(ambiguous.Country, Is.Null);
        Assert.That(ambiguous.Candidates, Has.Length.EqualTo(2));
        Assert.That(ambiguous.Candidates[0].Alpha2, Is.EqualTo("DE"));
        Assert.That(ambiguous.Candidates[1].Alpha2, Is.EqualTo("FR"));
        ambiguous.Candidates[0].Alpha2 = "XX";

        CountryLookupResult repeatedAmbiguous =
            CountryLookupEngine.CreateCandidateResult(ambiguousRows);
        Assert.That(repeatedAmbiguous.Candidates[0].Alpha2, Is.EqualTo("DE"));
    }

    private static void AssertFound(CountryLookupResult result, string alpha2)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Country, Is.Not.Null);
        Assert.That(result.Country!.Alpha2, Is.EqualTo(alpha2));
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertStatus(CountryLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertParity(
        CountryLookupResult textResult,
        CountryLookupResult spanResult)
    {
        Assert.That(spanResult.Status, Is.EqualTo(textResult.Status));
        if (textResult.Country == null)
        {
            Assert.That(spanResult.Country, Is.Null);
        }
        else
        {
            Assert.That(spanResult.Country, Is.Not.Null);
            Assert.That(spanResult.Country!.Alpha2, Is.EqualTo(textResult.Country.Alpha2));
            Assert.That(spanResult.Country, Is.Not.SameAs(textResult.Country));
        }

        Assert.That(spanResult.Candidates, Is.EqualTo(textResult.Candidates));
    }
}
