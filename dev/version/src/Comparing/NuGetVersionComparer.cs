namespace version;

internal static class NuGetVersionComparer
{
    internal static int Compare(string leftText, string rightText)
    {
        VersionParseResult leftResult = NuGetVersionParser.Parse(leftText);
        VersionParseResult rightResult = NuGetVersionParser.Parse(rightText);

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
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        int numericComparison = CompareNumericComponents(left, right);
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        return ComparePrerelease(left, right);
    }

    internal static int CompareNumericComponents(UniversalVersion left, UniversalVersion right)
    {
        int index = 0;

        while (index < 4)
        {
            string leftComponent = GetNuGetNumericComponent(left, index);
            string rightComponent = GetNuGetNumericComponent(right, index);
            int comparison = CompareNumericText(leftComponent, rightComponent);

            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string GetNuGetNumericComponent(UniversalVersion version, int componentIndex)
    {
        if (componentIndex < 3)
        {
            if (componentIndex < version.GetNumericComponentCount())
            {
                return version.GetNumericComponent(componentIndex);
            }

            return "0";
        }

        if (version.HasRevisionComponent())
        {
            return version.GetRevisionComponent();
        }

        return "0";
    }

    internal static int ComparePrerelease(UniversalVersion left, UniversalVersion right)
    {
        if (!left.IsPrerelease() && !right.IsPrerelease())
        {
            return 0;
        }

        if (!left.IsPrerelease())
        {
            return 1;
        }

        if (!right.IsPrerelease())
        {
            return -1;
        }

        int maxCount = left.GetPrereleaseIdentifierCount();
        if (right.GetPrereleaseIdentifierCount() > maxCount)
        {
            maxCount = right.GetPrereleaseIdentifierCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            if (index >= left.GetPrereleaseIdentifierCount())
            {
                return -1;
            }

            if (index >= right.GetPrereleaseIdentifierCount())
            {
                return 1;
            }

            int comparison = ComparePrereleaseIdentifier(left.GetPrereleaseIdentifier(index), right.GetPrereleaseIdentifier(index));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static int ComparePrereleaseIdentifier(VersionIdentifier left, VersionIdentifier right)
    {
        if (left.IsNumeric() && right.IsNumeric())
        {
            return CompareNumericText(left.GetText(), right.GetText());
        }

        if (left.IsNumeric())
        {
            return -1;
        }

        if (right.IsNumeric())
        {
            return 1;
        }

        return NormalizeComparison(System.String.Compare(left.GetText(), right.GetText(), System.StringComparison.OrdinalIgnoreCase));
    }

    internal static int CompareNumericText(string left, string right)
    {
        string normalizedLeft = VersionFormatter.NormalizeNumericText(left);
        string normalizedRight = VersionFormatter.NormalizeNumericText(right);

        if (normalizedLeft.Length < normalizedRight.Length)
        {
            return -1;
        }

        if (normalizedLeft.Length > normalizedRight.Length)
        {
            return 1;
        }

        return NormalizeComparison(System.String.CompareOrdinal(normalizedLeft, normalizedRight));
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
