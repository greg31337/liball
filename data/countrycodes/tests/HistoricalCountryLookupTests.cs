using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class HistoricalCountryLookupTests
{
    [Test]
    public void EnumerationIsStableCompleteAndDetached()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        HistoricalCountry[] first = catalog.GetHistoricalCountries();
        Assert.That(first, Has.Length.EqualTo(31));
        Assert.That(first[0].Alpha4, Is.EqualTo("AIDJ"));
        Assert.That(first[first.Length - 1].Alpha4, Is.EqualTo("ZRCD"));
        first[0].Alpha4 = "changed";
        first[0].SuccessorIdentifiers[0] = "changed";
        first[0].Changes[0].SuccessorIdentifiers[0] = "changed";

        HistoricalCountry[] second = catalog.GetHistoricalCountries();
        Assert.That(second[0].Alpha4, Is.EqualTo("AIDJ"));
        Assert.That(second[0].SuccessorIdentifiers[0], Is.Not.EqualTo("changed"));
        Assert.That(second[0].Changes[0].SuccessorIdentifiers[0], Is.Not.EqualTo("changed"));
    }

    [Test]
    public void EveryAvailableFormerCodeAndNameResolvesItsHistoricalRecord()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();
        HistoricalCountry[] countries = catalog.GetHistoricalCountries();

        foreach (HistoricalCountry country in countries)
        {
            AssertContains(
                catalog.FindHistoricalCountryByFormerCode(country.Alpha2!),
                country.Alpha4);
            AssertContains(
                catalog.FindHistoricalCountryByFormerCode(country.Alpha3!),
                country.Alpha4);
            AssertContains(
                catalog.FindHistoricalCountryByFormerCode(country.Alpha4.ToLowerInvariant()),
                country.Alpha4);
            if (country.HasNumericValue)
            {
                AssertContains(
                    catalog.FindHistoricalCountryByFormerCode(country.NumericText!),
                    country.Alpha4);
            }

            AssertContains(
                catalog.FindHistoricalCountryByName(country.Name),
                country.Alpha4);
        }
    }

    [Test]
    public void FormerCodeLookupReturnsAllAmbiguousRecordsInAlpha4Order()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        HistoricalLookupResult alpha2 =
            catalog.FindHistoricalCountryByFormerCode("cs");
        AssertHistoricalCandidates(alpha2, new[] { "CSHH", "CSXX" });

        HistoricalLookupResult numeric =
            catalog.FindHistoricalCountryByFormerCode("891");
        AssertHistoricalCandidates(numeric, new[] { "CSXX", "YUCS" });
        numeric.HistoricalCountryCandidates[0].Alpha4 = "changed";

        HistoricalLookupResult repeated =
            catalog.FindHistoricalCountryByFormerCode("891");
        AssertHistoricalCandidates(repeated, new[] { "CSXX", "YUCS" });
    }

    [Test]
    public void FormerCodeLookupDistinguishesInvalidAndUnknownCodes()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode(null!),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode(string.Empty),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("A"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("A1"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("12A"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("AB1D"),
            LookupStatus.InvalidInput);

        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("ZZ"),
            LookupStatus.NotFound);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("ZZZ"),
            LookupStatus.NotFound);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("000"),
            LookupStatus.NotFound);
        AssertEmpty(
            catalog.FindHistoricalCountryByFormerCode("ZZZZ"),
            LookupStatus.NotFound);
    }

    [Test]
    public void HistoricalNameLookupIsCaseInsensitiveAndDisplayExact()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        AssertContains(
            catalog.FindHistoricalCountryByName("netherlands antilles"),
            "ANHH");
        AssertContains(
            catalog.FindHistoricalCountryByName(
                "Burma, Socialist Republic of the Union of"),
            "BUMM");
        AssertEmpty(
            catalog.FindHistoricalCountryByName(
                "Burma Socialist Republic of the Union of"),
            LookupStatus.NotFound);
        AssertEmpty(
            catalog.FindHistoricalCountryByName("Unknown Former Country"),
            LookupStatus.NotFound);
        AssertEmpty(
            catalog.FindHistoricalCountryByName(null!),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByName(string.Empty),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByName("---"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindHistoricalCountryByName("\uD800"),
            LookupStatus.InvalidInput);
    }

    [Test]
    public void Alpha4ChangeLookupReturnsDetachedTransitionData()
    {
        IHistoricalCountryCatalog catalog = new CountryCodeService();

        HistoricalLookupResult first = catalog.FindCountryChangeByAlpha4("anhh");
        Assert.That(first.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(first.Country, Is.Null);
        Assert.That(first.HistoricalCountry, Is.Null);
        Assert.That(first.CountryChange, Is.Not.Null);
        Assert.That(first.CountryChange!.Alpha4, Is.EqualTo("ANHH"));
        Assert.That(first.CountryChange.Kind, Is.EqualTo(CountryChangeKind.Split));
        Assert.That(first.CountryChange.PredecessorIdentifiers, Is.EqualTo(new[] { "ANHH" }));
        Assert.That(
            first.CountryChange.SuccessorIdentifiers,
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));
        Assert.That(first.CountryCandidates, Is.Empty);
        Assert.That(first.HistoricalCountryCandidates, Is.Empty);
        Assert.That(first.CountryChangeCandidates, Is.Empty);
        first.CountryChange.SuccessorIdentifiers[0] = "changed";

        HistoricalLookupResult second = catalog.FindCountryChangeByAlpha4("ANHH");
        Assert.That(
            second.CountryChange!.SuccessorIdentifiers,
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));

        AssertEmpty(
            catalog.FindCountryChangeByAlpha4(null!),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindCountryChangeByAlpha4("ANH"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindCountryChangeByAlpha4("AN1H"),
            LookupStatus.InvalidInput);
        AssertEmpty(
            catalog.FindCountryChangeByAlpha4("ZZZZ"),
            LookupStatus.NotFound);
    }

    private static void AssertContains(HistoricalLookupResult result, string alpha4)
    {
        if (result.Status == LookupStatus.Found)
        {
            Assert.That(result.HistoricalCountry, Is.Not.Null);
            Assert.That(result.HistoricalCountry!.Alpha4, Is.EqualTo(alpha4));
            Assert.That(result.HistoricalCountryCandidates, Is.Empty);
            return;
        }

        Assert.That(result.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(
            global::System.Array.Exists(
                result.HistoricalCountryCandidates,
                value => value.Alpha4 == alpha4),
            Is.True);
    }

    private static void AssertHistoricalCandidates(
        HistoricalLookupResult result,
        string[] expectedAlpha4)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(result.CountryChange, Is.Null);
        Assert.That(result.CountryCandidates, Is.Empty);
        Assert.That(
            global::System.Array.ConvertAll(
                result.HistoricalCountryCandidates,
                value => value.Alpha4),
            Is.EqualTo(expectedAlpha4));
        Assert.That(result.CountryChangeCandidates, Is.Empty);
    }

    private static void AssertEmpty(HistoricalLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.HistoricalCountry, Is.Null);
        Assert.That(result.CountryChange, Is.Null);
        Assert.That(result.CountryCandidates, Is.Empty);
        Assert.That(result.HistoricalCountryCandidates, Is.Empty);
        Assert.That(result.CountryChangeCandidates, Is.Empty);
    }
}
