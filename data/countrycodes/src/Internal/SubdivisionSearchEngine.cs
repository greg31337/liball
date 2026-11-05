using System.Collections.Generic;

namespace countrycodes;

internal class SubdivisionSearchCandidate
{
    internal GeneratedSubdivisionRow? Row;
    internal GeneratedSubdivisionAliasRow? Alias;
    internal GeneratedCountryRow CountryRow;
    internal SearchMatchKind Kind;
    internal string SourceText;
    internal string NormalizedKey;
    internal int Score;

    internal SubdivisionSearchCandidate(
        GeneratedSubdivisionRow? row,
        GeneratedSubdivisionAliasRow? alias,
        GeneratedCountryRow countryRow,
        SearchMatchKind kind,
        string sourceText,
        string normalizedKey,
        int score)
    {
        Row = row;
        Alias = alias;
        CountryRow = countryRow;
        Kind = kind;
        SourceText = sourceText;
        NormalizedKey = normalizedKey;
        Score = score;
    }
}

internal static class SubdivisionSearchEngine
{
    internal static SubdivisionSearchMatch[] SearchGlobal(
        string? query,
        SearchOptions? options)
    {
        return SearchCore(query, options, null, false);
    }

    internal static SubdivisionSearchMatch[] SearchInCountry(
        string? countryAlpha2,
        string? query,
        SearchOptions? options)
    {
        if (countryAlpha2 == null)
        {
            return new SubdivisionSearchMatch[0];
        }

        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(countryAlpha2, canonical))
        {
            return new SubdivisionSearchMatch[0];
        }

        string countryCode = new string(canonical);
        if (CountryIndex.FindCountryByAlpha2(countryCode) == null)
        {
            return new SubdivisionSearchMatch[0];
        }

        return SearchCore(query, options, countryCode, true);
    }

    internal static SubdivisionSearchMatch[] SearchCore(
        string? query,
        SearchOptions? options,
        string? countryCode,
        bool constrainCountry)
    {
        if (query == null ||
            options == null ||
            !SearchScorer.AreOptionsValid(options))
        {
            return new SubdivisionSearchMatch[0];
        }

        string? queryKey = UnicodeNameUtility.CreateMatchKey(
            query,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (queryKey == null || queryKey.Length == 0)
        {
            return new SubdivisionSearchMatch[0];
        }

        List<SubdivisionSearchCandidate> candidates =
            new List<SubdivisionSearchCandidate>();
        AddCurrentCodeMatches(
            candidates,
            query,
            queryKey,
            options,
            countryCode,
            constrainCountry);
        AddCurrentNameMatches(
            candidates,
            query,
            queryKey,
            options,
            countryCode,
            constrainCountry);
        if (options.IncludeHistorical)
        {
            AddAliasCodeMatches(
                candidates,
                query,
                queryKey,
                options,
                countryCode,
                constrainCountry);
        }

        SortCandidates(candidates);
        return CreateResults(candidates, options.MaximumResults);
    }

    internal static void AddCurrentCodeMatches(
        List<SubdivisionSearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options,
        string? countryCode,
        bool constrainCountry)
    {
        GeneratedSubdivisionRow[] rows = RuntimeCatalog.GetState().Subdivisions;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedSubdivisionRow row = rows[index];
            if (!constrainCountry ||
                AsciiCodeUtility.EqualsOrdinal(row.CountryCode, countryCode))
            {
                AddCurrentCodeCandidate(
                    candidates,
                    row,
                    query,
                    queryKey,
                    row.Code,
                    options,
                    0);
                if (constrainCountry)
                {
                    AddCurrentCodeCandidate(
                        candidates,
                        row,
                        query,
                        queryKey,
                        row.Suffix,
                        options,
                        10);
                }
            }

            index += 1;
        }
    }

    internal static void AddCurrentCodeCandidate(
        List<SubdivisionSearchCandidate> candidates,
        GeneratedSubdivisionRow row,
        string query,
        string queryKey,
        string sourceText,
        SearchOptions options,
        int penalty)
    {
        GeneratedCountryRow? country =
            CountryIndex.FindCountryByAlpha2(row.CountryCode);
        string? sourceKey = UnicodeNameUtility.CreateMatchKey(
            sourceText,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (country == null || sourceKey == null)
        {
            return;
        }

        SearchEvaluation evaluation = SearchEngine.EvaluateCode(
            query,
            queryKey,
            sourceText,
            sourceKey,
            options.Mode);
        if (evaluation.Matched)
        {
            AddOrReplaceCandidate(
                candidates,
                new SubdivisionSearchCandidate(
                    row,
                    null,
                    country,
                    evaluation.Kind,
                    sourceText,
                    sourceKey,
                    evaluation.Score - penalty));
        }
    }

    internal static void AddCurrentNameMatches(
        List<SubdivisionSearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options,
        string? countryCode,
        bool constrainCountry)
    {
        string[] locales = SearchEngine.GetRequestedLocales(options);
        GeneratedSubdivisionNameRow[] names =
            RuntimeCatalog.GetState().SubdivisionNames;
        int index = 0;
        while (index < names.Length)
        {
            GeneratedSubdivisionNameRow name = names[index];
            if (name.IsCurrent && IsNameIncluded(name, locales))
            {
                GeneratedSubdivisionRow? row =
                    SubdivisionIndex.FindSubdivisionByCode(name.SubdivisionCode);
                if (row != null &&
                    (!constrainCountry ||
                     AsciiCodeUtility.EqualsOrdinal(row.CountryCode, countryCode)))
                {
                    AddCurrentNameCandidate(
                        candidates,
                        row,
                        name,
                        query,
                        queryKey,
                        options);
                }
            }

            index += 1;
        }
    }

    internal static void AddCurrentNameCandidate(
        List<SubdivisionSearchCandidate> candidates,
        GeneratedSubdivisionRow row,
        GeneratedSubdivisionNameRow name,
        string query,
        string queryKey,
        SearchOptions options)
    {
        GeneratedCountryRow? country =
            CountryIndex.FindCountryByAlpha2(row.CountryCode);
        if (country == null)
        {
            return;
        }

        SearchMatchKind exactKind = SearchMatchKind.ExactName;
        int exactScore = 950;
        SearchMatchKind provenanceKind = SearchMatchKind.Normalized;
        int penalty = 0;
        if (name.NameKind == GeneratedSubdivisionNameKind.Alias)
        {
            exactKind = SearchMatchKind.ExactAlias;
            exactScore = 925;
            penalty = 10;
        }
        else if (name.NameKind == GeneratedSubdivisionNameKind.Localized)
        {
            provenanceKind = SearchMatchKind.Localized;
            exactScore = 900;
            penalty = 25;
        }

        AddEvaluatedNameCandidate(
            candidates,
            row,
            country,
            query,
            queryKey,
            name.DisplayName,
            options,
            exactKind,
            exactScore,
            provenanceKind,
            penalty);
        AddContextCandidates(
            candidates,
            row,
            country,
            query,
            queryKey,
            name.DisplayName,
            options);
    }

    internal static void AddEvaluatedNameCandidate(
        List<SubdivisionSearchCandidate> candidates,
        GeneratedSubdivisionRow row,
        GeneratedCountryRow country,
        string query,
        string queryKey,
        string sourceText,
        SearchOptions options,
        SearchMatchKind exactKind,
        int exactScore,
        SearchMatchKind provenanceKind,
        int penalty)
    {
        string? sourceKey = UnicodeNameUtility.CreateMatchKey(
            sourceText,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (sourceKey == null || sourceKey.Length == 0)
        {
            return;
        }

        SearchEvaluation evaluation = SearchEngine.EvaluateName(
            query,
            queryKey,
            sourceText,
            sourceKey,
            options.Mode,
            exactKind,
            exactScore,
            provenanceKind,
            penalty);
        if (evaluation.Matched)
        {
            AddOrReplaceCandidate(
                candidates,
                new SubdivisionSearchCandidate(
                    row,
                    null,
                    country,
                    evaluation.Kind,
                    sourceText,
                    sourceKey,
                    evaluation.Score));
        }
    }

    internal static void AddContextCandidates(
        List<SubdivisionSearchCandidate> candidates,
        GeneratedSubdivisionRow row,
        GeneratedCountryRow country,
        string query,
        string queryKey,
        string displayName,
        SearchOptions options)
    {
        string? displayKey = UnicodeNameUtility.CreateMatchKey(
            displayName,
            true,
            options.FoldPunctuation,
            true,
            options.FoldDiacritics);
        if (displayKey == null ||
            queryKey.IndexOf(
                displayKey,
                global::System.StringComparison.Ordinal) < 0)
        {
            return;
        }

        string countryName = GetCountryName(country.Alpha2);
        if (countryName.Length > 0)
        {
            AddContextCandidate(
                candidates,
                row,
                country,
                query,
                queryKey,
                displayName + " " + countryName,
                options);
            AddContextCandidate(
                candidates,
                row,
                country,
                query,
                queryKey,
                countryName + " " + displayName,
                options);
        }

        AddContextCandidate(
            candidates,
            row,
            country,
            query,
            queryKey,
            displayName + " " + country.Alpha2,
            options);
        AddContextCandidate(
            candidates,
            row,
            country,
            query,
            queryKey,
            country.Alpha2 + " " + displayName,
            options);

        if (row.HasParent)
        {
            string parentName = GetSubdivisionName(row.ParentCode);
            if (parentName.Length > 0)
            {
                AddContextCandidate(
                    candidates,
                    row,
                    country,
                    query,
                    queryKey,
                    displayName + " " + parentName,
                    options);
                AddContextCandidate(
                    candidates,
                    row,
                    country,
                    query,
                    queryKey,
                    parentName + " " + displayName,
                    options);
                if (countryName.Length > 0)
                {
                    AddContextCandidate(
                        candidates,
                        row,
                        country,
                        query,
                        queryKey,
                        displayName + " " + parentName + " " + countryName,
                        options);
                }
            }
        }
    }

    internal static void AddContextCandidate(
        List<SubdivisionSearchCandidate> candidates,
        GeneratedSubdivisionRow row,
        GeneratedCountryRow country,
        string query,
        string queryKey,
        string sourceText,
        SearchOptions options)
    {
        AddEvaluatedNameCandidate(
            candidates,
            row,
            country,
            query,
            queryKey,
            sourceText,
            options,
            SearchMatchKind.ExactName,
            910,
            SearchMatchKind.Normalized,
            15);
    }

    internal static void AddAliasCodeMatches(
        List<SubdivisionSearchCandidate> candidates,
        string query,
        string queryKey,
        SearchOptions options,
        string? countryCode,
        bool constrainCountry)
    {
        if (!SearchEngine.AllowsHistoricalCodePolicy(options.ValidationPolicy))
        {
            return;
        }

        GeneratedSubdivisionAliasRow[] aliases =
            RuntimeCatalog.GetState().SubdivisionAliases;
        int index = 0;
        while (index < aliases.Length)
        {
            GeneratedSubdivisionAliasRow alias = aliases[index];
            if (!alias.IsCurrentSubdivision &&
                (!constrainCountry ||
                 AsciiCodeUtility.EqualsOrdinal(alias.CountryCode, countryCode)))
            {
                GeneratedCountryRow? country =
                    CountryIndex.FindCountryByAlpha2(alias.CountryCode);
                string? sourceKey = UnicodeNameUtility.CreateMatchKey(
                    alias.Code,
                    true,
                    options.FoldPunctuation,
                    true,
                    options.FoldDiacritics);
                if (country != null && sourceKey != null)
                {
                    SearchEvaluation evaluation = SearchEngine.EvaluateHistorical(
                        query,
                        queryKey,
                        alias.Code,
                        sourceKey,
                        options.Mode,
                        true);
                    if (evaluation.Matched)
                    {
                        AddOrReplaceCandidate(
                            candidates,
                            new SubdivisionSearchCandidate(
                                null,
                                alias,
                                country,
                                SearchMatchKind.Historical,
                                alias.Code,
                                sourceKey,
                                evaluation.Score));
                    }
                }
            }

            index += 1;
        }
    }

    internal static bool IsNameIncluded(
        GeneratedSubdivisionNameRow name,
        string[] locales)
    {
        if (name.NameKind != GeneratedSubdivisionNameKind.Localized)
        {
            return name.NameKind == GeneratedSubdivisionNameKind.Canonical ||
                   name.NameKind == GeneratedSubdivisionNameKind.Alias;
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

    internal static string GetCountryName(string alpha2)
    {
        GeneratedCountryNameRow[] names = NameIndex.GetNamesForCountry(alpha2);
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].NameKind == GeneratedCountryNameKind.IsoShort)
            {
                return names[index].DisplayName;
            }

            index += 1;
        }

        return string.Empty;
    }

    internal static string GetSubdivisionName(string code)
    {
        GeneratedSubdivisionNameRow[] names =
            NameIndex.GetNamesForSubdivision(code);
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].NameKind == GeneratedSubdivisionNameKind.Canonical)
            {
                return names[index].DisplayName;
            }

            index += 1;
        }

        return string.Empty;
    }

    internal static void AddOrReplaceCandidate(
        List<SubdivisionSearchCandidate> candidates,
        SubdivisionSearchCandidate value)
    {
        string valueCode = GetCandidateCode(value);
        int index = 0;
        while (index < candidates.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(
                    GetCandidateCode(candidates[index]),
                    valueCode))
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

    internal static string GetCandidateCode(SubdivisionSearchCandidate candidate)
    {
        if (candidate.Row != null)
        {
            return candidate.Row.Code;
        }

        if (candidate.Alias != null)
        {
            return candidate.Alias.Code;
        }

        return string.Empty;
    }

    internal static bool IsCandidateBetter(
        SubdivisionSearchCandidate left,
        SubdivisionSearchCandidate right)
    {
        return SearchScorer.CompareCandidates(
            left.Score,
            left.Kind,
            left.SourceText,
            left.NormalizedKey,
            GetCandidateCode(left),
            right.Score,
            right.Kind,
            right.SourceText,
            right.NormalizedKey,
            GetCandidateCode(right)) < 0;
    }

    internal static void SortCandidates(
        List<SubdivisionSearchCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            SubdivisionSearchCandidate value = candidates[index];
            int position = index;
            while (position > 0 && ComesBefore(value, candidates[position - 1]))
            {
                candidates[position] = candidates[position - 1];
                position -= 1;
            }

            candidates[position] = value;
            index += 1;
        }
    }

    internal static bool ComesBefore(
        SubdivisionSearchCandidate left,
        SubdivisionSearchCandidate right)
    {
        return IsCandidateBetter(left, right);
    }

    internal static SubdivisionSearchMatch[] CreateResults(
        List<SubdivisionSearchCandidate> candidates,
        int maximumResults)
    {
        int count = SearchScorer.GetResultCount(
            candidates.Count,
            maximumResults);

        SubdivisionSearchMatch[] results = new SubdivisionSearchMatch[count];
        int index = 0;
        while (index < count)
        {
            SubdivisionSearchCandidate candidate = candidates[index];
            Subdivision? subdivision = null;
            if (candidate.Row != null)
            {
                subdivision = SubdivisionMapper.MapSubdivision(candidate.Row);
            }
            else if (candidate.Alias != null)
            {
                subdivision = SubdivisionMapper.MapSubdivisionAlias(candidate.Alias);
            }

            if (subdivision != null)
            {
                results[index] = new SubdivisionSearchMatch(
                    subdivision,
                    CountryMapper.MapCountry(candidate.CountryRow),
                    candidate.Kind,
                    candidate.SourceText,
                    candidate.NormalizedKey,
                    candidate.Score);
            }

            index += 1;
        }

        return results;
    }
}
