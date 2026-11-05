namespace version;

internal static class RubyGemsVersionFormatter
{
    internal static string FormatNormalized(RubyGemsVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return FormatSegments(version.GetSegments());
    }

    internal static string FormatCanonical(RubyGemsVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        return FormatSegments(GetCanonicalSegments(version));
    }

    internal static string FormatNormalized(string text)
    {
        RubyGemsVersionParseResult result = RubyGemsVersionParser.Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems version.");
        }

        return FormatNormalized(result.GetVersion());
    }

    internal static string FormatCanonical(string text)
    {
        RubyGemsVersionParseResult result = RubyGemsVersionParser.Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems version.");
        }

        return FormatCanonical(result.GetVersion());
    }

    internal static string FormatSegments(RubyGemsVersionSegment[] segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < segments.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            if (segments[index].IsNumeric())
            {
                builder.Append(VersionFormatter.NormalizeNumericText(segments[index].GetText()));
            }
            else
            {
                builder.Append(segments[index].GetText());
            }

            index = index + 1;
        }

        return builder.ToString();
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

    internal static bool IsZeroNumericSegment(RubyGemsVersionSegment segment)
    {
        if (segment.IsText())
        {
            return false;
        }

        return VersionFormatter.NormalizeNumericText(segment.GetText()) == "0";
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
}
