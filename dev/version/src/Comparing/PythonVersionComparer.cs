namespace version;

internal static class PythonVersionComparer
{
    internal static int Compare(string leftText, string rightText)
    {
        PythonVersion left = PythonVersionParser.ParseVersion(leftText);
        PythonVersion right = PythonVersionParser.ParseVersion(rightText);

        return Compare(left, right);
    }

    internal static int Compare(PythonVersion left, PythonVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        int comparison = CompareNumericText(left.GetEpoch(), right.GetEpoch());
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareReleaseSegments(left, right);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = CompareReleasePhase(left, right);
        if (comparison != 0)
        {
            return comparison;
        }

        return CompareLocalSegments(left, right);
    }

    internal static int CompareReleaseSegments(PythonVersion left, PythonVersion right)
    {
        int maxCount = left.GetReleaseSegmentCount();
        if (right.GetReleaseSegmentCount() > maxCount)
        {
            maxCount = right.GetReleaseSegmentCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            int comparison = CompareNumericText(GetReleaseSegmentOrZero(left, index), GetReleaseSegmentOrZero(right, index));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string GetReleaseSegmentOrZero(PythonVersion version, int segmentIndex)
    {
        if (segmentIndex < version.GetReleaseSegmentCount())
        {
            return version.GetReleaseSegment(segmentIndex);
        }

        return "0";
    }

    internal static int CompareReleasePhase(PythonVersion left, PythonVersion right)
    {
        int leftRank = GetReleasePhaseRank(left);
        int rightRank = GetReleasePhaseRank(right);

        if (leftRank != rightRank)
        {
            return NormalizeComparison(leftRank - rightRank);
        }

        if (leftRank == 1)
        {
            int comparison = ComparePreRelease(left, right);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        if (leftRank == 3)
        {
            int comparison = CompareNumericText(left.GetPostReleaseNumber(), right.GetPostReleaseNumber());
            if (comparison != 0)
            {
                return comparison;
            }
        }

        if (left.HasDevelopmentRelease() || right.HasDevelopmentRelease())
        {
            return CompareDevelopmentRelease(left, right);
        }

        return 0;
    }

    internal static int GetReleasePhaseRank(PythonVersion version)
    {
        if (version.HasDevelopmentRelease() && !version.HasPreRelease() && !version.HasPostRelease())
        {
            return 0;
        }

        if (version.HasPreRelease())
        {
            return 1;
        }

        if (version.HasPostRelease())
        {
            return 3;
        }

        return 2;
    }

    internal static int ComparePreRelease(PythonVersion left, PythonVersion right)
    {
        int comparison = NormalizeComparison(GetPreReleaseLabelRank(left.GetPreReleaseLabel()) - GetPreReleaseLabelRank(right.GetPreReleaseLabel()));
        if (comparison != 0)
        {
            return comparison;
        }

        return CompareNumericText(left.GetPreReleaseNumber(), right.GetPreReleaseNumber());
    }

    internal static int GetPreReleaseLabelRank(string label)
    {
        if (label == "a")
        {
            return 0;
        }

        if (label == "b")
        {
            return 1;
        }

        return 2;
    }

    internal static int CompareDevelopmentRelease(PythonVersion left, PythonVersion right)
    {
        if (left.HasDevelopmentRelease() && !right.HasDevelopmentRelease())
        {
            return -1;
        }

        if (!left.HasDevelopmentRelease() && right.HasDevelopmentRelease())
        {
            return 1;
        }

        if (!left.HasDevelopmentRelease() && !right.HasDevelopmentRelease())
        {
            return 0;
        }

        return CompareNumericText(left.GetDevelopmentReleaseNumber(), right.GetDevelopmentReleaseNumber());
    }

    internal static int CompareLocalSegments(PythonVersion left, PythonVersion right)
    {
        if (!left.HasLocalVersion() && !right.HasLocalVersion())
        {
            return 0;
        }

        if (!left.HasLocalVersion())
        {
            return -1;
        }

        if (!right.HasLocalVersion())
        {
            return 1;
        }

        int maxCount = left.GetLocalSegmentCount();
        if (right.GetLocalSegmentCount() > maxCount)
        {
            maxCount = right.GetLocalSegmentCount();
        }

        int index = 0;
        while (index < maxCount)
        {
            if (index >= left.GetLocalSegmentCount())
            {
                return -1;
            }

            if (index >= right.GetLocalSegmentCount())
            {
                return 1;
            }

            int comparison = CompareLocalSegment(left.GetLocalSegment(index), right.GetLocalSegment(index));
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static int CompareLocalSegment(PythonLocalVersionSegment left, PythonLocalVersionSegment right)
    {
        if (left.IsNumeric() && right.IsNumeric())
        {
            return CompareNumericText(left.GetText(), right.GetText());
        }

        if (left.IsNumeric())
        {
            return 1;
        }

        if (right.IsNumeric())
        {
            return -1;
        }

        return NormalizeComparison(System.String.CompareOrdinal(left.GetText(), right.GetText()));
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
