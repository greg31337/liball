using System.Collections.Generic;

namespace countrycodes;

internal static class NormalizationEngine
{
    internal static CodeNormalizationResult NormalizeAlpha2Text(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeAlpha2Characters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeAlpha2Characters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(prepared, canonical))
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Alpha2,
            new string(canonical),
            value);
    }

    internal static CodeNormalizationResult NormalizeAlpha3Text(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeAlpha3Characters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeAlpha3Characters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        char[] canonical = new char[CountryCodeConstants.Alpha3Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha3(prepared, canonical))
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Alpha3,
            new string(canonical),
            value);
    }

    internal static CodeNormalizationResult NormalizeNumericText(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeNumericCharacters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeNumericCharacters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        string? canonical = CreateNumericCanonical(prepared, permissive);
        if (canonical == null)
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(CountryCodeKind.Numeric3, canonical, value);
    }

    internal static CodeNormalizationResult NormalizeNumericValue(int value)
    {
        char[] canonical = new char[CountryCodeConstants.Numeric3Length];
        if (!AsciiCodeUtility.TryFormatNumeric3(value, canonical))
        {
            return CreateInvalidResult();
        }

        return CreateCountrySemanticResult(
            NormalizationStatus.Normalized,
            CountryCodeKind.Numeric3,
            new string(canonical));
    }

    internal static CodeNormalizationResult NormalizeAlpha4Text(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeAlpha4Characters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeAlpha4Characters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        char[] canonical = new char[CountryCodeConstants.Alpha4Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha4(prepared, canonical))
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Alpha4,
            new string(canonical),
            value);
    }

    internal static CodeNormalizationResult NormalizeAnyText(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeAnyCharacters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeAnyCharacters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        if (permissive && prepared.Length > 0 &&
            prepared.Length <= CountryCodeConstants.Numeric3Length &&
            HasOnlyAsciiDigits(prepared))
        {
            string? numericCanonical = CreateNumericCanonical(prepared, true);
            if (numericCanonical != null)
            {
                return CreateCanonicalResult(
                    CountryCodeKind.Numeric3,
                    numericCanonical,
                    value);
            }
        }

        if (prepared.Length == CountryCodeConstants.Alpha2Length)
        {
            char[] alpha2 = new char[CountryCodeConstants.Alpha2Length];
            if (AsciiCodeUtility.TryNormalizeAlpha2(prepared, alpha2))
            {
                return CreateCanonicalResult(
                    CountryCodeKind.Alpha2,
                    new string(alpha2),
                    value);
            }
        }
        else if (prepared.Length == CountryCodeConstants.Alpha3Length)
        {
            char[] numeric = new char[CountryCodeConstants.Numeric3Length];
            if (AsciiCodeUtility.TryNormalizeNumeric3(prepared, numeric))
            {
                return CreateCanonicalResult(
                    CountryCodeKind.Numeric3,
                    new string(numeric),
                    value);
            }

            char[] alpha3 = new char[CountryCodeConstants.Alpha3Length];
            if (AsciiCodeUtility.TryNormalizeAlpha3(prepared, alpha3))
            {
                return CreateCanonicalResult(
                    CountryCodeKind.Alpha3,
                    new string(alpha3),
                    value);
            }
        }
        else if (prepared.Length == CountryCodeConstants.Alpha4Length)
        {
            char[] alpha4 = new char[CountryCodeConstants.Alpha4Length];
            if (AsciiCodeUtility.TryNormalizeAlpha4(prepared, alpha4))
            {
                return CreateCanonicalResult(
                    CountryCodeKind.Alpha4,
                    new string(alpha4),
                    value);
            }
        }

        return CreateInvalidResult();
    }

    internal static CodeNormalizationResult NormalizeSubdivisionText(
        string? value,
        bool permissive)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return NormalizeSubdivisionCharacters(value, permissive);
    }

    internal static CodeNormalizationResult NormalizeSubdivisionCharacters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        global::System.ReadOnlySpan<char> prepared = PrepareCharacters(value, permissive);
        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        char[] canonical = new char[maximumLength];
        if (!AsciiCodeUtility.TryNormalizeSubdivisionCode(
                prepared,
                canonical,
                permissive))
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Subdivision,
            new string(canonical, 0, prepared.Length),
            value);
    }

    internal static CodeNormalizationResult NormalizeLocale(string? value)
    {
        string? canonical = LocaleTagUtility.NormalizeLocaleTag(value);
        if (canonical == null || value == null)
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Unknown,
            canonical,
            value);
    }

    internal static CodeNormalizationResult NormalizeName(
        string? value,
        bool foldDiacritics,
        bool foldPunctuation)
    {
        string? canonical = UnicodeNameUtility.CreateMatchKey(
            value,
            true,
            foldPunctuation,
            true,
            foldDiacritics);
        if (canonical == null || canonical.Length == 0 || value == null)
        {
            return CreateInvalidResult();
        }

        return CreateCanonicalResult(
            CountryCodeKind.Unknown,
            canonical,
            value);
    }

    internal static global::System.ReadOnlySpan<char> PrepareCharacters(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        if (!permissive)
        {
            return value;
        }

        int start = 0;
        while (start < value.Length && char.IsWhiteSpace(value[start]))
        {
            start += 1;
        }

        int end = value.Length;
        while (end > start && char.IsWhiteSpace(value[end - 1]))
        {
            end -= 1;
        }

        return value.Slice(start, end - start);
    }

    internal static string? CreateNumericCanonical(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        if (!permissive)
        {
            char[] exact = new char[CountryCodeConstants.Numeric3Length];
            if (!AsciiCodeUtility.TryNormalizeNumeric3(value, exact))
            {
                return null;
            }

            return new string(exact);
        }

        if (value.Length == 0 ||
            value.Length > CountryCodeConstants.Numeric3Length ||
            !HasOnlyAsciiDigits(value))
        {
            return null;
        }

        char[] canonical = new char[CountryCodeConstants.Numeric3Length];
        int padding = CountryCodeConstants.Numeric3Length - value.Length;
        int index = 0;
        while (index < padding)
        {
            canonical[index] = '0';
            index += 1;
        }

        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            canonical[padding + valueIndex] = value[valueIndex];
            valueIndex += 1;
        }

        return new string(canonical);
    }

    internal static bool HasOnlyAsciiDigits(global::System.ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiDigit(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static CodeNormalizationResult CreateCanonicalResult(
        CountryCodeKind kind,
        string canonical,
        global::System.ReadOnlySpan<char> original)
    {
        NormalizationStatus status = NormalizationStatus.Normalized;
        if (AsciiCodeUtility.EqualsOrdinal(canonical, original))
        {
            status = NormalizationStatus.AlreadyCanonical;
        }

        if (kind == CountryCodeKind.Alpha2 ||
            kind == CountryCodeKind.Alpha3 ||
            kind == CountryCodeKind.Numeric3 ||
            kind == CountryCodeKind.Alpha4)
        {
            return CreateCountrySemanticResult(status, kind, canonical);
        }

        if (kind == CountryCodeKind.Subdivision)
        {
            return CreateSubdivisionSemanticResult(status, canonical);
        }

        return new CodeNormalizationResult(
            status,
            kind,
            false,
            CodeAssignmentStatus.Unassigned,
            canonical,
            new string[0]);
    }

    internal static CodeNormalizationResult CreateCountrySemanticResult(
        NormalizationStatus syntacticStatus,
        CountryCodeKind kind,
        string canonical)
    {
        GeneratedCountryRow? current = AssignmentClassifier.FindCurrent(kind, canonical);
        if (current != null)
        {
            return new CodeNormalizationResult(
                syntacticStatus,
                kind,
                true,
                CountryMapper.MapAssignmentStatus(current.AssignmentState),
                canonical,
                new string[0]);
        }

        HistoricalLookupResult successors =
            HistoricalLookupEngine.ResolveSuccessors(canonical);
        string[] replacements = GetHistoricalReplacements(successors);
        CodeAssignmentStatus assignmentStatus =
            AssignmentClassifier.Classify(kind, canonical);

        NormalizationStatus status = syntacticStatus;
        if (replacements.Length == 1)
        {
            status = NormalizationStatus.Replaced;
        }
        else if (replacements.Length > 1)
        {
            status = NormalizationStatus.Ambiguous;
        }

        return new CodeNormalizationResult(
            status,
            kind,
            true,
            assignmentStatus,
            canonical,
            replacements);
    }

    internal static CodeNormalizationResult CreateSubdivisionSemanticResult(
        NormalizationStatus syntacticStatus,
        string canonical)
    {
        if (SubdivisionIndex.FindSubdivisionByCode(canonical) != null)
        {
            return new CodeNormalizationResult(
                syntacticStatus,
                CountryCodeKind.Subdivision,
                false,
                CodeAssignmentStatus.Unassigned,
                canonical,
                new string[0]);
        }

        GeneratedSubdivisionAliasRow? alias =
            SubdivisionIndex.FindSubdivisionAliasByCode(canonical);
        if (alias == null)
        {
            return new CodeNormalizationResult(
                syntacticStatus,
                CountryCodeKind.Subdivision,
                false,
                CodeAssignmentStatus.Unassigned,
                canonical,
                new string[0]);
        }

        GeneratedSubdivisionReplacementRow[] rows =
            SubdivisionIndex.GetReplacementsForAlias(canonical);
        List<string> replacements = new List<string>();
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index].IsCurrent)
            {
                AddReplacementInOrder(replacements, rows[index].ReplacementCode);
            }

            index += 1;
        }

        NormalizationStatus status = syntacticStatus;
        if (replacements.Count == 1)
        {
            status = NormalizationStatus.Replaced;
        }
        else if (replacements.Count > 1)
        {
            status = NormalizationStatus.Ambiguous;
        }

        return new CodeNormalizationResult(
            status,
            CountryCodeKind.Subdivision,
            false,
            CodeAssignmentStatus.Unassigned,
            canonical,
            replacements.ToArray());
    }

    internal static string[] GetHistoricalReplacements(
        HistoricalLookupResult result)
    {
        List<string> replacements = new List<string>();
        if (result.Status == LookupStatus.Found)
        {
            if (result.Country != null)
            {
                AddReplacementInOrder(replacements, result.Country.Alpha2);
            }
        }
        else if (result.Status == LookupStatus.Ambiguous)
        {
            int index = 0;
            while (index < result.CountryCandidates.Length)
            {
                AddReplacementInOrder(
                    replacements,
                    result.CountryCandidates[index].Alpha2);
                index += 1;
            }
        }

        return replacements.ToArray();
    }

    internal static void AddReplacementInOrder(
        List<string> replacements,
        string value)
    {
        int index = 0;
        while (index < replacements.Count)
        {
            int comparison = AsciiCodeUtility.CompareOrdinal(
                replacements[index],
                value);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                replacements.Insert(index, value);
                return;
            }

            index += 1;
        }

        replacements.Add(value);
    }

    internal static CodeNormalizationResult CreateInvalidResult()
    {
        return new CodeNormalizationResult(
            NormalizationStatus.Invalid,
            CountryCodeKind.Unknown,
            false,
            CodeAssignmentStatus.Unassigned,
            null,
            new string[0]);
    }
}
