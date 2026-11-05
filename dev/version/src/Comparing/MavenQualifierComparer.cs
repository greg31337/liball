namespace version;

internal static class MavenQualifierComparer
{
    internal static string NormalizeQualifier(string qualifier)
    {
        System.ArgumentNullException.ThrowIfNull(qualifier);

        string lower = qualifier.ToLowerInvariant();

        if (lower == "a")
        {
            return "alpha";
        }

        if (lower == "b")
        {
            return "beta";
        }

        if (lower == "m")
        {
            return "milestone";
        }

        if (lower == "cr")
        {
            return "rc";
        }

        if (lower == "ga" || lower == "final" || lower == "release")
        {
            return "";
        }

        return lower;
    }

    internal static int CompareQualifiers(string left, string right)
    {
        string normalizedLeft = NormalizeQualifier(left);
        string normalizedRight = NormalizeQualifier(right);
        int leftRank = GetQualifierRank(normalizedLeft);
        int rightRank = GetQualifierRank(normalizedRight);

        if (leftRank < rightRank)
        {
            return -1;
        }

        if (leftRank > rightRank)
        {
            return 1;
        }

        if (leftRank != 8)
        {
            return 0;
        }

        return NormalizeComparison(System.String.Compare(normalizedLeft, normalizedRight, System.StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsKnownQualifier(string qualifier)
    {
        return GetQualifierRank(NormalizeQualifier(qualifier)) != 8;
    }

    internal static int GetQualifierRank(string normalizedQualifier)
    {
        if (normalizedQualifier == "alpha")
        {
            return 1;
        }

        if (normalizedQualifier == "beta")
        {
            return 2;
        }

        if (normalizedQualifier == "milestone")
        {
            return 3;
        }

        if (normalizedQualifier == "rc")
        {
            return 4;
        }

        if (normalizedQualifier == "snapshot")
        {
            return 5;
        }

        if (normalizedQualifier == "")
        {
            return 6;
        }

        if (normalizedQualifier == "sp")
        {
            return 7;
        }

        return 8;
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
