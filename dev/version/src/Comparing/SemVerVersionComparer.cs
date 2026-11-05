namespace version;

internal static class SemVerVersionComparer
{
    internal static int ComparePrecedence(UniversalVersion left, UniversalVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        int coreComparison = CompareCoreComponents(left, right);
        if (coreComparison != 0)
        {
            return coreComparison;
        }

        return ComparePrerelease(left, right);
    }

    internal static int CompareIdentity(UniversalVersion left, UniversalVersion right)
    {
        int precedenceComparison = ComparePrecedence(left, right);
        if (precedenceComparison != 0)
        {
            return precedenceComparison;
        }

        return CompareBuildMetadata(left, right);
    }

    internal static int CompareNormalizedIdentity(UniversalVersion left, UniversalVersion right)
    {
        return ComparePrecedence(left, right);
    }

    internal static int ComparePrecedenceCaseInsensitive(UniversalVersion left, UniversalVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        int coreComparison = CompareCoreComponents(left, right);
        if (coreComparison != 0)
        {
            return coreComparison;
        }

        return ComparePrereleaseCaseInsensitive(left, right);
    }

    internal static int CompareIdentityCaseInsensitive(UniversalVersion left, UniversalVersion right)
    {
        int precedenceComparison = ComparePrecedenceCaseInsensitive(left, right);
        if (precedenceComparison != 0)
        {
            return precedenceComparison;
        }

        return CompareBuildMetadataCaseInsensitive(left, right);
    }

    internal static bool EqualsByPrecedence(UniversalVersion left, UniversalVersion right)
    {
        return ComparePrecedence(left, right) == 0;
    }

    internal static bool EqualsByIdentity(UniversalVersion left, UniversalVersion right)
    {
        return CompareIdentity(left, right) == 0;
    }

    internal static bool EqualsByNormalizedIdentity(UniversalVersion left, UniversalVersion right)
    {
        return CompareNormalizedIdentity(left, right) == 0;
    }

    internal static int CompareCoreComponents(UniversalVersion left, UniversalVersion right)
    {
        int maxCount = left.GetNumericComponentCount();
        if (right.GetNumericComponentCount() > maxCount)
        {
            maxCount = right.GetNumericComponentCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            string leftText = "0";
            string rightText = "0";

            if (index < left.GetNumericComponentCount())
            {
                leftText = left.GetNumericComponent(index);
            }

            if (index < right.GetNumericComponentCount())
            {
                rightText = right.GetNumericComponent(index);
            }

            int comparison = CompareNumericText(leftText, rightText);
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
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

    internal static int ComparePrereleaseCaseInsensitive(UniversalVersion left, UniversalVersion right)
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

            int comparison = ComparePrereleaseIdentifierCaseInsensitive(
                left.GetPrereleaseIdentifier(index),
                right.GetPrereleaseIdentifier(index));
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

        return NormalizeComparison(System.String.CompareOrdinal(left.GetText(), right.GetText()));
    }

    internal static int ComparePrereleaseIdentifierCaseInsensitive(VersionIdentifier left, VersionIdentifier right)
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

    internal static int CompareBuildMetadata(UniversalVersion left, UniversalVersion right)
    {
        int maxCount = left.GetBuildIdentifierCount();
        if (right.GetBuildIdentifierCount() > maxCount)
        {
            maxCount = right.GetBuildIdentifierCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            if (index >= left.GetBuildIdentifierCount())
            {
                return -1;
            }

            if (index >= right.GetBuildIdentifierCount())
            {
                return 1;
            }

            int comparison = NormalizeComparison(System.String.CompareOrdinal(left.GetBuildIdentifier(index).GetText(), right.GetBuildIdentifier(index).GetText()));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static int CompareBuildMetadataCaseInsensitive(UniversalVersion left, UniversalVersion right)
    {
        int maxCount = left.GetBuildIdentifierCount();
        if (right.GetBuildIdentifierCount() > maxCount)
        {
            maxCount = right.GetBuildIdentifierCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            if (index >= left.GetBuildIdentifierCount())
            {
                return -1;
            }

            if (index >= right.GetBuildIdentifierCount())
            {
                return 1;
            }

            int comparison = NormalizeComparison(System.String.Compare(
                left.GetBuildIdentifier(index).GetText(),
                right.GetBuildIdentifier(index).GetText(),
                System.StringComparison.OrdinalIgnoreCase));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
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
