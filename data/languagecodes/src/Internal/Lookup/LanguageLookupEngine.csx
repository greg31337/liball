namespace languagecodes;

internal class NameSearchCandidate
{
    internal NameIndexEntry Name;
    internal int MatchLevel;
    internal bool IsLiteralMatch;

    internal NameSearchCandidate(
        NameIndexEntry name,
        int matchLevel,
        bool isLiteralMatch)
    {
        Name = name;
        MatchLevel = matchLevel;
        IsLiteralMatch = isLiteralMatch;
    }
}

internal static class LanguageLookupEngine
{
    internal static LanguageTagMatchResult LookupBestTag(
        string? range,
        string[] tags)
    {
        return LanguageTagMatchingEngine.LookupBestTag(range, tags);
    }

    internal static LanguageTagMatchResult[] FilterTags(
        string? range,
        string[] tags)
    {
        return LanguageTagMatchingEngine.FilterTags(range, tags);
    }

    internal static LanguageSubtagMetadata? GetLanguageSubtagMetadata(
        LanguageSubtagType type,
        string? value)
    {
        return LanguageTagMetadataEngine.GetLanguageSubtagMetadata(type, value);
    }

    internal static LanguageLookupResult ResolveTagLanguage(LanguageTag tag)
    {
        return LanguageTagResolutionEngine.ResolveTagLanguage(tag);
    }

    internal static LanguageLookupResult LookupCode(
        string? code,
        LanguageCodeForm form,
        LanguageLookupOptions options)
    {
        string? prepared = PrepareCode(code, options);
        if (prepared == null || !IsLexicallyValidForForm(prepared, form))
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = prepared
            };
        }

        CodeIndexEntry[] entries = FindMatchingCodeEntries(prepared, form, options.NormalizeCase);
        return BuildCodeLookupResult(
            prepared,
            entries,
            form,
            options.IncludeRetiredCodes,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry RequireCode(
        string? code,
        LanguageCodeForm form,
        LanguageLookupOptions options)
    {
        return RequireCodeResult(LookupCode(code, form, options), code);
    }

    internal static LanguageLookupResult ResolveValue(
        string? value,
        LanguageLookupOptions options)
    {
        LanguageLookupResult codeResult = LookupPreferredCode(value, options);
        if (codeResult.Status != LanguageLookupStatus.NotFound &&
            codeResult.Status != LanguageLookupStatus.Invalid)
        {
            return PublicResultFactory.CopyLanguageLookupResult(codeResult);
        }

        if (codeResult.Status == LanguageLookupStatus.Invalid &&
            IsPotentialCodeValue(codeResult.NormalizedQuery))
        {
            return PublicResultFactory.CopyLanguageLookupResult(codeResult);
        }

        return ResolveNameValueEntries(
            value,
            options,
            NameIndex.EntriesByNormalizedValue,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry RequireValue(
        string? value,
        LanguageLookupOptions options)
    {
        return RequireValueResult(ResolveValue(value, options), value);
    }

    internal static LanguageLookupResult LookupName(
        string? name,
        LanguageLookupOptions options)
    {
        return LookupNameFiltered(
            name,
            options,
            ReconciledSource.Unknown,
            false,
            LanguageNameKind.Reference,
            false);
    }

    internal static LanguageLookupResult LookupNameFiltered(
        string? name,
        LanguageLookupOptions options,
        ReconciledSource source,
        bool requireSource,
        LanguageNameKind kind,
        bool requireKind)
    {
        string? literal = PrepareNameLiteral(name, options.TrimWhitespace);
        if (literal == null || literal.Length == 0)
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = literal
            };
        }

        string comparison = PrepareNameComparison(
            literal,
            options.NormalizeCase,
            options.NormalizeNames);
        if (comparison.Length == 0)
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = comparison
            };
        }

        return BuildNameLookupResult(
            literal,
            comparison,
            NameIndex.FindExact(comparison),
            options,
            source,
            requireSource,
            kind,
            requireKind,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageLookupResult LookupPreferredCode(
        string? code,
        LanguageLookupOptions options)
    {
        string? prepared = PrepareCode(code, options);
        if (prepared == null ||
            !IsLexicallyValidForForm(prepared, LanguageCodeForm.Unknown))
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = prepared
            };
        }

        CodeIndexEntry[] entries = FindMatchingCodeEntries(
            prepared,
            LanguageCodeForm.Unknown,
            options.NormalizeCase);
        CodeIndexEntry[] preferred = SelectPreferredCodeEntries(
            entries,
            options.IncludeRetiredCodes);
        return BuildCodeLookupResult(
            prepared,
            preferred,
            LanguageCodeForm.Unknown,
            options.IncludeRetiredCodes,
            LanguageCatalogEngine.Data);
    }

    internal static CodeIndexEntry[] SelectPreferredCodeEntries(
        CodeIndexEntry[] entries,
        bool includeRetired)
    {
        int preferredLevel = int.MaxValue;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            int level = FindCodeResolutionLevel(entries[index], includeRetired);
            if (level >= 0 && level < preferredLevel)
            {
                preferredLevel = level;
            }
        }

        if (preferredLevel == int.MaxValue)
        {
            return new CodeIndexEntry[0];
        }

        global::System.Collections.Generic.List<CodeIndexEntry> preferred =
            new global::System.Collections.Generic.List<CodeIndexEntry>();
        for (index = 0; index < entries.Length; index++)
        {
            if (FindCodeResolutionLevel(entries[index], includeRetired) == preferredLevel)
            {
                preferred.Add(entries[index]);
            }
        }

        return preferred.ToArray();
    }

    internal static int FindCodeResolutionLevel(
        CodeIndexEntry entry,
        bool includeRetired)
    {
        if (entry.Form == LanguageCodeForm.Alpha2 ||
            entry.Form == LanguageCodeForm.Set2Terminology ||
            entry.Form == LanguageCodeForm.Set2Bibliographic ||
            entry.Form == LanguageCodeForm.Set3 ||
            entry.Form == LanguageCodeForm.Set5 ||
            entry.Form == LanguageCodeForm.PrivateUse)
        {
            return 0;
        }

        if (entry.Form == LanguageCodeForm.IanaLanguage ||
            entry.Form == LanguageCodeForm.IanaExtlang ||
            entry.Form == LanguageCodeForm.GrandfatheredTag ||
            entry.Form == LanguageCodeForm.RedundantTag)
        {
            return 1;
        }

        if (entry.Form == LanguageCodeForm.RetiredSet3 && includeRetired)
        {
            return 2;
        }

        return -1;
    }

    internal static LanguageLookupResult ResolveNameValueEntries(
        string? value,
        LanguageLookupOptions options,
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> entries,
        LanguageCatalogData catalog)
    {
        string? literal = PrepareNameLiteral(value, options.TrimWhitespace);
        if (literal == null || literal.Length == 0)
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = literal
            };
        }

        LanguageLookupOptions literalOptions = CreateLiteralNameOptions(options);
        LanguageLookupResult literalResult = BuildNameLookupResult(
            literal,
            literal,
            FindIndexedNameEntries(literal, entries),
            literalOptions,
            ReconciledSource.Unknown,
            false,
            LanguageNameKind.Reference,
            false,
            catalog);
        if (literalResult.Status == LanguageLookupStatus.Found ||
            literalResult.Status == LanguageLookupStatus.Ambiguous ||
            (!options.NormalizeCase && !options.NormalizeNames))
        {
            return PublicResultFactory.CopyLanguageLookupResult(literalResult);
        }

        string comparison = PrepareNameComparison(
            literal,
            options.NormalizeCase,
            options.NormalizeNames);
        if (comparison.Length == 0)
        {
            return new LanguageLookupResult
            {
                Status = LanguageLookupStatus.Invalid,
                NormalizedQuery = comparison
            };
        }

        return BuildNameLookupResult(
            literal,
            comparison,
            FindIndexedNameEntries(comparison, entries),
            options,
            ReconciledSource.Unknown,
            false,
            LanguageNameKind.Reference,
            false,
            catalog);
    }

    internal static NameIndexEntry[] FindIndexedNameEntries(
        string value,
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> entries)
    {
        string normalized = UnicodeName.Normalize(value);
        if (normalized.Length == 0 || !entries.ContainsKey(normalized))
        {
            return new NameIndexEntry[0];
        }

        return NameIndexBuilder.CopyNameEntries(entries[normalized]);
    }

    internal static LanguageLookupOptions CreateLiteralNameOptions(
        LanguageLookupOptions options)
    {
        string? nameLanguage = options.NameLanguage;
        if (options.NormalizeCase && nameLanguage != null)
        {
            nameLanguage = nameLanguage.ToLowerInvariant();
        }

        return new LanguageLookupOptions
        {
            TrimWhitespace = options.TrimWhitespace,
            NormalizeCase = false,
            NormalizeNames = false,
            IncludeRetiredCodes = options.IncludeRetiredCodes,
            NameLanguage = nameLanguage,
            SearchMode = options.SearchMode,
            ResultLimit = options.ResultLimit
        };
    }

    internal static bool IsPotentialCodeValue(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return true;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (!AsciiCode.IsAsciiLetter(character) &&
                (character < '0' || character > '9') &&
                character != '-')
            {
                return false;
            }
        }

        return true;
    }

    internal static LanguageCodeEntry RequireCodeResult(
        LanguageLookupResult result,
        string? code)
    {
        if ((result.Status == LanguageLookupStatus.Found ||
                result.Status == LanguageLookupStatus.Retired) &&
            result.Entry != null)
        {
            return PublicResultFactory.CopyLanguageCodeEntry(result.Entry);
        }

        if (result.Status == LanguageLookupStatus.Invalid ||
            result.Status == LanguageLookupStatus.Ambiguous)
        {
            throw new InvalidLanguageCodeException(
                "Language code is invalid or ambiguous: " + FormatRequiredValue(code));
        }

        throw new LanguageCodeNotFoundException(
            "Language code was not found: " + FormatRequiredValue(code));
    }

    internal static LanguageCodeEntry RequireValueResult(
        LanguageLookupResult result,
        string? value)
    {
        if ((result.Status == LanguageLookupStatus.Found ||
                result.Status == LanguageLookupStatus.Retired) &&
            result.Entry != null)
        {
            return PublicResultFactory.CopyLanguageCodeEntry(result.Entry);
        }

        if (result.Status == LanguageLookupStatus.Ambiguous)
        {
            throw new AmbiguousLanguageNameException(
                "Language value is ambiguous: " + FormatRequiredValue(value));
        }

        if (result.Status == LanguageLookupStatus.Invalid)
        {
            throw new InvalidLanguageCodeException(
                "Language value is invalid: " + FormatRequiredValue(value));
        }

        throw new LanguageCodeNotFoundException(
            "Language value was not found: " + FormatRequiredValue(value));
    }

    internal static string FormatRequiredValue(string? value)
    {
        return value == null ? "<null>" : "'" + value + "'";
    }

    internal static LanguageMatch[] SearchNames(
        string? query,
        LanguageLookupOptions options)
    {
        return SearchNameEntries(
            query,
            options,
            NameIndex.EntriesByNormalizedValue,
            NameIndex.SortedNormalizedValues,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageMatch[] SearchNameEntries(
        string? query,
        LanguageLookupOptions options,
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> entries,
        string[] sortedValues,
        LanguageCatalogData catalog)
    {
        if (options.ResultLimit <= 0 || !IsSearchModeValid(options.SearchMode))
        {
            return new LanguageMatch[0];
        }

        string? literal = PrepareNameLiteral(query, options.TrimWhitespace);
        if (literal == null || literal.Length == 0)
        {
            return new LanguageMatch[0];
        }

        string comparison = PrepareNameComparison(
            literal,
            options.NormalizeCase,
            options.NormalizeNames);
        if (comparison.Length == 0)
        {
            return new LanguageMatch[0];
        }

        global::System.Collections.Generic.Dictionary<int, NameSearchCandidate> bestByEntity =
            new global::System.Collections.Generic.Dictionary<int, NameSearchCandidate>();
        int valueIndex;
        for (valueIndex = 0; valueIndex < sortedValues.Length; valueIndex++)
        {
            NameIndexEntry[] sameValue = entries[sortedValues[valueIndex]];
            int entryIndex;
            for (entryIndex = 0; entryIndex < sameValue.Length; entryIndex++)
            {
                NameIndexEntry entry = sameValue[entryIndex];
                if (NameLanguageMatches(
                    entry.NameLanguage,
                    options.NameLanguage,
                    options.NormalizeCase))
                {
                    string candidateValue = PrepareSearchCandidateValue(entry, options);
                    int matchLevel = FindNameSearchMatchLevel(
                        candidateValue,
                        comparison,
                        options.SearchMode);
                    if (matchLevel >= 0 &&
                        entry.EntityIndex >= 0 &&
                        entry.EntityIndex < catalog.EntriesByEntityIndex.Length)
                    {
                        NameSearchCandidate candidate = new NameSearchCandidate(
                            entry,
                            matchLevel,
                            entry.Text == literal);
                        if (!bestByEntity.ContainsKey(entry.EntityIndex) ||
                            CompareNameChoices(candidate, bestByEntity[entry.EntityIndex]) < 0)
                        {
                            bestByEntity[entry.EntityIndex] = candidate;
                        }
                    }
                }
            }
        }

        global::System.Collections.Generic.List<NameSearchCandidate> ranked =
            new global::System.Collections.Generic.List<NameSearchCandidate>();
        foreach (NameSearchCandidate candidate in bestByEntity.Values)
        {
            ranked.Add(candidate);
        }

        ranked.Sort(
            (left, right) => CompareRankedNameCandidates(left, right, catalog));
        int resultCount = ranked.Count;
        if (resultCount > options.ResultLimit)
        {
            resultCount = options.ResultLimit;
        }

        LanguageMatch[] results = new LanguageMatch[resultCount];
        int index;
        for (index = 0; index < resultCount; index++)
        {
            NameSearchCandidate candidate = ranked[index];
            results[index] = new LanguageMatch
            {
                Entry = PublicResultFactory.CopyLanguageCodeEntry(
                    catalog.EntriesByEntityIndex[candidate.Name.EntityIndex]),
                MatchedValue = candidate.Name.Text,
                Kind = candidate.MatchLevel == 0 && candidate.IsLiteralMatch
                    ? LanguageMatchKind.ExactName
                    : LanguageMatchKind.NormalizedName,
                Rank = index
            };
        }

        return PublicResultFactory.CopyLanguageMatches(results);
    }

    internal static string? PrepareCode(string? code, LanguageLookupOptions options)
    {
        if (code == null)
        {
            return null;
        }

        string prepared = code;
        if (options.TrimWhitespace)
        {
            prepared = prepared.Trim();
        }

        if (options.NormalizeCase)
        {
            prepared = SubtagIndexBuilder.NormalizeValue(prepared);
        }

        return prepared;
    }

    internal static CodeIndexEntry[] FindMatchingCodeEntries(
        string prepared,
        LanguageCodeForm form,
        bool normalizeCase)
    {
        CodeIndexEntry[] entries = CodeIndex.FindAll(prepared);
        if (entries.Length == 0 && normalizeCase && prepared.Contains(
            "-",
            global::System.StringComparison.Ordinal))
        {
            IanaSubtagRow? completeTag = SubtagIndex.FindCompleteTag(prepared);
            if (completeTag != null)
            {
                entries = CodeIndex.FindAll(completeTag.Value);
            }
        }

        if (form == LanguageCodeForm.Unknown)
        {
            return entries;
        }

        global::System.Collections.Generic.List<CodeIndexEntry> matching =
            new global::System.Collections.Generic.List<CodeIndexEntry>();
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            if (entries[index].Form == form)
            {
                matching.Add(entries[index]);
            }
        }

        return matching.ToArray();
    }

    internal static LanguageLookupResult BuildCodeLookupResult(
        string prepared,
        CodeIndexEntry[] entries,
        LanguageCodeForm form,
        bool includeRetired,
        LanguageCatalogData catalog)
    {
        global::System.Collections.Generic.List<LanguageMatch> candidates =
            new global::System.Collections.Generic.List<LanguageMatch>();
        global::System.Collections.Generic.HashSet<int> resolvedEntityIndexes =
            new global::System.Collections.Generic.HashSet<int>();
        LanguageCodeEntry? selectedEntry = null;
        LanguageCodeEntry? retiredEntry = null;
        bool foundReserved = false;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            CodeIndexEntry entry = entries[index];
            if (form != LanguageCodeForm.Unknown && entry.Form != form)
            {
                continue;
            }

            if (entry.Form == LanguageCodeForm.RetiredSet3)
            {
                if (includeRetired && catalog.RetiredEntriesByCode.ContainsKey(entry.Value))
                {
                    retiredEntry = PublicResultFactory.CopyLanguageCodeEntry(
                        catalog.RetiredEntriesByCode[entry.Value]);
                    candidates.Add(CreateCodeMatch(entry, retiredEntry, candidates.Count));
                }

                continue;
            }

            if (entry.Form == LanguageCodeForm.PrivateUse)
            {
                foundReserved = true;
                candidates.Add(CreateCodeMatch(entry, null, candidates.Count));
                continue;
            }

            if (entry.EntityIndex >= 0 && entry.EntityIndex < catalog.EntriesByEntityIndex.Length)
            {
                LanguageCodeEntry resolved = PublicResultFactory.CopyLanguageCodeEntry(
                    catalog.EntriesByEntityIndex[entry.EntityIndex]);
                candidates.Add(CreateCodeMatch(entry, resolved, candidates.Count));
                if (!resolvedEntityIndexes.Contains(entry.EntityIndex))
                {
                    resolvedEntityIndexes.Add(entry.EntityIndex);
                    selectedEntry = resolved;
                }
            }
        }

        LanguageLookupStatus status = LanguageLookupStatus.NotFound;
        LanguageCodeEntry? resultEntry = null;
        if (resolvedEntityIndexes.Count > 1)
        {
            status = LanguageLookupStatus.Ambiguous;
        }
        else if (resolvedEntityIndexes.Count == 1)
        {
            status = LanguageLookupStatus.Found;
            resultEntry = selectedEntry;
        }
        else if (retiredEntry != null)
        {
            status = LanguageLookupStatus.Retired;
            resultEntry = retiredEntry;
        }
        else if (foundReserved)
        {
            status = LanguageLookupStatus.Reserved;
        }

        return PublicResultFactory.CopyLanguageLookupResult(
            new LanguageLookupResult
            {
                Status = status,
                Entry = resultEntry,
                Candidates = candidates.ToArray(),
                NormalizedQuery = prepared
            });
    }

    internal static LanguageLookupResult BuildNameLookupResult(
        string literal,
        string comparison,
        NameIndexEntry[] entries,
        LanguageLookupOptions options,
        ReconciledSource source,
        bool requireSource,
        LanguageNameKind kind,
        bool requireKind,
        LanguageCatalogData catalog)
    {
        global::System.Collections.Generic.List<LanguageMatch> candidates =
            new global::System.Collections.Generic.List<LanguageMatch>();
        global::System.Collections.Generic.HashSet<int> resolvedEntityIndexes =
            new global::System.Collections.Generic.HashSet<int>();
        global::System.Collections.Generic.Dictionary<int, int> candidatePositions =
            new global::System.Collections.Generic.Dictionary<int, int>();
        LanguageCodeEntry? selectedEntry = null;
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            NameIndexEntry entry = entries[index];
            if (NameEntryMatches(
                    entry,
                    literal,
                    comparison,
                    options,
                    source,
                    requireSource,
                    kind,
                    requireKind) &&
                entry.EntityIndex >= 0 &&
                entry.EntityIndex < catalog.EntriesByEntityIndex.Length)
            {
                if (!resolvedEntityIndexes.Contains(entry.EntityIndex))
                {
                    resolvedEntityIndexes.Add(entry.EntityIndex);
                    LanguageCodeEntry resolved = PublicResultFactory.CopyLanguageCodeEntry(
                        catalog.EntriesByEntityIndex[entry.EntityIndex]);
                    selectedEntry = resolved;
                    candidatePositions.Add(entry.EntityIndex, candidates.Count);
                    candidates.Add(new LanguageMatch
                    {
                        Entry = PublicResultFactory.CopyLanguageCodeEntry(resolved),
                        MatchedValue = entry.Text,
                        Kind = entry.Text == literal
                            ? LanguageMatchKind.ExactName
                            : LanguageMatchKind.NormalizedName,
                        Rank = candidates.Count
                    });
                }
                else if (entry.Text == literal)
                {
                    LanguageMatch candidate = candidates[candidatePositions[entry.EntityIndex]];
                    candidate.MatchedValue = entry.Text;
                    candidate.Kind = LanguageMatchKind.ExactName;
                }
            }
        }

        LanguageLookupStatus status = LanguageLookupStatus.NotFound;
        LanguageCodeEntry? resultEntry = null;
        if (resolvedEntityIndexes.Count > 1)
        {
            status = LanguageLookupStatus.Ambiguous;
        }
        else if (resolvedEntityIndexes.Count == 1)
        {
            status = LanguageLookupStatus.Found;
            resultEntry = selectedEntry;
        }

        return PublicResultFactory.CopyLanguageLookupResult(
            new LanguageLookupResult
            {
                Status = status,
                Entry = resultEntry,
                Candidates = candidates.ToArray(),
                NormalizedQuery = comparison
            });
    }

    internal static bool NameEntryMatches(
        NameIndexEntry entry,
        string literal,
        string comparison,
        LanguageLookupOptions options,
        ReconciledSource source,
        bool requireSource,
        LanguageNameKind kind,
        bool requireKind)
    {
        bool textMatches = false;
        if (options.NormalizeNames)
        {
            textMatches = entry.NormalizedValue == comparison;
        }
        else if (options.NormalizeCase)
        {
            textMatches = entry.Text.ToUpperInvariant() == comparison;
        }
        else
        {
            textMatches = entry.Text == literal;
        }

        if (!textMatches)
        {
            return false;
        }

        if (!NameLanguageMatches(entry.NameLanguage, options.NameLanguage, options.NormalizeCase))
        {
            return false;
        }

        if (requireSource && entry.Source != source)
        {
            return false;
        }

        if (requireKind && entry.Kind != kind)
        {
            return false;
        }

        return true;
    }

    internal static bool NameLanguageMatches(
        string actual,
        string? requested,
        bool normalizeCase)
    {
        if (requested == null)
        {
            return true;
        }

        if (normalizeCase)
        {
            return actual.ToUpperInvariant() == requested.ToUpperInvariant();
        }

        return actual == requested;
    }

    internal static string? PrepareNameLiteral(string? name, bool trimWhitespace)
    {
        if (name == null)
        {
            return null;
        }

        if (trimWhitespace)
        {
            return name.Trim();
        }

        return name;
    }

    internal static string PrepareNameComparison(
        string literal,
        bool normalizeCase,
        bool normalizeNames)
    {
        if (normalizeNames)
        {
            return UnicodeName.Normalize(literal);
        }

        if (normalizeCase)
        {
            return literal.ToUpperInvariant();
        }

        return literal;
    }

    internal static string PrepareSearchCandidateValue(
        NameIndexEntry entry,
        LanguageLookupOptions options)
    {
        if (options.NormalizeNames)
        {
            return entry.NormalizedValue;
        }

        if (options.NormalizeCase)
        {
            return entry.Text.ToUpperInvariant();
        }

        return entry.Text;
    }

    internal static int FindNameSearchMatchLevel(
        string candidate,
        string query,
        LanguageSearchMode mode)
    {
        if (candidate == query)
        {
            return 0;
        }

        if (mode == LanguageSearchMode.Prefix || mode == LanguageSearchMode.Contains)
        {
            if (candidate.StartsWith(query, global::System.StringComparison.Ordinal))
            {
                return 1;
            }
        }

        if (mode == LanguageSearchMode.Contains &&
            candidate.Contains(query, global::System.StringComparison.Ordinal))
        {
            return 2;
        }

        return -1;
    }

    internal static int CompareNameChoices(
        NameSearchCandidate left,
        NameSearchCandidate right)
    {
        int comparison = left.MatchLevel.CompareTo(right.MatchLevel);
        if (comparison != 0)
        {
            return comparison;
        }

        if (left.IsLiteralMatch != right.IsLiteralMatch)
        {
            return left.IsLiteralMatch ? -1 : 1;
        }

        comparison = ((int)left.Name.Kind).CompareTo((int)right.Name.Kind);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = ((int)left.Name.Source).CompareTo((int)right.Name.Source);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Name.NameLanguage, right.Name.NameLanguage);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Name.Text, right.Name.Text);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.Name.NameIndex.CompareTo(right.Name.NameIndex);
    }

    internal static int CompareRankedNameCandidates(
        NameSearchCandidate left,
        NameSearchCandidate right,
        LanguageCatalogData catalog)
    {
        int comparison = left.MatchLevel.CompareTo(right.MatchLevel);
        if (comparison != 0)
        {
            return comparison;
        }

        if (left.IsLiteralMatch != right.IsLiteralMatch)
        {
            return left.IsLiteralMatch ? -1 : 1;
        }

        comparison = string.CompareOrdinal(left.Name.NormalizedValue, right.Name.NormalizedValue);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(
            LanguageCatalogBuilder.FindStablePublicCode(
                catalog.EntriesByEntityIndex[left.Name.EntityIndex]),
            LanguageCatalogBuilder.FindStablePublicCode(
                catalog.EntriesByEntityIndex[right.Name.EntityIndex]));
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareNameChoices(left, right);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.Name.EntityIndex.CompareTo(right.Name.EntityIndex);
    }

    internal static bool IsSearchModeValid(LanguageSearchMode mode)
    {
        return mode == LanguageSearchMode.Exact ||
            mode == LanguageSearchMode.Prefix ||
            mode == LanguageSearchMode.Contains;
    }

    internal static LanguageMatch CreateCodeMatch(
        CodeIndexEntry source,
        LanguageCodeEntry? entry,
        int rank)
    {
        return new LanguageMatch
        {
            Entry = entry == null ? null : PublicResultFactory.CopyLanguageCodeEntry(entry),
            MatchedValue = source.Value,
            Kind = ConvertMatchKind(source.Form),
            Rank = rank
        };
    }

    internal static LanguageMatchKind ConvertMatchKind(LanguageCodeForm form)
    {
        switch (form)
        {
            case LanguageCodeForm.Alpha2:
                return LanguageMatchKind.Alpha2;
            case LanguageCodeForm.Set2Terminology:
                return LanguageMatchKind.Set2Terminology;
            case LanguageCodeForm.Set2Bibliographic:
                return LanguageMatchKind.Set2Bibliographic;
            case LanguageCodeForm.Set3:
                return LanguageMatchKind.Set3;
            case LanguageCodeForm.Set5:
                return LanguageMatchKind.Set5;
            case LanguageCodeForm.RetiredSet3:
                return LanguageMatchKind.RetiredSet3;
            case LanguageCodeForm.PrivateUse:
                return LanguageMatchKind.PrivateUse;
            case LanguageCodeForm.IanaLanguage:
                return LanguageMatchKind.IanaAlias;
            case LanguageCodeForm.IanaExtlang:
                return LanguageMatchKind.IanaAlias;
            case LanguageCodeForm.GrandfatheredTag:
                return LanguageMatchKind.IanaAlias;
            case LanguageCodeForm.RedundantTag:
                return LanguageMatchKind.IanaAlias;
            default:
                return LanguageMatchKind.None;
        }
    }

    internal static bool IsLexicallyValidForForm(string value, LanguageCodeForm form)
    {
        if (form == LanguageCodeForm.Unknown)
        {
            return IsAsciiLettersBetween(value, 2, 8) || IsCompleteTagShape(value);
        }

        if (form == LanguageCodeForm.Alpha2)
        {
            return IsAsciiLettersBetween(value, 2, 2);
        }

        if (form == LanguageCodeForm.Set2Terminology ||
            form == LanguageCodeForm.Set2Bibliographic ||
            form == LanguageCodeForm.Set3 ||
            form == LanguageCodeForm.Set5 ||
            form == LanguageCodeForm.RetiredSet3 ||
            form == LanguageCodeForm.IanaExtlang ||
            form == LanguageCodeForm.PrivateUse)
        {
            return IsAsciiLettersBetween(value, 3, 3);
        }

        if (form == LanguageCodeForm.IanaLanguage)
        {
            return IsAsciiLettersBetween(value, 2, 8);
        }

        if (form == LanguageCodeForm.GrandfatheredTag ||
            form == LanguageCodeForm.RedundantTag)
        {
            return IsCompleteTagShape(value);
        }

        return false;
    }

    internal static bool IsAsciiLettersBetween(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            if (!AsciiCode.IsAsciiLetter(value[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsCompleteTagShape(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
        {
            return false;
        }

        bool foundSeparator = false;
        bool previousWasSeparator = false;
        int index;
        for (index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character == '-')
            {
                if (previousWasSeparator)
                {
                    return false;
                }

                foundSeparator = true;
                previousWasSeparator = true;
            }
            else
            {
                if (!AsciiCode.IsAsciiLetter(character) && (character < '0' || character > '9'))
                {
                    return false;
                }

                previousWasSeparator = false;
            }
        }

        return foundSeparator;
    }
}
