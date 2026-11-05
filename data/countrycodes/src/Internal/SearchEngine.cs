using System.Collections.Generic;

namespace countrycodes;

internal class CountrySearchCandidate
{
    internal GeneratedCountryRow Row;
    internal SearchMatchKind Kind;
    internal string SourceText;
    internal string NormalizedKey;
    internal int Score;

    internal CountrySearchCandidate(
        GeneratedCountryRow row,
        SearchMatchKind kind,
        string sourceText,
        string normalizedKey,
        int score)
    {
        Row = row;
        Kind = kind;
        SourceText = sourceText;
        NormalizedKey = normalizedKey;
        Score = score;
    }
}

internal class SearchEvaluation
{
    internal bool Matched;
    internal SearchMatchKind Kind;
    internal int Score;

    internal SearchEvaluation(
        bool matched,
        SearchMatchKind kind,
        int score)
    {
        Matched = matched;
        Kind = kind;
        Score = score;
    }
}

internal static class SearchEngine
{
    internal static CountrySearchMatch[] SearchCountries(
        string? query,
        SearchOptions? options)
    {
        if (query == null ||
            options == null ||
            !SearchScorer.AreOptionsValid(options))
        {
            return new CountrySearchMatch[0];
        }

        string? queryKey = UnicodeNameUtility.CreateMatchKey(
            query,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (queryKey == null || queryKey.Length == 0)
        {
            return new CountrySearchMatch[0];
        }

        List<CountrySearchCandidate> candidates =
            new List<CountrySearchCandidate>();
        AddCurrentCodeMatches(candidates, query, queryKey, options);
        if (options.IncludeReserved)
        {
            AddReservedCodeMatches(candidates, query, queryKey, options);
        }

        AddCurrentNameMatches(candidates, query, queryKey, options);
        if (options.IncludeHistorical)
        {
            AddHistoricalCodeMatches(candidates, query, queryKey, options);
            AddHistoricalNameMatches(candidates, query, queryKey, options);
        }

        SortCandidates(candidates);
        return CreateResults(candidates, options.MaximumResults);
    }

    internal static void AddCurrentCodeMatches(
        List<CountrySearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options)
    {
        GeneratedCountryRow[] rows = RuntimeCatalog.GetState().Countries;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedCountryRow row = rows[index];
            AddCodeCandidate(candidates, row, query, queryKey, row.Alpha2, options);
            AddCodeCandidate(candidates, row, query, queryKey, row.Alpha3, options);
            AddCodeCandidate(candidates, row, query, queryKey, row.NumericText, options);
            index += 1;
        }
    }

    internal static void AddCodeCandidate(
        List<CountrySearchCandidate> candidates,
        GeneratedCountryRow row,
        string query,
        string queryKey,
        string sourceText,
        SearchOptions options)
    {
        string? sourceKey = UnicodeNameUtility.CreateMatchKey(
            sourceText,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (sourceKey == null)
        {
            return;
        }

        SearchEvaluation evaluation = EvaluateCode(
            query,
            queryKey,
            sourceText,
            sourceKey,
            options.Mode);
        if (evaluation.Matched)
        {
            AddOrReplaceCandidate(
                candidates,
                new CountrySearchCandidate(
                    row,
                    evaluation.Kind,
                    sourceText,
                    sourceKey,
                    evaluation.Score));
        }
    }

    internal static void AddCurrentNameMatches(
        List<CountrySearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options)
    {
        string[] locales = GetRequestedLocales(options);
        GeneratedCountryNameRow[] names = RuntimeCatalog.GetState().CountryNames;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedCountryNameRow name = names[index];
            if (name.TargetKind == GeneratedCountryNameTargetKind.Country &&
                IsCurrentNameIncluded(name, locales))
            {
                GeneratedCountryRow? row =
                    CountryIndex.FindCountryByAlpha2(name.TargetCode);
                if (row != null)
                {
                    AddNameCandidate(
                        candidates,
                        row,
                        query,
                        queryKey,
                        name,
                        options);
                }
            }

            index += 1;
        }
    }

    internal static void AddReservedCodeMatches(
        List<CountrySearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options)
    {
        if (!AllowsReservedCodePolicy(options.ValidationPolicy))
        {
            return;
        }

        GeneratedReservedCodeRow[] rows = RuntimeCatalog.GetState().ReservedCodes;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedReservedCodeRow row = rows[index];
            if (row.AssignmentState != GeneratedCodeAssignmentState.FormerlyAssigned)
            {
                string? sourceKey = UnicodeNameUtility.CreateMatchKey(
                    row.Code,
                    true,
                    options.FoldPunctuation,
                    true,
                    options.FoldDiacritics);
                if (sourceKey != null)
                {
                    SearchEvaluation evaluation = EvaluateHistorical(
                        query,
                        queryKey,
                        row.Code,
                        sourceKey,
                        options.Mode,
                        true);
                    if (evaluation.Matched)
                    {
                        AddHistoricalSuccessors(
                            candidates,
                            row.Code,
                            row.Code,
                            sourceKey,
                            evaluation.Score);
                    }
                }
            }

            index += 1;
        }
    }

    internal static void AddNameCandidate(
        List<CountrySearchCandidate> candidates,
        GeneratedCountryRow row,
        string query,
        string queryKey,
        GeneratedCountryNameRow name,
        SearchOptions options)
    {
        string? sourceKey = UnicodeNameUtility.CreateMatchKey(
            name.DisplayName,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (sourceKey == null || sourceKey.Length == 0)
        {
            return;
        }

        SearchMatchKind exactKind = SearchMatchKind.ExactAlias;
        int exactScore = 925;
        if (name.NameKind == GeneratedCountryNameKind.IsoShort ||
            name.NameKind == GeneratedCountryNameKind.IsoOfficial)
        {
            exactKind = SearchMatchKind.ExactName;
            exactScore = 950;
        }

        SearchMatchKind provenanceKind = SearchMatchKind.Normalized;
        int provenancePenalty = 10;
        if (name.NameKind == GeneratedCountryNameKind.IsoShort)
        {
            provenancePenalty = 0;
        }
        else if (name.NameKind == GeneratedCountryNameKind.IsoOfficial)
        {
            provenancePenalty = 5;
        }

        if (name.NameKind == GeneratedCountryNameKind.Localized)
        {
            provenanceKind = SearchMatchKind.Localized;
            exactScore = 900;
            provenancePenalty = 25;
        }

        SearchEvaluation evaluation = EvaluateName(
            query,
            queryKey,
            name.DisplayName,
            sourceKey,
            options.Mode,
            exactKind,
            exactScore,
            provenanceKind,
            provenancePenalty);
        if (evaluation.Matched)
        {
            AddOrReplaceCandidate(
                candidates,
                new CountrySearchCandidate(
                    row,
                    evaluation.Kind,
                    name.DisplayName,
                    sourceKey,
                    evaluation.Score));
        }
    }

    internal static void AddHistoricalCodeMatches(
        List<CountrySearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options)
    {
        if (!AllowsHistoricalCodePolicy(options.ValidationPolicy))
        {
            return;
        }

        GeneratedHistoricalCountryRow[] rows =
            RuntimeCatalog.GetState().HistoricalCountries;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedHistoricalCountryRow row = rows[index];
            AddHistoricalCodeSource(
                candidates,
                row.Alpha4,
                row.Alpha2,
                query,
                queryKey,
                options);
            AddHistoricalCodeSource(
                candidates,
                row.Alpha4,
                row.Alpha3,
                query,
                queryKey,
                options);
            if (row.HasNumeric)
            {
                AddHistoricalCodeSource(
                    candidates,
                    row.Alpha4,
                    row.NumericText,
                    query,
                    queryKey,
                    options);
            }

            AddHistoricalCodeSource(
                candidates,
                row.Alpha4,
                row.Alpha4,
                query,
                queryKey,
                options);
            index += 1;
        }
    }

    internal static void AddHistoricalCodeSource(
        List<CountrySearchCandidate> candidates,
        string alpha4,
        string sourceText,
        string query,
        string queryKey,
        SearchOptions options)
    {
        string? sourceKey = UnicodeNameUtility.CreateMatchKey(
            sourceText,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (sourceKey == null)
        {
            return;
        }

        SearchEvaluation evaluation = EvaluateHistorical(
            query,
            queryKey,
            sourceText,
            sourceKey,
            options.Mode,
            true);
        if (evaluation.Matched)
        {
            AddHistoricalSuccessors(
                candidates,
                alpha4,
                sourceText,
                sourceKey,
                evaluation.Score);
        }
    }

    internal static void AddHistoricalNameMatches(
        List<CountrySearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options)
    {
        GeneratedCountryNameRow[] names = RuntimeCatalog.GetState().CountryNames;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedCountryNameRow name = names[index];
            if (name.TargetKind == GeneratedCountryNameTargetKind.HistoricalCountry &&
                name.NameKind == GeneratedCountryNameKind.Historical)
            {
                string? sourceKey = UnicodeNameUtility.CreateMatchKey(
                    name.DisplayName,
                    true,
                    options.FoldPunctuation,
                    true,
                    options.FoldDiacritics);
                if (sourceKey != null && sourceKey.Length > 0)
                {
                    SearchEvaluation evaluation = EvaluateHistorical(
                        query,
                        queryKey,
                        name.DisplayName,
                        sourceKey,
                        options.Mode,
                        false);
                    if (evaluation.Matched)
                    {
                        AddHistoricalSuccessors(
                            candidates,
                            name.TargetCode,
                            name.DisplayName,
                            sourceKey,
                            evaluation.Score);
                    }
                }
            }

            index += 1;
        }
    }

    internal static void AddHistoricalSuccessors(
        List<CountrySearchCandidate> candidates,
        string alpha4,
        string sourceText,
        string sourceKey,
        int score)
    {
        HistoricalLookupResult resolved =
            HistoricalLookupEngine.ResolveSuccessors(alpha4);
        if (resolved.Country != null)
        {
            AddHistoricalCountryCandidate(
                candidates,
                resolved.Country,
                sourceText,
                sourceKey,
                score);
        }

        int index = 0;
        while (index < resolved.CountryCandidates.Length)
        {
            AddHistoricalCountryCandidate(
                candidates,
                resolved.CountryCandidates[index],
                sourceText,
                sourceKey,
                score);
            index += 1;
        }
    }

    internal static void AddHistoricalCountryCandidate(
        List<CountrySearchCandidate> candidates,
        Country country,
        string sourceText,
        string sourceKey,
        int score)
    {
        GeneratedCountryRow? row = CountryIndex.FindCountryByAlpha2(country.Alpha2);
        if (row != null)
        {
            AddOrReplaceCandidate(
                candidates,
                new CountrySearchCandidate(
                    row,
                    SearchMatchKind.Historical,
                    sourceText,
                    sourceKey,
                    score));
        }
    }

    internal static SearchEvaluation EvaluateCode(
        string query,
        string queryKey,
        string sourceText,
        string sourceKey,
        SearchMode mode)
    {
        if (global::System.String.Equals(
                query,
                sourceText,
                global::System.StringComparison.OrdinalIgnoreCase))
        {
            return new SearchEvaluation(true, SearchMatchKind.ExactCode, 1000);
        }

        if (AsciiCodeUtility.EqualsOrdinal(queryKey, sourceKey))
        {
            return new SearchEvaluation(true, SearchMatchKind.Normalized, 850);
        }

        return SearchScorer.EvaluateBroadMatch(
            queryKey,
            sourceKey,
            mode,
            720,
            620,
            520,
            420);
    }

    internal static SearchEvaluation EvaluateName(
        string query,
        string queryKey,
        string sourceText,
        string sourceKey,
        SearchMode mode,
        SearchMatchKind exactKind,
        int exactScore,
        SearchMatchKind provenanceKind,
        int provenancePenalty)
    {
        if (global::System.String.Equals(
                query,
                sourceText,
                global::System.StringComparison.OrdinalIgnoreCase))
        {
            if (provenanceKind == SearchMatchKind.Localized)
            {
                return new SearchEvaluation(true, provenanceKind, exactScore);
            }

            return new SearchEvaluation(true, exactKind, exactScore);
        }

        if (AsciiCodeUtility.EqualsOrdinal(queryKey, sourceKey))
        {
            if (provenanceKind == SearchMatchKind.Localized)
            {
                return new SearchEvaluation(true, provenanceKind, 850 - provenancePenalty);
            }

            return new SearchEvaluation(
                true,
                SearchMatchKind.Normalized,
                850 - provenancePenalty);
        }

        SearchEvaluation broad = SearchScorer.EvaluateBroadMatch(
            queryKey,
            sourceKey,
            mode,
            700 - provenancePenalty,
            600 - provenancePenalty,
            500 - provenancePenalty,
            400 - provenancePenalty);
        if (broad.Matched && provenanceKind == SearchMatchKind.Localized)
        {
            broad.Kind = provenanceKind;
        }

        return broad;
    }

    internal static SearchEvaluation EvaluateHistorical(
        string query,
        string queryKey,
        string sourceText,
        string sourceKey,
        SearchMode mode,
        bool code)
    {
        int exactScore = 875;
        if (code)
        {
            exactScore = 900;
        }

        if (global::System.String.Equals(
                query,
                sourceText,
                global::System.StringComparison.OrdinalIgnoreCase))
        {
            return new SearchEvaluation(
                true,
                SearchMatchKind.Historical,
                exactScore);
        }

        if (AsciiCodeUtility.EqualsOrdinal(queryKey, sourceKey))
        {
            return new SearchEvaluation(true, SearchMatchKind.Historical, 800);
        }

        SearchEvaluation broad = SearchScorer.EvaluateBroadMatch(
            queryKey,
            sourceKey,
            mode,
            650,
            550,
            450,
            350);
        if (broad.Matched)
        {
            broad.Kind = SearchMatchKind.Historical;
        }

        return broad;
    }

    internal static string[] GetRequestedLocales(SearchOptions options)
    {
        if (options.Locale == null)
        {
            return new string[0];
        }

        return LocalizedNameCatalogEngine.GetLookupLocales(
            options.Locale,
            options.UseLocaleFallback);
    }

    internal static bool IsCurrentNameIncluded(
        GeneratedCountryNameRow name,
        string[] locales)
    {
        if (name.NameKind != GeneratedCountryNameKind.Localized)
        {
            return name.NameKind == GeneratedCountryNameKind.IsoShort ||
                   name.NameKind == GeneratedCountryNameKind.IsoOfficial ||
                   name.NameKind == GeneratedCountryNameKind.Common ||
                   name.NameKind == GeneratedCountryNameKind.Alias ||
                   name.NameKind == GeneratedCountryNameKind.Deprecated;
        }

        int index = 0;
        while (index < locales.Length)
        {
            if (AsciiCodeUtility.EqualsOrdinal(name.Locale, locales[index]))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool AllowsHistoricalCodePolicy(ValidationPolicy policy)
    {
        return policy == ValidationPolicy.SyntaxOnly ||
               policy == ValidationPolicy.AnyKnownIso ||
               policy == ValidationPolicy.AllowHistorical;
    }

    internal static bool AllowsReservedCodePolicy(ValidationPolicy policy)
    {
        return policy == ValidationPolicy.SyntaxOnly ||
               policy == ValidationPolicy.AnyKnownIso ||
               policy == ValidationPolicy.AllowHistorical;
    }

    internal static void AddOrReplaceCandidate(
        List<CountrySearchCandidate> candidates,
        CountrySearchCandidate value)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(
                    candidates[index].Row.Alpha2,
                    value.Row.Alpha2))
            {
                if (IsCandidateBetter(value, candidates[index]))
                {
                    candidates[index] = value;
                }

                return;
            }

            index += 1;
        }

        candidates.Add(value);
    }

    internal static bool IsCandidateBetter(
        CountrySearchCandidate left,
        CountrySearchCandidate right)
    {
        return SearchScorer.CompareCandidates(
            left.Score,
            left.Kind,
            left.SourceText,
            left.NormalizedKey,
            left.Row.Alpha2,
            right.Score,
            right.Kind,
            right.SourceText,
            right.NormalizedKey,
            right.Row.Alpha2) < 0;
    }

    internal static void SortCandidates(List<CountrySearchCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            CountrySearchCandidate value = candidates[index];
            int position = index;
            while (position > 0 &&
                   ComesBefore(value, candidates[position - 1]))
            {
                candidates[position] = candidates[position - 1];
                position -= 1;
            }

            candidates[position] = value;
            index += 1;
        }
    }

    internal static bool ComesBefore(
        CountrySearchCandidate left,
        CountrySearchCandidate right)
    {
        return IsCandidateBetter(left, right);
    }

    internal static CountrySearchMatch[] CreateResults(
        List<CountrySearchCandidate> candidates,
        int maximumResults)
    {
        int count = SearchScorer.GetResultCount(
            candidates.Count,
            maximumResults);

        CountrySearchMatch[] results = new CountrySearchMatch[count];
        int index = 0;
        while (index < count)
        {
            CountrySearchCandidate candidate = candidates[index];
            results[index] = new CountrySearchMatch(
                CountryMapper.MapCountry(candidate.Row),
                candidate.Kind,
                candidate.SourceText,
                candidate.NormalizedKey,
                candidate.Score);
            index += 1;
        }

        return results;
    }
}
