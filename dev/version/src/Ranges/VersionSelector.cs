namespace version;

/// <summary>
/// Represents a version selector.
/// </summary>
/// <remarks>
/// Values are exposed through ordinary methods because the library intentionally avoids properties and external dependencies.
/// </remarks>
internal static class VersionSelector
{
    /// <summary>
    /// Filters candidates by range membership and prerelease policy while preserving their input order.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static UniversalVersion[] Filter(VersionRange range, UniversalVersion[] candidates)
    {
        return Filter(range, candidates, VersionSelectionOptions.CreateDefault());
    }

    /// <summary>
    /// Filters candidates by range membership and prerelease policy while preserving their input order.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static UniversalVersion[] Filter(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(candidates);
        System.ArgumentNullException.ThrowIfNull(options);
        VersionSelectionOptions.ValidatePrereleasePolicy(options.GetPrereleasePolicy());
        ValidateCandidates(candidates);

        if (options.GetPrereleasePolicy() == PrereleasePolicy.AutoIfNoStableCandidate)
        {
            return FilterAutoIfNoStableCandidate(range, candidates);
        }

        System.Collections.Generic.List<UniversalVersion> filtered = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;

        while (index < candidates.Length)
        {
            if (Matches(range, candidates[index], options.GetPrereleasePolicy()))
            {
                filtered.Add(candidates[index]);
            }

            index = index + 1;
        }

        return filtered.ToArray();
    }

    /// <summary>
    /// Selects the lowest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? MinSatisfying(VersionRange range, UniversalVersion[] candidates)
    {
        return MinSatisfying(range, candidates, VersionSelectionOptions.CreateDefault());
    }

    /// <summary>
    /// Selects the lowest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? MinSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        UniversalVersion[] filtered = Filter(range, candidates, options);
        UniversalVersion? selected = null;
        int index = 0;

        while (index < filtered.Length)
        {
            if (selected == null || filtered[index].CompareToVersion(selected, VersionComparisonMode.Precedence) < 0)
            {
                selected = filtered[index];
            }

            index = index + 1;
        }

        return selected;
    }

    /// <summary>
    /// Selects the highest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? MaxSatisfying(VersionRange range, UniversalVersion[] candidates)
    {
        return MaxSatisfying(range, candidates, VersionSelectionOptions.CreateDefault());
    }

    /// <summary>
    /// Selects the highest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? MaxSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        UniversalVersion[] filtered = Filter(range, candidates, options);
        return MaxVersion(filtered);
    }

    /// <summary>
    /// Selects the preferred stable candidate, falling back according to the prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? BestSatisfying(VersionRange range, UniversalVersion[] candidates)
    {
        return BestSatisfying(range, candidates, VersionSelectionOptions.CreateDefault());
    }

    /// <summary>
    /// Selects the preferred stable candidate, falling back according to the prerelease policy.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    public static UniversalVersion? BestSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        UniversalVersion[] filtered = Filter(range, candidates, options);
        UniversalVersion? bestStable = MaxStableVersion(filtered);
        if (bestStable != null)
        {
            return bestStable;
        }

        return MaxVersion(filtered);
    }

    internal static UniversalVersion[] FilterAutoIfNoStableCandidate(VersionRange range, UniversalVersion[] candidates)
    {
        System.Collections.Generic.List<UniversalVersion> stableMatches = new System.Collections.Generic.List<UniversalVersion>();
        System.Collections.Generic.List<UniversalVersion> allMatches = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;

        while (index < candidates.Length)
        {
            if (range.ContainsVersionWithPrerelease(candidates[index], true))
            {
                allMatches.Add(candidates[index]);
                if (!candidates[index].IsPrerelease())
                {
                    stableMatches.Add(candidates[index]);
                }
            }

            index = index + 1;
        }

        if (stableMatches.Count > 0)
        {
            return stableMatches.ToArray();
        }

        return allMatches.ToArray();
    }

    internal static bool Matches(VersionRange range, UniversalVersion candidate, PrereleasePolicy policy)
    {
        if (policy == PrereleasePolicy.Include)
        {
            return range.ContainsVersionWithPrerelease(candidate, true);
        }

        if (policy == PrereleasePolicy.Exclude)
        {
            return !candidate.IsPrerelease() && range.ContainsVersionWithPrerelease(candidate, true);
        }

        if (policy == PrereleasePolicy.ExplicitOnly)
        {
            return range.ContainsVersionWithPrerelease(candidate, false);
        }

        return range.ContainsVersionWithPrerelease(candidate, range.AllowsPrereleasesByDefault());
    }

    internal static UniversalVersion? MaxVersion(UniversalVersion[] candidates)
    {
        UniversalVersion? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (selected == null || candidates[index].CompareToVersion(selected, VersionComparisonMode.Precedence) > 0)
            {
                selected = candidates[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static UniversalVersion? MaxStableVersion(UniversalVersion[] candidates)
    {
        UniversalVersion? selected = null;
        int index = 0;

        while (index < candidates.Length)
        {
            if (!candidates[index].IsPrerelease())
            {
                if (selected == null || candidates[index].CompareToVersion(selected, VersionComparisonMode.Precedence) > 0)
                {
                    selected = candidates[index];
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static void ValidateCandidates(UniversalVersion[] candidates)
    {
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
}
