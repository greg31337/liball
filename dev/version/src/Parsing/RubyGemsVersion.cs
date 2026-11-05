namespace version;

internal class RubyGemsVersion
{
    internal string originalText;
    internal RubyGemsVersionSegment[] segments;

    internal RubyGemsVersion(string originalTextValue, RubyGemsVersionSegment[] segmentValues)
    {
        System.ArgumentNullException.ThrowIfNull(originalTextValue);
        System.ArgumentNullException.ThrowIfNull(segmentValues);

        originalText = originalTextValue;
        segments = CopySegments(segmentValues);
    }

    internal string GetOriginalText()
    {
        return originalText;
    }

    internal int GetSegmentCount()
    {
        return segments.Length;
    }

    internal RubyGemsVersionSegment GetSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= segments.Length)
        {
            throw new System.ArgumentOutOfRangeException("segmentIndex");
        }

        return segments[segmentIndex];
    }

    internal RubyGemsVersionSegment[] GetSegments()
    {
        return CopySegments(segments);
    }

    internal bool IsPrerelease()
    {
        int index = 0;

        while (index < segments.Length)
        {
            if (segments[index].IsText())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static RubyGemsVersionSegment[] CopySegments(RubyGemsVersionSegment[] segmentValues)
    {
        RubyGemsVersionSegment[] copy = new RubyGemsVersionSegment[segmentValues.Length];
        int index = 0;

        while (index < segmentValues.Length)
        {
            if (segmentValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems segments cannot contain null values.");
            }

            copy[index] = segmentValues[index];
            index = index + 1;
        }

        return copy;
    }
}
