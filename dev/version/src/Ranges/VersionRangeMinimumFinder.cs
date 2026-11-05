namespace version;

internal static class VersionRangeMinimumFinder
{
    internal static VersionRangeMinimumResult Find(VersionRange range, RangeSyntax syntax)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        VersionRangeNormalizedFormatter.ValidateSyntax(syntax);

        if (VersionRangeAlgebra.IsUnsatisfiable(range))
        {
            return VersionRangeMinimumResult.CreateNoValue();
        }

        UniversalVersion? selected = null;
        System.Collections.Generic.List<UniversalVersion> unresolvedInfima =
            new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;
        while (index < range.GetSetCount())
        {
            VersionComparatorSet set = range.GetSet(index);
            if (!VersionRangeAlgebra.IsUnsatisfiable(set))
            {
                VersionRangeMinimumResult setResult = FindSetMinimum(range, set, syntax);
                if (!setResult.hasValue || setResult.version == null)
                {
                    unresolvedInfima.Add(GetSetInfimum(set, syntax));
                }
                else if (selected == null
                    || setResult.version.CompareToVersion(selected, VersionComparisonMode.Precedence) < 0)
                {
                    selected = setResult.version;
                }
            }

            index = index + 1;
        }

        if (selected == null)
        {
            return VersionRangeMinimumResult.CreateNoValue();
        }

        index = 0;
        while (index < unresolvedInfima.Count)
        {
            if (selected.CompareToVersion(unresolvedInfima[index], VersionComparisonMode.Precedence) > 0)
            {
                return VersionRangeMinimumResult.CreateNoValue();
            }

            index = index + 1;
        }

        return VersionRangeMinimumResult.CreateValue(selected);
    }

    internal static UniversalVersion GetSetInfimum(VersionComparatorSet set, RangeSyntax syntax)
    {
        VersionRangeAlgebraBounds bounds = VersionRangeAlgebra.GetBounds(set);
        if (bounds.lowerVersion != null)
        {
            return bounds.lowerVersion;
        }

        return VersionRangeAlgebra.GetMinimumRepresentableVersion(GetDialectForSyntax(syntax));
    }

    internal static VersionRangeMinimumResult FindSetMinimum(
        VersionRange sourceRange,
        VersionComparatorSet set,
        RangeSyntax syntax)
    {
        VersionComparator? equalComparator = VersionRangeAlgebra.GetEqualComparator(set);
        if (equalComparator != null)
        {
            if (SetContainsCandidate(sourceRange, set, equalComparator.GetVersion()))
            {
                return VersionRangeMinimumResult.CreateValue(equalComparator.GetVersion());
            }

            return VersionRangeMinimumResult.CreateNoValue();
        }

        VersionRangeAlgebraBounds bounds = VersionRangeAlgebra.GetBounds(set);
        UniversalVersion? candidate = null;
        if (bounds.lowerVersion == null)
        {
            candidate = VersionRangeAlgebra.GetMinimumRepresentableVersion(GetDialectForSyntax(syntax));
        }
        else if (bounds.lowerInclusive)
        {
            candidate = bounds.lowerVersion;
        }
        else
        {
            candidate = CreateNextVersion(bounds.lowerVersion, syntax);
        }

        int attempts = 0;
        int maximumAttempts = set.GetComparatorCount() + 2;
        while (candidate != null && attempts < maximumAttempts)
        {
            if (SetContainsCandidate(sourceRange, set, candidate))
            {
                return VersionRangeMinimumResult.CreateValue(candidate);
            }

            candidate = CreateNextVersion(candidate, syntax);
            attempts = attempts + 1;
        }

        return VersionRangeMinimumResult.CreateNoValue();
    }

    internal static bool SetContainsCandidate(
        VersionRange sourceRange,
        VersionComparatorSet set,
        UniversalVersion candidate)
    {
        VersionComparatorSet[] sets = new VersionComparatorSet[1];
        sets[0] = set;
        VersionRange single = VersionRange.CreateWithPrereleaseAdmission(
            sets,
            sourceRange.prereleasesAllowedByDefault);
        return single.ContainsVersionWithPrerelease(candidate, sourceRange.prereleasesAllowedByDefault);
    }

    internal static UniversalVersion? CreateNextVersion(UniversalVersion version, RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.NodeSemVer || syntax == RangeSyntax.Cargo)
        {
            return CreateSemanticNextVersion(version, syntax);
        }

        if (syntax == RangeSyntax.NuGet)
        {
            return CreateNuGetNextVersion(version);
        }

        return null;
    }

    internal static UniversalVersion? CreateSemanticNextVersion(
        UniversalVersion version,
        RangeSyntax syntax)
    {
        VersionDialect dialect = GetDialectForSyntax(syntax);
        if (version.IsPrerelease())
        {
            return ParseNextCandidate(
                VersionFormatter.FormatNormalized(version) + ".0",
                dialect);
        }

        string major = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(0));
        string minor = "0";
        string patch = "0";
        if (version.GetNumericComponentCount() > 1)
        {
            minor = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(1));
        }

        if (version.GetNumericComponentCount() > 2)
        {
            patch = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(2));
        }

        string text = major + "." + minor + "." + IncrementNumericText(patch);
        return ParseNextCandidate(text, dialect);
    }

    internal static UniversalVersion? CreateNuGetNextVersion(UniversalVersion version)
    {
        if (version.IsPrerelease())
        {
            return ParseNextCandidate(
                VersionFormatter.FormatNormalized(version) + ".0",
                VersionDialect.NuGet);
        }

        string major = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(0));
        string minor = "0";
        string patch = "0";
        if (version.GetNumericComponentCount() > 1)
        {
            minor = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(1));
        }

        if (version.GetNumericComponentCount() > 2)
        {
            patch = VersionFormatter.NormalizeNumericText(version.GetNumericComponent(2));
        }

        string revision = "0";
        if (version.HasRevisionComponent())
        {
            revision = VersionFormatter.NormalizeNumericText(version.GetRevisionComponent());
        }

        string text = major
            + "."
            + minor
            + "."
            + patch
            + "."
            + IncrementNumericText(revision);
        return ParseNextCandidate(text, VersionDialect.NuGet);
    }

    internal static UniversalVersion? ParseNextCandidate(string text, VersionDialect dialect)
    {
        VersionParseResult result = VersionDialectStrategies.Get(dialect).Parse(text);
        if (result.IsFailure())
        {
            return null;
        }

        return result.GetVersion();
    }

    internal static string IncrementNumericText(string text)
    {
        string normalized = VersionFormatter.NormalizeNumericText(text);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int carry = 1;
        int index = normalized.Length - 1;

        while (index >= 0)
        {
            int digit = (normalized[index] - '0') + carry;
            if (digit == 10)
            {
                builder.Insert(0, '0');
            }
            else
            {
                builder.Insert(0, (char)('0' + digit));
                carry = 0;
            }

            index = index - 1;
        }

        if (carry == 1)
        {
            builder.Insert(0, '1');
        }

        return builder.ToString();
    }

    internal static VersionDialect GetDialectForSyntax(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.NodeSemVer)
        {
            return VersionDialect.NodeSemVer;
        }

        if (syntax == RangeSyntax.Cargo)
        {
            return VersionDialect.Cargo;
        }

        if (syntax == RangeSyntax.NuGet)
        {
            return VersionDialect.NuGet;
        }

        if (syntax == RangeSyntax.Maven)
        {
            return VersionDialect.Maven;
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return VersionDialect.RubyGems;
        }

        return VersionDialect.Python;
    }
}
