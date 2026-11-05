using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SubdivisionSearchTests
{
    [Test]
    public void GlobalSearchMatchesFullCodesAndCanonicalNames()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        AssertMatch(
            search.SearchSubdivisions("us-ca", options)[0],
            "US-CA",
            "US",
            SearchMatchKind.ExactCode,
            "US-CA");
        AssertMatch(
            search.SearchSubdivisions("California", options)[0],
            "US-CA",
            "US",
            SearchMatchKind.ExactName,
            "California");
    }

    [Test]
    public void NormalizedAndLocalizedSubdivisionNamesRetainTheirMatchReason()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions folded = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions localized = CreateOptions(SearchMode.ExactNormalized);
        localized.Locale = "en";

        AssertMatch(
            search.SearchSubdivisions("Sao Paulo", folded)[0],
            "BR-SP",
            "BR",
            SearchMatchKind.Normalized,
            "São Paulo");
        Assert.That(search.SearchSubdivisions("Wales", folded), Is.Empty);
        AssertMatch(
            search.SearchSubdivisions("Wales", localized)[0],
            "GB-WLS",
            "GB",
            SearchMatchKind.Localized,
            "Wales");
    }

    [Test]
    public void GlobalAmbiguityUsesStableSubdivisionCodeOrder()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        SubdivisionSearchMatch[] first = search.SearchSubdivisions("Florida", options);
        SubdivisionSearchMatch[] second = search.SearchSubdivisions("Florida", options);

        Assert.That(
            first.Select(match => match.Subdivision.Code),
            Is.EqualTo(new[] { "US-FL", "UY-FD" }));
        Assert.That(
            second.Select(DescribeMatch),
            Is.EqualTo(first.Select(DescribeMatch)));
        Assert.That(first.All(match => match.Kind == SearchMatchKind.ExactName), Is.True);
    }

    [Test]
    public void CountryConstrainedSearchNormalizesCountryAndAcceptsLocalSuffixes()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        SubdivisionSearchMatch[] unitedStates =
            search.SearchSubdivisionsInCountry("us", "Florida", options);
        SubdivisionSearchMatch[] uruguay =
            search.SearchSubdivisionsInCountry("UY", "Florida", options);

        Assert.That(
            unitedStates.Select(match => match.Subdivision.Code),
            Is.EqualTo(new[] { "US-FL" }));
        Assert.That(
            uruguay.Select(match => match.Subdivision.Code),
            Is.EqualTo(new[] { "UY-FD" }));
        AssertMatch(
            search.SearchSubdivisionsInCountry("US", "CA", options)[0],
            "US-CA",
            "US",
            SearchMatchKind.ExactCode,
            "CA");
        Assert.That(
            search.SearchSubdivisionsInCountry("CA", "Florida", options),
            Is.Empty);
        Assert.That(
            search.SearchSubdivisionsInCountry("ZZ", "Florida", options),
            Is.Empty);
        Assert.That(
            search.SearchSubdivisionsInCountry("U1", "Florida", options),
            Is.Empty);
    }

    [Test]
    public void SearchModesAddPrefixTokenSubstringAndFuzzyMatches()
    {
        ICountrySearch search = new CountryCodeService();

        Assert.That(
            search.SearchSubdivisions(
                "Calif",
                CreateOptions(SearchMode.ExactNormalized)),
            Is.Empty);
        AssertMatch(
            search.SearchSubdivisions(
                "Calif",
                CreateOptions(SearchMode.Prefix))[0],
            "US-CA",
            "US",
            SearchMatchKind.Prefix,
            "California");

        SubdivisionSearchMatch token = search.SearchSubdivisions(
            "Dagenham",
            CreateOptions(SearchMode.Token)).Single(
                match => match.Subdivision.Code == "GB-BDG");
        AssertMatch(
            token,
            "GB-BDG",
            "GB",
            SearchMatchKind.Token,
            "Barking and Dagenham");

        SubdivisionSearchMatch substring = search.SearchSubdivisions(
            "forn",
            CreateOptions(SearchMode.Substring)).First(
                match => match.Subdivision.Code == "US-CA");
        Assert.That(substring.Kind, Is.EqualTo(SearchMatchKind.Substring));

        AssertMatch(
            search.SearchSubdivisions(
                "Californa",
                CreateOptions(SearchMode.Fuzzy))[0],
            "US-CA",
            "US",
            SearchMatchKind.Fuzzy,
            "California");
    }

    [Test]
    public void CountryAndParentContextDisambiguateCompositeQueries()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        SubdivisionSearchMatch[] countryContext =
            search.SearchSubdivisions("Florida United States", options);
        Assert.That(countryContext, Has.Length.EqualTo(1));
        AssertMatch(
            countryContext[0],
            "US-FL",
            "US",
            SearchMatchKind.ExactName,
            "Florida United States");

        SubdivisionSearchMatch[] parentContext = search.SearchSubdivisions(
            "Barking and Dagenham England",
            options);
        Assert.That(parentContext, Has.Length.EqualTo(1));
        AssertMatch(
            parentContext[0],
            "GB-BDG",
            "GB",
            SearchMatchKind.ExactName,
            "Barking and Dagenham England");

        Assert.That(
            search.SearchSubdivisions("United States", options),
            Is.Empty);
        Assert.That(
            search.SearchSubdivisions("England", options)
                .Count(match => match.Subdivision.Code == "GB-BDG"),
            Is.Zero);
        Assert.That(
            search.SearchSubdivisions(
                    "England",
                    CreateOptions(SearchMode.Substring))
                .Count(match => match.Subdivision.Code == "GB-BDG"),
            Is.Zero);
    }

    [Test]
    public void DeprecatedAliasCodesRequireHistoricalParticipation()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions excluded = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions disallowed = CreateOptions(SearchMode.ExactNormalized);
        disallowed.IncludeHistorical = true;
        SearchOptions included = CreateOptions(SearchMode.ExactNormalized);
        included.IncludeHistorical = true;
        included.ValidationPolicy = ValidationPolicy.AllowHistorical;

        Assert.That(search.SearchSubdivisions("CN-11", excluded), Is.Empty);
        Assert.That(search.SearchSubdivisions("CN-11", disallowed), Is.Empty);
        SubdivisionSearchMatch match = search.SearchSubdivisions("CN-11", included)[0];

        AssertMatch(match, "CN-11", "CN", SearchMatchKind.Historical, "CN-11");
        Assert.That(match.Subdivision.Status, Is.EqualTo(SubdivisionStatus.Deprecated));
        Assert.That(match.Subdivision.ReplacementCodes, Is.EqualTo(new[] { "CN-BJ" }));
    }

    [Test]
    public void ResultsAreLimitedDeduplicatedAndDetached()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Prefix);
        options.MaximumResults = 4;

        SubdivisionSearchMatch[] first = search.SearchSubdivisions("A", options);
        SubdivisionSearchMatch[] second = search.SearchSubdivisions("A", options);

        Assert.That(first, Has.Length.EqualTo(4));
        Assert.That(
            first.Select(match => match.Subdivision.Code).Distinct().Count(),
            Is.EqualTo(first.Length));
        Assert.That(
            second.Select(DescribeMatch),
            Is.EqualTo(first.Select(DescribeMatch)));

        string originalSubdivisionName = second[0].Subdivision.Name;
        string originalCountryName = second[0].Country.ShortName;
        first[0].Subdivision.Name = "changed";
        first[0].Country.ShortName = "changed";
        Assert.That(second[0].Subdivision.Name, Is.EqualTo(originalSubdivisionName));
        Assert.That(second[0].Country.ShortName, Is.EqualTo(originalCountryName));
    }

    [Test]
    public void InvalidFrequentSearchInputsReturnEmptyResults()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Fuzzy);

        Assert.That(search.SearchSubdivisions(null!, options), Is.Empty);
        Assert.That(search.SearchSubdivisions("", options), Is.Empty);
        Assert.That(search.SearchSubdivisions("\ud800", options), Is.Empty);
        Assert.That(search.SearchSubdivisions("California", null!), Is.Empty);
        Assert.That(
            search.SearchSubdivisionsInCountry(null!, "California", options),
            Is.Empty);
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

    private static void AssertMatch(
        SubdivisionSearchMatch match,
        string code,
        string countryCode,
        SearchMatchKind kind,
        string sourceText)
    {
        Assert.Multiple(() =>
        {
            Assert.That(match.Subdivision.Code, Is.EqualTo(code));
            Assert.That(match.Country.Alpha2, Is.EqualTo(countryCode));
            Assert.That(match.Kind, Is.EqualTo(kind));
            Assert.That(match.SourceText, Is.EqualTo(sourceText));
            Assert.That(match.NormalizedKey, Is.Not.Empty);
            Assert.That(match.Score, Is.GreaterThan(0));
        });
    }

    private static string DescribeMatch(SubdivisionSearchMatch match)
    {
        return match.Subdivision.Code + ":" +
               match.Country.Alpha2 + ":" +
               match.Kind + ":" +
               match.SourceText + ":" +
               match.NormalizedKey + ":" +
               match.Score;
    }
}
