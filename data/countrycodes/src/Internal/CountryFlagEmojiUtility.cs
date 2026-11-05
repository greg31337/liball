namespace countrycodes;

internal static class CountryFlagEmojiUtility
{
    internal static CountryFlagEmojiResult GetResult(string? value)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        char[] canonicalCharacters =
            new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(
                value,
                canonicalCharacters))
        {
            return CreateInvalidResult();
        }

        string canonical = new string(canonicalCharacters);
        CodeAssignmentStatus status = AssignmentClassifier.Classify(
            CountryCodeKind.Alpha2,
            canonical);
        GeneratedCountryRow? row = CountryIndex.FindCountryByAlpha2(canonical);
        if (row == null || status != CodeAssignmentStatus.Assigned)
        {
            return new CountryFlagEmojiResult(
                false,
                canonical,
                true,
                status,
                null);
        }

        return new CountryFlagEmojiResult(
            true,
            canonical,
            true,
            status,
            CreateRegionalIndicatorPair(canonical));
    }

    internal static string CreateForCountryRow(GeneratedCountryRow row)
    {
        if (row.AssignmentState != GeneratedCodeAssignmentState.Assigned ||
            !AsciiCodeUtility.IsAlpha2(row.Alpha2))
        {
            return string.Empty;
        }

        return CreateRegionalIndicatorPair(row.Alpha2);
    }

    internal static string CreateRegionalIndicatorPair(string canonicalAlpha2)
    {
        if (canonicalAlpha2.Length != CountryCodeConstants.Alpha2Length ||
            canonicalAlpha2[0] < 'A' ||
            canonicalAlpha2[0] > 'Z' ||
            canonicalAlpha2[1] < 'A' ||
            canonicalAlpha2[1] > 'Z')
        {
            return string.Empty;
        }

        int first = 0x1F1E6 + canonicalAlpha2[0] - 'A';
        int second = 0x1F1E6 + canonicalAlpha2[1] - 'A';
        return global::System.Char.ConvertFromUtf32(first) +
               global::System.Char.ConvertFromUtf32(second);
    }

    internal static CountryFlagEmojiResult CreateInvalidResult()
    {
        return new CountryFlagEmojiResult(
            false,
            null,
            false,
            CodeAssignmentStatus.Unassigned,
            null);
    }
}
