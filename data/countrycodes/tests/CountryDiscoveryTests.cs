using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryDiscoveryTests
{
    [Test]
    public void AmbiguousSubdivisionNamesReturnEveryDistinctCountry()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        CountrySearchMatch[] matches =
            search.DiscoverCountriesBySubdivision("Florida", options);

        Assert.That(
            matches.Select(match => match.Country.Alpha2),
            Is.EqualTo(new[] { "US", "UY" }));
        Assert.That(
            matches.All(match => match.Kind == SearchMatchKind.SubdivisionDerived),
            Is.True);
        Assert.That(matches.All(match => match.SourceText == "Florida"), Is.True);
        Assert.That(matches.All(match => match.NormalizedKey == "florida"), Is.True);
    }

    [Test]
    public void MultipleAmbiguousSubdivisionsInOneCountryProduceOneCountryMatch()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        SubdivisionSearchMatch[] subdivisions =
            search.SearchSubdivisions("Barishal", options);
        CountrySearchMatch[] countries =
            search.DiscoverCountriesBySubdivision("Barishal", options);

        Assert.That(
            subdivisions.Select(match => match.Subdivision.Code),
            Is.EqualTo(new[] { "BD-06", "BD-A" }));
        Assert.That(countries, Has.Length.EqualTo(1));
        AssertCountryMatch(countries[0], "BD", "Barishal");
    }

    [Test]
    public void CountryContextNarrowsSubdivisionDerivedDiscovery()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        CountrySearchMatch[] matches = search.DiscoverCountriesBySubdivision(
            "Florida United States",
            options);

        Assert.That(matches, Has.Length.EqualTo(1));
        AssertCountryMatch(matches[0], "US", "Florida United States");
    }

    [Test]
    public void DerivedMatchesPreserveSubdivisionRankingData()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Fuzzy);

        SubdivisionSearchMatch subdivision =
            search.SearchSubdivisions("Californa", options)[0];
        CountrySearchMatch country =
            search.DiscoverCountriesBySubdivision("Californa", options)[0];

        Assert.Multiple(() =>
        {
            Assert.That(country.Country.Alpha2, Is.EqualTo(subdivision.Country.Alpha2));
            Assert.That(country.Kind, Is.EqualTo(SearchMatchKind.SubdivisionDerived));
            Assert.That(country.SourceText, Is.EqualTo(subdivision.SourceText));
            Assert.That(country.NormalizedKey, Is.EqualTo(subdivision.NormalizedKey));
            Assert.That(country.Score, Is.EqualTo(subdivision.Score));
        });
    }

    [Test]
    public void HistoricalSubdivisionCodesCanDiscoverTheirOwningCountry()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);
        options.IncludeHistorical = true;
        options.ValidationPolicy = ValidationPolicy.AllowHistorical;

        CountrySearchMatch[] matches =
            search.DiscoverCountriesBySubdivision("CN-11", options);

        Assert.That(matches, Has.Length.EqualTo(1));
        AssertCountryMatch(matches[0], "CN", "CN-11");
    }

    [Test]
    public void ResultLimitIsAppliedAfterCountryDeduplication()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);
        options.MaximumResults = 1;

        CountrySearchMatch[] matches =
            search.DiscoverCountriesBySubdivision("Florida", options);

        Assert.That(matches, Has.Length.EqualTo(1));
        Assert.That(matches[0].Country.Alpha2, Is.EqualTo("US"));
        Assert.That(options.MaximumResults, Is.EqualTo(1));
    }

    [Test]
    public void ResultsAreDeterministicAndDetached()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        CountrySearchMatch[] first =
            search.DiscoverCountriesBySubdivision("Florida", options);
        CountrySearchMatch[] second =
            search.DiscoverCountriesBySubdivision("Florida", options);

        Assert.That(
            second.Select(DescribeMatch),
            Is.EqualTo(first.Select(DescribeMatch)));
        string originalName = second[0].Country.ShortName;
        first[0].Country.ShortName = "changed";
        Assert.That(second[0].Country.ShortName, Is.EqualTo(originalName));
        Assert.That(
            search.DiscoverCountriesBySubdivision("Florida", options)[0].Country.ShortName,
            Is.EqualTo(originalName));
    }

    [Test]
    public void InvalidFrequentInputsReturnEmptyResults()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Fuzzy);

        Assert.That(search.DiscoverCountriesBySubdivision(null!, options), Is.Empty);
        Assert.That(search.DiscoverCountriesBySubdivision("", options), Is.Empty);
        Assert.That(search.DiscoverCountriesBySubdivision("\ud800", options), Is.Empty);
        Assert.That(search.DiscoverCountriesBySubdivision("Florida", null!), Is.Empty);
    }

    private static SearchOptions CreateOptions(SearchMode mode)
    {
        return new SearchOptions(
            mode,
            ValidationPolicy.AssignedOnly,
            null,
            false,
            false,
            false,
            false,
            true,
            true,
            50);
    }

    private static void AssertCountryMatch(
        CountrySearchMatch match,
        string alpha2,
        string sourceText)
    {
        Assert.Multiple(() =>
        {
            Assert.That(match.Country.Alpha2, Is.EqualTo(alpha2));
            Assert.That(match.Kind, Is.EqualTo(SearchMatchKind.SubdivisionDerived));
            Assert.That(match.SourceText, Is.EqualTo(sourceText));
            Assert.That(match.NormalizedKey, Is.Not.Empty);
            Assert.That(match.Score, Is.GreaterThan(0));
        });
    }

    private static string DescribeMatch(CountrySearchMatch match)
    {
        return match.Country.Alpha2 + ":" +
               match.Kind + ":" +
               match.SourceText + ":" +
               match.NormalizedKey + ":" +
               match.Score;
    }
}
