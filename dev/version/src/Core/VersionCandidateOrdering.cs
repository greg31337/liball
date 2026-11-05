namespace version;

internal static class VersionCandidateOrdering
{
    internal static UniversalVersion[] SortVersionsAscending(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        ValidateVersions(candidates, options);
        UniversalVersion[] sorted = CopyVersions(candidates);
        SortVersions(sorted, options, false);
        return sorted;
    }

    internal static UniversalVersion[] SortVersionsDescending(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        ValidateVersions(candidates, options);
        UniversalVersion[] sorted = CopyVersions(candidates);
        SortVersions(sorted, options, true);
        return sorted;
    }

    internal static string[] SortTextsAscending(string[] candidates, VersionComparisonOptions options)
    {
        ValidateTexts(candidates, options);
        string[] sorted = CopyTexts(candidates);
        SortTexts(sorted, options, false);
        return sorted;
    }

    internal static string[] SortTextsDescending(string[] candidates, VersionComparisonOptions options)
    {
        ValidateTexts(candidates, options);
        string[] sorted = CopyTexts(candidates);
        SortTexts(sorted, options, true);
        return sorted;
    }

    internal static UniversalVersion? MinimumVersion(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        ValidateVersions(candidates, options);
        UniversalVersion? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (selected == null || VersionOptionDispatcher.CompareVersions(candidates[index], selected, options) < 0)
            {
                selected = candidates[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static UniversalVersion? MaximumVersion(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        ValidateVersions(candidates, options);
        UniversalVersion? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (selected == null || VersionOptionDispatcher.CompareVersions(candidates[index], selected, options) > 0)
            {
                selected = candidates[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static string? MinimumText(string[] candidates, VersionComparisonOptions options)
    {
        ValidateTexts(candidates, options);
        string? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (selected == null || VersionOptionDispatcher.Compare(candidates[index], selected, options) < 0)
            {
                selected = candidates[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static string? MaximumText(string[] candidates, VersionComparisonOptions options)
    {
        ValidateTexts(candidates, options);
        string? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (selected == null || VersionOptionDispatcher.Compare(candidates[index], selected, options) > 0)
            {
                selected = candidates[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static void SortVersions(
        UniversalVersion[] candidates,
        VersionComparisonOptions options,
        bool descending)
    {
        int outerIndex = 1;

        while (outerIndex < candidates.Length)
        {
            UniversalVersion candidate = candidates[outerIndex];
            int innerIndex = outerIndex - 1;

            while (innerIndex >= 0
                && ShouldMoveVersion(candidates[innerIndex], candidate, options, descending))
            {
                candidates[innerIndex + 1] = candidates[innerIndex];
                innerIndex = innerIndex - 1;
            }

            candidates[innerIndex + 1] = candidate;
            outerIndex = outerIndex + 1;
        }
    }

    internal static void SortTexts(string[] candidates, VersionComparisonOptions options, bool descending)
    {
        int outerIndex = 1;

        while (outerIndex < candidates.Length)
        {
            string candidate = candidates[outerIndex];
            int innerIndex = outerIndex - 1;

            while (innerIndex >= 0 && ShouldMoveText(candidates[innerIndex], candidate, options, descending))
            {
                candidates[innerIndex + 1] = candidates[innerIndex];
                innerIndex = innerIndex - 1;
            }

            candidates[innerIndex + 1] = candidate;
            outerIndex = outerIndex + 1;
        }
    }

    internal static bool ShouldMoveVersion(
        UniversalVersion left,
        UniversalVersion right,
        VersionComparisonOptions options,
        bool descending)
    {
        int comparison = VersionOptionDispatcher.CompareVersions(left, right, options);
        if (descending)
        {
            return comparison < 0;
        }

        return comparison > 0;
    }

    internal static bool ShouldMoveText(
        string left,
        string right,
        VersionComparisonOptions options,
        bool descending)
    {
        int comparison = VersionOptionDispatcher.Compare(left, right, options);
        if (descending)
        {
            return comparison < 0;
        }

        return comparison > 0;
    }

    internal static void ValidateVersions(UniversalVersion[] candidates, VersionComparisonOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(candidates);
        System.ArgumentNullException.ThrowIfNull(options);
        VersionOptionDispatcher.ValidateComparisonOptions(options);

        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentException("Candidate arrays cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateTexts(string[] candidates, VersionComparisonOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(candidates);
        System.ArgumentNullException.ThrowIfNull(options);
        VersionOptionDispatcher.ValidateComparisonOptions(options);

        VersionParseOptions parseOptions = VersionParseOptions.CreateForDialect(options.GetDialect());
        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentException("Candidate arrays cannot contain null values.");
            }

            if (options.GetInvalidVersionOrdering() == InvalidVersionOrdering.Reject
                && VersionOptionDispatcher.Parse(candidates[index], parseOptions).IsFailure())
            {
                throw new System.ArgumentException("Invalid version text.");
            }

            index = index + 1;
        }
    }

    internal static UniversalVersion[] CopyVersions(UniversalVersion[] candidates)
    {
        UniversalVersion[] copy = new UniversalVersion[candidates.Length];
        int index = 0;

        while (index < candidates.Length)
        {
            copy[index] = candidates[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyTexts(string[] candidates)
    {
        string[] copy = new string[candidates.Length];
        int index = 0;

        while (index < candidates.Length)
        {
            copy[index] = candidates[index];
            index = index + 1;
        }

        return copy;
    }
}
