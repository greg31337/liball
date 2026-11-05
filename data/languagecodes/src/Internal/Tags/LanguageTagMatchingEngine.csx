namespace languagecodes;

internal static class LanguageTagMatchingEngine
{
    internal static LanguageTagMatchResult LookupBestTag(
        string? range,
        string[] tags)
    {
        string? normalizedRange = NormalizeBasicRange(range);
        if (normalizedRange == null)
        {
            return PublicResultFactory.CopyLanguageTagMatchResult(
                new LanguageTagMatchResult
                {
                    Status = LanguageTagMatchStatus.InvalidRange,
                    MatchedIndex = -1
                });
        }

        if (tags == null)
        {
            return PublicResultFactory.CopyLanguageTagMatchResult(
                new LanguageTagMatchResult
                {
                    Status = LanguageTagMatchStatus.InvalidTag,
                    MatchedIndex = -1,
                    NormalizedRange = normalizedRange
                });
        }

        if (normalizedRange == "*")
        {
            return PublicResultFactory.CopyLanguageTagMatchResult(
                new LanguageTagMatchResult
                {
                    Status = LanguageTagMatchStatus.NoMatch,
                    MatchedIndex = -1,
                    NormalizedRange = normalizedRange
                });
        }

        string?[] normalizedTags = new string?[tags.Length];
        LanguageTag?[] parsedTags = new LanguageTag?[tags.Length];
        int index;
        for (index = 0; index < tags.Length; index++)
        {
            string? candidate = tags[index];
            LanguageValidationResult grammar =
                LanguageTagValidationEngine.ValidateTagWellFormed(candidate);
            if (!grammar.IsWellFormed || !grammar.IsValid || candidate == null)
            {
                continue;
            }

            LanguageTag parsed = LanguageTagParser.ParseTag(candidate);
            normalizedTags[index] = parsed.NormalizedValue == null
                ? SubtagIndexBuilder.NormalizeValue(candidate)
                : SubtagIndexBuilder.NormalizeValue(parsed.NormalizedValue);
            parsedTags[index] = parsed;
        }

        string fallback = normalizedRange;
        while (fallback.Length > 0)
        {
            for (index = 0; index < normalizedTags.Length; index++)
            {
                if (normalizedTags[index] == fallback)
                {
                    return PublicResultFactory.CopyLanguageTagMatchResult(
                        new LanguageTagMatchResult
                        {
                            Status = LanguageTagMatchStatus.Matched,
                            MatchedTag = tags[index],
                            MatchedIndex = index,
                            NormalizedRange = normalizedRange,
                            ParsedTag = parsedTags[index]
                        });
                }
            }

            fallback = TruncateLookupRange(fallback);
        }

        return PublicResultFactory.CopyLanguageTagMatchResult(
            new LanguageTagMatchResult
            {
                Status = LanguageTagMatchStatus.NoMatch,
                MatchedIndex = -1,
                NormalizedRange = normalizedRange
            });
    }

    internal static LanguageTagMatchResult[] FilterTags(
        string? range,
        string[] tags)
    {
        string? normalizedRange = NormalizeBasicRange(range);
        if (normalizedRange == null)
        {
            return PublicResultFactory.CopyLanguageTagMatchResults(
                new LanguageTagMatchResult[]
                {
                    new LanguageTagMatchResult
                    {
                        Status = LanguageTagMatchStatus.InvalidRange,
                        MatchedIndex = -1
                    }
                });
        }

        if (tags == null)
        {
            return PublicResultFactory.CopyLanguageTagMatchResults(
                new LanguageTagMatchResult[]
                {
                    new LanguageTagMatchResult
                    {
                        Status = LanguageTagMatchStatus.InvalidTag,
                        MatchedIndex = -1,
                        NormalizedRange = normalizedRange
                    }
                });
        }

        global::System.Collections.Generic.List<LanguageTagMatchResult> results =
            new global::System.Collections.Generic.List<LanguageTagMatchResult>();
        int index;
        for (index = 0; index < tags.Length; index++)
        {
            string? candidate = tags[index];
            LanguageValidationResult grammar =
                LanguageTagValidationEngine.ValidateTagWellFormed(candidate);
            if (!grammar.IsWellFormed || !grammar.IsValid || candidate == null)
            {
                results.Add(new LanguageTagMatchResult
                {
                    Status = LanguageTagMatchStatus.InvalidTag,
                    MatchedTag = candidate,
                    MatchedIndex = index,
                    NormalizedRange = normalizedRange
                });
                continue;
            }

            LanguageTag parsed = LanguageTagParser.ParseTag(candidate);
            string normalizedTag = parsed.NormalizedValue == null
                ? SubtagIndexBuilder.NormalizeValue(candidate)
                : SubtagIndexBuilder.NormalizeValue(parsed.NormalizedValue);
            if (MatchesBasicRange(normalizedRange, normalizedTag))
            {
                results.Add(new LanguageTagMatchResult
                {
                    Status = LanguageTagMatchStatus.Matched,
                    MatchedTag = candidate,
                    MatchedIndex = index,
                    NormalizedRange = normalizedRange,
                    ParsedTag = parsed
                });
            }
        }

        return PublicResultFactory.CopyLanguageTagMatchResults(results.ToArray());
    }

    internal static string? NormalizeBasicRange(string? range)
    {
        if (range == null || range.Length == 0)
        {
            return null;
        }

        if (range == "*")
        {
            return range;
        }

        string[] subtags = range.Split('-');
        if (!LanguageTagParser.IsAlphabeticSubtag(subtags[0], 1, 8))
        {
            return null;
        }

        int index;
        for (index = 1; index < subtags.Length; index++)
        {
            if (!LanguageTagParser.IsAlphanumericSubtag(subtags[index], 1, 8))
            {
                return null;
            }
        }

        return SubtagIndexBuilder.NormalizeValue(range);
    }

    internal static bool MatchesBasicRange(string range, string tag)
    {
        if (range == "*")
        {
            return true;
        }

        if (tag == range)
        {
            return true;
        }

        return tag.Length > range.Length &&
            tag[range.Length] == '-' &&
            tag.StartsWith(range, global::System.StringComparison.Ordinal);
    }

    internal static string TruncateLookupRange(string range)
    {
        int separator = range.LastIndexOf('-');
        if (separator < 0)
        {
            return string.Empty;
        }

        string truncated = range.Substring(0, separator);
        separator = truncated.LastIndexOf('-');
        int lastSubtagStart = separator + 1;
        if (truncated.Length - lastSubtagStart == 1)
        {
            if (separator < 0)
            {
                return string.Empty;
            }

            truncated = truncated.Substring(0, separator);
        }

        return truncated;
    }
}
