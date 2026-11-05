namespace ttf;

internal static class PdfCidWidthsBuilder
{
    internal static PdfCidWidthsArtifact Build(
        FontInstance instance,
        PdfCidAllocation allocation)
    {
        if (instance == null || allocation == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (allocation.CopySourceGlyphToCids().Length !=
            instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The CID allocation does not belong to the font instance.",
                "allocation");
        }

        int unitsPerEm = instance.GetGlobalMetrics().GetUnitsPerEm();
        int[] sourceGlyphIds = allocation.CopyCidToSourceGlyphIds();
        int[] widths = new int[sourceGlyphIds.Length];
        int cid = 0;

        while (cid < widths.Length)
        {
            int sourceGlyphId = sourceGlyphIds[cid];
            if (sourceGlyphId < 0)
            {
                sourceGlyphId = 0;
            }
            int advance = instance.GetGlyphMetrics(
                sourceGlyphId).GetAdvanceWidth();
            widths[cid] = ConvertAdvance(advance, unitsPerEm);
            cid += 1;
        }

        return BuildFromWidths(widths);
    }

    internal static int ConvertAdvance(int advance, int unitsPerEm)
    {
        if (advance < 0)
        {
            throw new System.ArgumentOutOfRangeException("advance");
        }

        if (unitsPerEm <= 0)
        {
            throw new System.ArgumentOutOfRangeException("unitsPerEm");
        }

        long numerator = (long)advance * 1000;
        long rounded = (numerator + unitsPerEm / 2) / unitsPerEm;

        if (rounded > int.MaxValue)
        {
            throw FontLimitException.Create(
                "A converted PDF glyph width exceeds the supported range.");
        }

        return (int)rounded;
    }

    internal static PdfCidWidthsArtifact BuildFromWidths(int[] widths)
    {
        if (widths == null)
        {
            throw new System.ArgumentNullException("widths");
        }

        if (widths.Length == 0)
        {
            throw new System.ArgumentException(
                "At least CID zero must have a width.",
                "widths");
        }

        ValidateWidths(widths);
        int defaultWidth = SelectDefaultWidth(widths);
        PdfCidWidthEntry[] entries = BuildEntries(
            widths,
            defaultWidth);
        return new PdfCidWidthsArtifact(
            defaultWidth,
            widths,
            entries);
    }

    static int SelectDefaultWidth(int[] widths)
    {
        System.Collections.Generic.Dictionary<int, int> counts =
            new System.Collections.Generic.Dictionary<int, int>();
        int index = 0;

        while (index < widths.Length)
        {
            int width = widths[index];

            if (counts.ContainsKey(width))
            {
                counts[width] = counts[width] + 1;
            }
            else
            {
                counts.Add(width, 1);
            }

            index += 1;
        }

        int selectedWidth = widths[0];
        int selectedCount = 0;

        foreach (System.Collections.Generic.KeyValuePair<int, int> item
            in counts)
        {
            int width = item.Key;
            int count = item.Value;

            if (count > selectedCount ||
                (count == selectedCount &&
                    IsPreferredTie(width, selectedWidth)))
            {
                selectedWidth = width;
                selectedCount = count;
            }
        }

        return selectedWidth;
    }

    static bool IsPreferredTie(int candidate, int selected)
    {
        if (candidate == 1000)
        {
            return selected != 1000;
        }

        if (selected == 1000)
        {
            return false;
        }

        return candidate < selected;
    }

    static PdfCidWidthEntry[] BuildEntries(
        int[] widths,
        int defaultWidth)
    {
        System.Collections.Generic.List<PdfCidWidthEntry> entries =
            new System.Collections.Generic.List<PdfCidWidthEntry>();
        int cid = 0;

        while (cid < widths.Length)
        {
            while (cid < widths.Length && widths[cid] == defaultWidth)
            {
                cid += 1;
            }

            int segmentEnd = cid;

            while (segmentEnd < widths.Length &&
                widths[segmentEnd] != defaultWidth)
            {
                segmentEnd += 1;
            }

            while (cid < segmentEnd)
            {
                int repeatedEnd = FindRepeatedEnd(widths, cid, segmentEnd);

                if (repeatedEnd - cid >= 2)
                {
                    entries.Add(CreateRangeEntry(
                        cid,
                        repeatedEnd - 1,
                        widths[cid]));
                    cid = repeatedEnd;
                }
                else
                {
                    int arrayStart = cid;
                    cid += 1;

                    while (cid < segmentEnd &&
                        FindRepeatedEnd(widths, cid, segmentEnd) - cid < 2)
                    {
                        cid += 1;
                    }

                    entries.Add(CreateArrayEntry(
                        widths,
                        arrayStart,
                        cid));
                }
            }
        }

        return entries.ToArray();
    }

    static int FindRepeatedEnd(
        int[] widths,
        int start,
        int limit)
    {
        int end = start + 1;

        while (end < limit && widths[end] == widths[start])
        {
            end += 1;
        }

        return end;
    }

    static PdfCidWidthEntry CreateRangeEntry(
        int firstCid,
        int lastCid,
        int width)
    {
        return new PdfCidWidthEntry(
            PdfCidWidthEntryKind.Range,
            firstCid,
            lastCid,
            width,
            new int[0]);
    }

    static PdfCidWidthEntry CreateArrayEntry(
        int[] widths,
        int start,
        int end)
    {
        int[] values = new int[end - start];
        System.Array.Copy(widths, start, values, 0, values.Length);
        return new PdfCidWidthEntry(
            PdfCidWidthEntryKind.Array,
            start,
            end - 1,
            0,
            values);
    }

    static void ValidateWidths(int[] widths)
    {
        int index = 0;

        while (index < widths.Length)
        {
            if (widths[index] < 0)
            {
                throw new System.ArgumentOutOfRangeException("widths");
            }

            index += 1;
        }
    }
}
