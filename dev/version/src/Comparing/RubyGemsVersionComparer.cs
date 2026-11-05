namespace version;

internal static class RubyGemsVersionComparer
{
    internal static int Compare(string leftText, string rightText)
    {
        RubyGemsVersionParseResult leftResult = RubyGemsVersionParser.Parse(leftText);
        RubyGemsVersionParseResult rightResult = RubyGemsVersionParser.Parse(rightText);

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

    internal static int Compare(RubyGemsVersion left, RubyGemsVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        RubyGemsVersionSegment[] leftSegments = GetCanonicalSegments(left);
        RubyGemsVersionSegment[] rightSegments = GetCanonicalSegments(right);
        int maxCount = leftSegments.Length;

        if (rightSegments.Length > maxCount)
        {
            maxCount = rightSegments.Length;
        }

        int index = 0;
        while (index < maxCount)
        {
            RubyGemsVersionSegment leftSegment = GetSegmentOrZero(leftSegments, index);
            RubyGemsVersionSegment rightSegment = GetSegmentOrZero(rightSegments, index);
            int comparison = CompareSegments(leftSegment, rightSegment);

            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static bool IsPrerelease(string text)
    {
        RubyGemsVersionParseResult result = RubyGemsVersionParser.Parse(text);
        if (result.IsFailure())
        {
            return false;
        }

        return IsPrerelease(result.GetVersion());
    }

    internal static bool IsPrerelease(RubyGemsVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return version.IsPrerelease();
    }

    internal static RubyGemsVersionSegment[] GetCanonicalSegments(RubyGemsVersion version)
    {
        RubyGemsVersionSegment[] segments = version.GetSegments();
        int length = TrimTrailingZeroSegments(segments);

        if (version.IsPrerelease())
        {
            length = RemoveZerosBeforeFirstTextSegment(segments, length);
        }

        return CopySegments(segments, length);
    }

    internal static int TrimTrailingZeroSegments(RubyGemsVersionSegment[] segments)
    {
        int length = segments.Length;

        while (length > 1 && IsZeroNumericSegment(segments[length - 1]))
        {
            length = length - 1;
        }

        return length;
    }

    internal static int RemoveZerosBeforeFirstTextSegment(RubyGemsVersionSegment[] segments, int length)
    {
        int firstTextIndex = FindFirstTextSegmentIndex(segments, length);
        if (firstTextIndex < 0)
        {
            return length;
        }

        int removeStartIndex = firstTextIndex;
        while (removeStartIndex > 0 && IsZeroNumericSegment(segments[removeStartIndex - 1]))
        {
            removeStartIndex = removeStartIndex - 1;
        }

        if (removeStartIndex == firstTextIndex)
        {
            return length;
        }

        int readIndex = firstTextIndex;
        int writeIndex = removeStartIndex;
        while (readIndex < length)
        {
            segments[writeIndex] = segments[readIndex];
            readIndex = readIndex + 1;
            writeIndex = writeIndex + 1;
        }

        return length - (firstTextIndex - removeStartIndex);
    }

    internal static int FindFirstTextSegmentIndex(RubyGemsVersionSegment[] segments, int length)
    {
        int index = 0;

        while (index < length)
        {
            if (segments[index].IsText())
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static RubyGemsVersionSegment[] CopySegments(RubyGemsVersionSegment[] segments, int length)
    {
        RubyGemsVersionSegment[] copy = new RubyGemsVersionSegment[length];
        int index = 0;

        while (index < length)
        {
            copy[index] = segments[index];
            index = index + 1;
        }

        return copy;
    }

    internal static RubyGemsVersionSegment GetSegmentOrZero(RubyGemsVersionSegment[] segments, int segmentIndex)
    {
        if (segmentIndex < segments.Length)
        {
            return segments[segmentIndex];
        }

        return new RubyGemsVersionSegment("0", true);
    }

    internal static int CompareSegments(RubyGemsVersionSegment left, RubyGemsVersionSegment right)
    {
        if (left.IsNumeric() && right.IsNumeric())
        {
            return CompareNumericText(left.GetText(), right.GetText());
        }

        if (left.IsText() && right.IsText())
        {
            return NormalizeComparison(System.String.CompareOrdinal(left.GetText(), right.GetText()));
        }

        if (left.IsText())
        {
            return -1;
        }

        return 1;
    }

    internal static bool IsZeroNumericSegment(RubyGemsVersionSegment segment)
    {
        if (segment.IsText())
        {
            return false;
        }

        return CompareNumericText(segment.GetText(), "0") == 0;
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
