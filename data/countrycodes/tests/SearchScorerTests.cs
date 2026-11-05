using System.Globalization;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SearchScorerTests
{
    [Test]
    public void SearchOptionsRequirePositiveLimitsKnownEnumsAndValidLocales()
    {
        SearchOptions options = CreateOptions(SearchMode.Fuzzy, 10);

        Assert.That(SearchScorer.AreOptionsValid(options), Is.True);
        options.Locale = "EN-us";
        Assert.That(SearchScorer.AreOptionsValid(options), Is.True);
        options.Locale = "en--US";
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);

        options.Locale = null;
        options.MaximumResults = 0;
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);
        options.MaximumResults = -1;
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);

        options.MaximumResults = 10;
        options.Mode = (SearchMode)(-1);
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);
        options.Mode = (SearchMode)5;
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);

        options.Mode = SearchMode.Fuzzy;
        options.ValidationPolicy = (ValidationPolicy)5;
        Assert.That(SearchScorer.AreOptionsValid(options), Is.False);
        Assert.That(SearchScorer.AreOptionsValid(null), Is.False);
    }

    [Test]
    public void InvalidOptionsProduceNoResultsForEverySearchOperation()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions invalidMode = CreateOptions((SearchMode)5, 10);
        SearchOptions invalidPolicy = CreateOptions(SearchMode.Fuzzy, 10);
        invalidPolicy.ValidationPolicy = (ValidationPolicy)(-1);
        SearchOptions invalidLocale = CreateOptions(SearchMode.Fuzzy, 10);
        invalidLocale.Locale = "en--US";

        Assert.Multiple(() =>
        {
            Assert.That(search.SearchCountries("Germany", invalidMode), Is.Empty);
            Assert.That(search.SearchSubdivisions("California", invalidPolicy), Is.Empty);
            Assert.That(
                search.SearchSubdivisionsInCountry(
                    "US",
                    "California",
                    invalidLocale),
                Is.Empty);
            Assert.That(
                search.DiscoverCountriesBySubdivision("California", invalidMode),
                Is.Empty);
        });
    }

    [Test]
    public void SignedResultLimitsAreEnforcedAfterRankingAndDeduplication()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions one = CreateOptions(SearchMode.Substring, 1);
        SearchOptions zero = CreateOptions(SearchMode.Substring, 0);
        SearchOptions negative = CreateOptions(SearchMode.Substring, -1);

        Assert.Multiple(() =>
        {
            Assert.That(search.SearchCountries("a", one), Has.Length.EqualTo(1));
            Assert.That(search.SearchSubdivisions("a", one), Has.Length.EqualTo(1));
            Assert.That(
                search.DiscoverCountriesBySubdivision("Florida", one),
                Has.Length.EqualTo(1));
            Assert.That(search.SearchCountries("a", zero), Is.Empty);
            Assert.That(search.SearchSubdivisions("a", negative), Is.Empty);
            Assert.That(
                search.SearchSubdivisionsInCountry("US", "a", zero),
                Is.Empty);
            Assert.That(
                search.DiscoverCountriesBySubdivision("Florida", negative),
                Is.Empty);
        });

        Assert.Multiple(() =>
        {
            Assert.That(SearchScorer.GetResultCount(12, 5), Is.EqualTo(5));
            Assert.That(SearchScorer.GetResultCount(3, 5), Is.EqualTo(3));
            Assert.That(SearchScorer.GetResultCount(3, 0), Is.Zero);
            Assert.That(SearchScorer.GetResultCount(3, -1), Is.Zero);
        });
    }

    [Test]
    public void FuzzyDistanceUsesLengthBasedBoundsAndReturnsABoundedSentinel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SearchScorer.GetMaximumDistance(4), Is.EqualTo(1));
            Assert.That(SearchScorer.GetMaximumDistance(5), Is.EqualTo(2));
            Assert.That(SearchScorer.GetMaximumDistance(8), Is.EqualTo(2));
            Assert.That(SearchScorer.GetMaximumDistance(9), Is.EqualTo(3));
            Assert.That(
                SearchScorer.CalculateDistance("germany", "germnay", 2),
                Is.EqualTo(2));
            Assert.That(
                SearchScorer.CalculateDistance("abcd", "abxy", 1),
                Is.EqualTo(2));
            Assert.That(
                SearchScorer.CalculateDistance("abcdefgh", "abcdwxyz", 99),
                Is.EqualTo(4));
            Assert.That(
                SearchScorer.CalculateDistance("same", "same", -1),
                Is.Zero);
        });
    }

    [Test]
    public void MatchTiersHaveStableIntegerScoresAndDistancePenalty()
    {
        SearchEvaluation prefix = SearchScorer.EvaluateBroadMatch(
            "uni",
            "united states",
            SearchMode.Fuzzy,
            700,
            600,
            500,
            400);
        SearchEvaluation token = SearchScorer.EvaluateBroadMatch(
            "states",
            "united states",
            SearchMode.Fuzzy,
            700,
            600,
            500,
            400);
        SearchEvaluation substring = SearchScorer.EvaluateBroadMatch(
            "nit",
            "united states",
            SearchMode.Fuzzy,
            700,
            600,
            500,
            400);
        SearchEvaluation fuzzy = SearchScorer.EvaluateBroadMatch(
            "germnay",
            "germany",
            SearchMode.Fuzzy,
            700,
            600,
            500,
            400);

        Assert.Multiple(() =>
        {
            AssertEvaluation(prefix, SearchMatchKind.Prefix, 700);
            AssertEvaluation(token, SearchMatchKind.Token, 600);
            AssertEvaluation(substring, SearchMatchKind.Substring, 500);
            AssertEvaluation(fuzzy, SearchMatchKind.Fuzzy, 398);
        });
    }

    [Test]
    public void SearchOrderingAndScoresDoNotDependOnCurrentCulture()
    {
        ICountrySearch search = new CountryCodeService();
        SearchOptions options = CreateOptions(SearchMode.Substring, 25);
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        string[] invariant;
        string[] turkish;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            invariant = DescribeMatches(search.SearchCountries("i", options));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            turkish = DescribeMatches(search.SearchCountries("i", options));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        Assert.That(turkish, Is.EqualTo(invariant));
        Assert.That(turkish, Is.Not.Empty);
    }

    private static SearchOptions CreateOptions(SearchMode mode, int maximumResults)
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
            maximumResults);
    }

    private static void AssertEvaluation(
        SearchEvaluation evaluation,
        SearchMatchKind kind,
        int score)
    {
        Assert.That(evaluation.Matched, Is.True);
        Assert.That(evaluation.Kind, Is.EqualTo(kind));
        Assert.That(evaluation.Score, Is.EqualTo(score));
    }

    private static string[] DescribeMatches(CountrySearchMatch[] matches)
    {
        return matches.Select(
            match => match.Country.Alpha2 + ":" +
                     match.Kind + ":" +
                     match.SourceText + ":" +
                     match.NormalizedKey + ":" +
                     match.Score).ToArray();
    }
}
