using System.Collections.Generic;

namespace countrycodes;

internal static class SubdivisionCountryDiscoveryEngine
{
    internal static CountrySearchMatch[] Discover(
        string? query,
        SearchOptions? options)
    {
        if (query == null ||
            options == null ||
            !SearchScorer.AreOptionsValid(options))
        {
            return new CountrySearchMatch[0];
        }

        SearchOptions completeOptions = CopyWithoutResultLimit(options);
        SubdivisionSearchMatch[] subdivisionMatches =
            SubdivisionSearchEngine.SearchGlobal(query, completeOptions);
        List<CountrySearchMatch> countryMatches =
            new List<CountrySearchMatch>();
        int index = 0;
        while (index < subdivisionMatches.Length)
        {
            SubdivisionSearchMatch subdivisionMatch = subdivisionMatches[index];
            if (!ContainsCountry(countryMatches, subdivisionMatch.Country.Alpha2))
            {
                countryMatches.Add(
                    new CountrySearchMatch(
                        subdivisionMatch.Country,
                        SearchMatchKind.SubdivisionDerived,
                        subdivisionMatch.SourceText,
                        subdivisionMatch.NormalizedKey,
                        subdivisionMatch.Score));
            }

            index += 1;
        }

        return LimitResults(countryMatches, options.MaximumResults);
    }

    internal static SearchOptions CopyWithoutResultLimit(SearchOptions options)
    {
        return new SearchOptions(
            options.Mode,
            options.ValidationPolicy,
            options.Locale,
            options.UseLocaleFallback,
            options.IncludeReserved,
            options.IncludeHistorical,
            options.IncludeUserAssigned,
            options.FoldDiacritics,
            options.FoldPunctuation,
            int.MaxValue);
    }

    internal static bool ContainsCountry(
        List<CountrySearchMatch> matches,
        string alpha2)
    {
        int index = 0;
        while (index < matches.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(matches[index].Country.Alpha2, alpha2))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static CountrySearchMatch[] LimitResults(
        List<CountrySearchMatch> matches,
        int maximumResults)
    {
        int count = SearchScorer.GetResultCount(matches.Count, maximumResults);

        CountrySearchMatch[] results = new CountrySearchMatch[count];
        int index = 0;
        while (index < count)
        {
            CountrySearchMatch match = matches[index];
            results[index] = new CountrySearchMatch(
                match.Country,
                match.Kind,
                match.SourceText,
                match.NormalizedKey,
                match.Score);
            index += 1;
        }

        return results;
    }
}
