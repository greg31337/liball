namespace version;

internal static class GoVersionComparer
{
    internal static string Canonicalize(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return Canonicalize(result.GetVersion());
    }

    internal static string Canonicalize(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return version.ToCanonicalString();
    }

    internal static string GetMajor(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return GetMajor(result.GetVersion());
    }

    internal static string GetMajor(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return "v" + VersionFormatter.NormalizeNumericText(version.GetNumericComponent(0));
    }

    internal static string GetMajorMinor(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return GetMajorMinor(result.GetVersion());
    }

    internal static string GetMajorMinor(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return "v"
            + VersionFormatter.NormalizeNumericText(version.GetNumericComponent(0))
            + "."
            + VersionFormatter.NormalizeNumericText(version.GetNumericComponent(1));
    }

    internal static string GetPrerelease(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return GetPrerelease(result.GetVersion());
    }

    internal static string GetPrerelease(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (!version.IsPrerelease())
        {
            return "";
        }

        return "-" + JoinIdentifierTexts(version.GetPrereleaseIdentifiers());
    }

    internal static string GetBuild(string text)
    {
        VersionParseResult result = GoVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return "";
        }

        return GetBuild(result.GetVersion());
    }

    internal static string GetBuild(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (!version.HasBuildMetadata())
        {
            return "";
        }

        return "+" + JoinIdentifierTexts(version.GetBuildIdentifiers());
    }

    internal static int Compare(string leftText, string rightText)
    {
        VersionParseResult leftResult = GoVersionParser.Parse(leftText);
        VersionParseResult rightResult = GoVersionParser.Parse(rightText);

        if (leftResult.IsFailure() && rightResult.IsFailure())
        {
            return 0;
        }

        if (leftResult.IsFailure())
        {
            return -1;
        }

        if (rightResult.IsFailure())
        {
            return 1;
        }

        return Compare(leftResult.GetVersion(), rightResult.GetVersion());
    }

    internal static int Compare(UniversalVersion left, UniversalVersion right)
    {
        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    internal static string[] SortAscending(string[] versions)
    {
        System.ArgumentNullException.ThrowIfNull(versions);

        string[] sorted = CopyStrings(versions);
        int outerIndex = 1;

        while (outerIndex < sorted.Length)
        {
            string value = sorted[outerIndex];
            int innerIndex = outerIndex - 1;

            while (innerIndex >= 0 && CompareForSort(sorted[innerIndex], value) > 0)
            {
                sorted[innerIndex + 1] = sorted[innerIndex];
                innerIndex = innerIndex - 1;
            }

            sorted[innerIndex + 1] = value;
            outerIndex = outerIndex + 1;
        }

        return sorted;
    }

    internal static string[] SortDescending(string[] versions)
    {
        string[] sorted = SortAscending(versions);
        int leftIndex = 0;
        int rightIndex = sorted.Length - 1;

        while (leftIndex < rightIndex)
        {
            string value = sorted[leftIndex];
            sorted[leftIndex] = sorted[rightIndex];
            sorted[rightIndex] = value;
            leftIndex = leftIndex + 1;
            rightIndex = rightIndex - 1;
        }

        return sorted;
    }

    internal static int CompareForSort(string leftText, string rightText)
    {
        int comparison = Compare(leftText, rightText);
        if (comparison != 0)
        {
            return comparison;
        }

        return NormalizeComparison(System.String.CompareOrdinal(leftText, rightText));
    }

    internal static string JoinIdentifierTexts(VersionIdentifier[] identifiers)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < identifiers.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(identifiers[index].GetText());
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int NormalizeComparison(int comparison)
    {
        if (comparison < 0)
        {
            return -1;
        }

        if (comparison > 0)
        {
            return 1;
        }

        return 0;
    }
}
