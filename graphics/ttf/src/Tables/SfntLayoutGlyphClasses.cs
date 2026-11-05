namespace ttf;

internal class SfntCoverageRange
{
    int startGlyphId;
    int endGlyphId;
    int startCoverageIndex;

    internal SfntCoverageRange(
        int startGlyphId,
        int endGlyphId,
        int startCoverageIndex)
    {
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.startCoverageIndex = startCoverageIndex;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetEndGlyphId()
    {
        return endGlyphId;
    }

    internal int GetStartCoverageIndex()
    {
        return startCoverageIndex;
    }

    internal bool Contains(int glyphId)
    {
        return glyphId >= startGlyphId && glyphId <= endGlyphId;
    }

    internal int GetCoverageIndex(int glyphId)
    {
        if (!Contains(glyphId))
        {
            return -1;
        }

        return startCoverageIndex + glyphId - startGlyphId;
    }
}

internal class SfntCoverageTable
{
    int format;
    int[] glyphIds;
    SfntCoverageRange[] ranges;
    int coveredGlyphCount;

    internal SfntCoverageTable(
        int format,
        int[] glyphIds,
        SfntCoverageRange[] ranges,
        int coveredGlyphCount)
    {
        this.format = format;
        this.glyphIds = glyphIds;
        this.ranges = ranges;
        this.coveredGlyphCount = coveredGlyphCount;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetCoveredGlyphCount()
    {
        return coveredGlyphCount;
    }

    internal int GetExplicitGlyphCount()
    {
        return glyphIds.Length;
    }

    internal int GetExplicitGlyphId(int index)
    {
        return glyphIds[index];
    }

    internal int GetRangeCount()
    {
        return ranges.Length;
    }

    internal SfntCoverageRange GetRange(int index)
    {
        return ranges[index];
    }

    internal bool Covers(int glyphId)
    {
        return GetCoverageIndex(glyphId) >= 0;
    }

    internal int GetGlyphId(int coverageIndex)
    {
        if (coverageIndex < 0 || coverageIndex >= coveredGlyphCount)
        {
            throw new System.ArgumentOutOfRangeException("coverageIndex");
        }

        if (format == 1)
        {
            return glyphIds[coverageIndex];
        }

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCoverageRange range = ranges[middle];
            int rangeStart = range.GetStartCoverageIndex();
            int rangeLength = range.GetEndGlyphId() -
                range.GetStartGlyphId() + 1;

            if (coverageIndex < rangeStart)
            {
                high = middle - 1;
            }
            else if (coverageIndex >= rangeStart + rangeLength)
            {
                low = middle + 1;
            }
            else
            {
                return range.GetStartGlyphId() +
                    coverageIndex - rangeStart;
            }
        }

        throw FontFormatException.Create(
            "A Coverage index does not map to a glyph ID.");
    }

    internal int GetCoverageIndex(int glyphId)
    {
        if (format == 1)
        {
            return FindExplicitGlyph(glyphId);
        }

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCoverageRange range = ranges[middle];

            if (glyphId < range.GetStartGlyphId())
            {
                high = middle - 1;
            }
            else if (glyphId > range.GetEndGlyphId())
            {
                low = middle + 1;
            }
            else
            {
                return range.GetCoverageIndex(glyphId);
            }
        }

        return -1;
    }

    int FindExplicitGlyph(int glyphId)
    {
        int low = 0;
        int high = glyphIds.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = glyphIds[middle];

            if (candidate == glyphId)
            {
                return middle;
            }

            if (candidate < glyphId)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }
}

internal class SfntClassRange
{
    int startGlyphId;
    int endGlyphId;
    int classValue;

    internal SfntClassRange(
        int startGlyphId,
        int endGlyphId,
        int classValue)
    {
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.classValue = classValue;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetEndGlyphId()
    {
        return endGlyphId;
    }

    internal int GetClassValue()
    {
        return classValue;
    }

    internal bool Contains(int glyphId)
    {
        return glyphId >= startGlyphId && glyphId <= endGlyphId;
    }
}

internal class SfntClassDefinitionTable
{
    int format;
    int startGlyphId;
    int[] classValues;
    SfntClassRange[] ranges;
    int maximumClassValue;

    internal SfntClassDefinitionTable(
        int format,
        int startGlyphId,
        int[] classValues,
        SfntClassRange[] ranges,
        int maximumClassValue)
    {
        this.format = format;
        this.startGlyphId = startGlyphId;
        this.classValues = classValues;
        this.ranges = ranges;
        this.maximumClassValue = maximumClassValue;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetClassValueCount()
    {
        return classValues.Length;
    }

    internal int GetClassValue(int index)
    {
        return classValues[index];
    }

    internal int GetRangeCount()
    {
        return ranges.Length;
    }

    internal SfntClassRange GetRange(int index)
    {
        return ranges[index];
    }

    internal int GetMaximumClassValue()
    {
        return maximumClassValue;
    }

    internal int GetClassForGlyph(int glyphId)
    {
        if (format == 1)
        {
            int index = glyphId - startGlyphId;

            if (index < 0 || index >= classValues.Length)
            {
                return 0;
            }

            return classValues[index];
        }

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntClassRange range = ranges[middle];

            if (glyphId < range.GetStartGlyphId())
            {
                high = middle - 1;
            }
            else if (glyphId > range.GetEndGlyphId())
            {
                low = middle + 1;
            }
            else
            {
                return range.GetClassValue();
            }
        }

        return 0;
    }
}
