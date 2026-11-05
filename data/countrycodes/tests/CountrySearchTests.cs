using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountrySearchTests
{
    [Test]
    public void ExactCodesCanonicalNamesOfficialNamesAndAliasesHaveDistinctRanks()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.ExactNormalized);

        AssertMatch(
            search.SearchCountries("de", options)[0],
            "DE",
            SearchMatchKind.ExactCode,
            "DE");
        AssertMatch(
            search.SearchCountries("276", options)[0],
            "DE",
            SearchMatchKind.ExactCode,
            "276");
        AssertMatch(
            search.SearchCountries("Germany", options)[0],
            "DE",
            SearchMatchKind.ExactName,
            "Germany");
        AssertMatch(
            search.SearchCountries("Federal Republic of Germany", options)[0],
            "DE",
            SearchMatchKind.ExactName,
            "Federal Republic of Germany");
        AssertMatch(
            search.SearchCountries("Ivory Coast", options)[0],
            "CI",
            SearchMatchKind.ExactAlias,
            "Ivory Coast");
    }

    [Test]
    public void NormalizedMatchingHonorsDiacriticAndPunctuationOptions()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions folded = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions strict = new SearchOptions(
            SearchMode.ExactNormalized,
            ValidationPolicy.AssignedOnly,
            null,
            false,
            false,
            false,
            false,
            false,
            false,
            50);

        CountrySearchMatch[] matches =
            search.SearchCountries("Cote d Ivoire", folded);

        Assert.That(matches, Is.Not.Empty);
        AssertMatch(matches[0], "CI", SearchMatchKind.Normalized, "Côte d'Ivoire");
        Assert.That(
            search.SearchCountries("Cote d Ivoire", strict),
            Is.Empty);
    }

    [Test]
    public void LocalizedNamesRequireTheRequestedLocaleOrItsExplicitFallback()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions noLocale = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions exactFrench = CreateOptionsWithLocale("fr", false);
        SearchOptions fallbackFrench = CreateOptionsWithLocale("fr-CA", true);
        SearchOptions noFallbackFrench = CreateOptionsWithLocale("fr-CA", false);

        Assert.That(search.SearchCountries("Allemagne", noLocale), Is.Empty);
        AssertMatch(
            search.SearchCountries("Allemagne", exactFrench)[0],
            "DE",
            SearchMatchKind.Localized,
            "Allemagne");
        AssertMatch(
            search.SearchCountries("Allemagne", fallbackFrench)[0],
            "DE",
            SearchMatchKind.Localized,
            "Allemagne");
        Assert.That(
            search.SearchCountries("Allemagne", noFallbackFrench),
            Is.Empty);
    }

    [Test]
    public void SearchModesAddPrefixTokenSubstringAndFuzzyTiers()
    {
        ICountrySearch search = new CountryCodeService();

        Assert.That(
            search.SearchCountries(
                "Czech",
                CreateOptions(SearchMode.ExactNormalized)),
            Is.Empty);
        AssertMatch(
            search.SearchCountries(
                "Czech",
                CreateOptions(SearchMode.Prefix))[0],
            "CZ",
            SearchMatchKind.Prefix,
            "Czechia");

        CountrySearchMatch[] tokens = search.SearchCountries(
            "States",
            CreateOptions(SearchMode.Token));
        Assert.That(tokens, Has.Length.GreaterThanOrEqualTo(2));
        CountrySearchMatch unitedStates =
            tokens.Single(match => match.Country.Alpha2 == "US");
        CountrySearchMatch outlyingIslands =
            tokens.Single(match => match.Country.Alpha2 == "UM");
        AssertMatch(
            unitedStates,
            "US",
            SearchMatchKind.Token,
            "United States");
        Assert.That(outlyingIslands.Kind, Is.EqualTo(SearchMatchKind.Token));

        CountrySearchMatch[] substrings = search.SearchCountries(
            "land",
            CreateOptions(SearchMode.Substring));
        Assert.That(substrings, Is.Not.Empty);
        Assert.That(substrings.All(match => match.NormalizedKey.Contains("land")), Is.True);

        AssertMatch(
            search.SearchCountries(
                "Germnay",
                CreateOptions(SearchMode.Fuzzy))[0],
            "DE",
            SearchMatchKind.Fuzzy,
            "Germany");
    }

    [Test]
    public void HistoricalNamesAndCodesResolveEveryCurrentSuccessor()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions excluded = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions included = CreateHistoricalOptions();

        Assert.That(
            search.SearchCountries("Netherlands Antilles", excluded),
            Is.Empty);

        CountrySearchMatch[] byName =
            search.SearchCountries("Netherlands Antilles", included);
        CountrySearchMatch[] byCode = search.SearchCountries("AN", included);

        Assert.That(
            byName.Select(match => match.Country.Alpha2),
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));
        Assert.That(byName.All(match => match.Kind == SearchMatchKind.Historical), Is.True);
        Assert.That(byName.All(match => match.SourceText == "Netherlands Antilles"), Is.True);
        Assert.That(
            byCode.Select(match => match.Country.Alpha2),
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));
        Assert.That(byCode.All(match => match.Kind == SearchMatchKind.Historical), Is.True);
        Assert.That(byCode.All(match => match.SourceText == "AN"), Is.True);
    }

    [Test]
    public void HistoricalCodeSearchHonorsItsValidationPolicy()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions assignedOnly = new SearchOptions(
            SearchMode.ExactNormalized,
            ValidationPolicy.AssignedOnly,
            null,
            false,
            false,
            true,
            false,
            true,
            true,
            50);

        Assert.That(search.SearchCountries("AN", assignedOnly), Is.Empty);
        Assert.That(search.SearchCountries("AN", CreateHistoricalOptions()), Is.Not.Empty);
    }

    [Test]
    public void ReservedCodesParticipateOnlyWhenRequestedAndResolvable()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions excluded = CreateOptions(SearchMode.ExactNormalized);
        SearchOptions included = new SearchOptions(
            SearchMode.ExactNormalized,
            ValidationPolicy.AnyKnownIso,
            null,
            false,
            true,
            false,
            false,
            true,
            true,
            50);

        Assert.That(search.SearchCountries("YU", excluded), Is.Empty);
        CountrySearchMatch[] matches = search.SearchCountries("YU", included);
        Assert.That(
            matches.Select(match => match.Country.Alpha2),
            Is.EqualTo(new[] { "ME", "RS" }));
        Assert.That(matches.All(match => match.Kind == SearchMatchKind.Historical), Is.True);
    }

    [Test]
    public void ResultsAreDeduplicatedDeterministicLimitedAndDetached()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Substring);
        options.MaximumResults = 5;

        CountrySearchMatch[] first = search.SearchCountries("united", options);
        CountrySearchMatch[] second = search.SearchCountries("united", options);

        Assert.That(first, Has.Length.EqualTo(5));
        Assert.That(
            first.Select(match => match.Country.Alpha2).Distinct().Count(),
            Is.EqualTo(first.Length));
        Assert.That(
            second.Select(DescribeMatch),
            Is.EqualTo(first.Select(DescribeMatch)));

        string originalName = second[0].Country.ShortName;
        first[0].Country.ShortName = "changed";
        Assert.That(second[0].Country.ShortName, Is.EqualTo(originalName));
        Assert.That(
            search.SearchCountries("united", options)[0].Country.ShortName,
            Is.EqualTo(originalName));
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

    private static SearchOptions CreateOptionsWithLocale(
        string locale,
        bool useFallback)
    {
        return new SearchOptions(
            SearchMode.ExactNormalized,
            ValidationPolicy.AssignedOnly,
            locale,
            useFallback,
            false,
            false,
            false,
            true,
            true,
            50);
    }

    private static SearchOptions CreateHistoricalOptions()
    {
        return new SearchOptions(
            SearchMode.ExactNormalized,
            ValidationPolicy.AllowHistorical,
            null,
            false,
            false,
            true,
            false,
            true,
            true,
            50);
    }

    private static void AssertMatch(
        CountrySearchMatch match,
        string alpha2,
        SearchMatchKind kind,
        string sourceText)
    {
        Assert.Multiple(() =>
        {
            Assert.That(match.Country.Alpha2, Is.EqualTo(alpha2));
            Assert.That(match.Kind, Is.EqualTo(kind));
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
