namespace ttf;

internal static class PdfCidVerticalWidthsBuilder
{
    internal static PdfCidVerticalWidthsArtifact Build(
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
        PdfCidVerticalMetric[] metrics =
            new PdfCidVerticalMetric[sourceGlyphIds.Length];
        int cid = 0;

        while (cid < metrics.Length)
        {
            int sourceGlyphId = sourceGlyphIds[cid];
            if (sourceGlyphId < 0)
            {
                sourceGlyphId = 0;
            }
            GlyphMetrics glyph = instance.GetGlyphMetrics(sourceGlyphId);
            metrics[cid] = new PdfCidVerticalMetric(
                ConvertDisplacement(glyph.GetAdvanceHeight(), unitsPerEm),
                ConvertOriginX(glyph.GetAdvanceWidth(), unitsPerEm),
                ConvertOriginY(glyph.GetVerticalOriginY(), unitsPerEm));
            cid += 1;
        }

        return BuildFromMetrics(metrics);
    }

    internal static int ConvertDisplacement(
        int advanceHeight,
        int unitsPerEm)
    {
        return -PdfCidWidthsBuilder.ConvertAdvance(
            advanceHeight,
            unitsPerEm);
    }

    internal static int ConvertOriginX(
        int advanceWidth,
        int unitsPerEm)
    {
        if (advanceWidth < 0)
        {
            throw new System.ArgumentOutOfRangeException("advanceWidth");
        }

        return ConvertNumerator(
            (long)advanceWidth * 500,
            unitsPerEm);
    }

    internal static int ConvertOriginY(
        int originY,
        int unitsPerEm)
    {
        return ConvertNumerator((long)originY * 1000, unitsPerEm);
    }

    internal static PdfCidVerticalWidthsArtifact BuildFromMetrics(
        PdfCidVerticalMetric[] metrics)
    {
        if (metrics == null)
        {
            throw new System.ArgumentNullException("metrics");
        }

        if (metrics.Length == 0)
        {
            throw new System.ArgumentException(
                "At least CID zero must have vertical metrics.",
                "metrics");
        }

        ValidateMetrics(metrics);
        PdfCidVerticalMetric defaultMetric = SelectDefault(metrics);
        PdfCidVerticalWidthEntry[] entries = BuildEntries(
            metrics,
            defaultMetric);
        return new PdfCidVerticalWidthsArtifact(
            defaultMetric.GetOriginY(),
            defaultMetric.GetDisplacementY(),
            metrics,
            entries);
    }

    static int ConvertNumerator(long numerator, int unitsPerEm)
    {
        if (unitsPerEm <= 0)
        {
            throw new System.ArgumentOutOfRangeException("unitsPerEm");
        }

        bool negative = numerator < 0;
        long magnitude = negative ? -numerator : numerator;
        long rounded = (magnitude + unitsPerEm / 2) / unitsPerEm;

        if (rounded > int.MaxValue)
        {
            throw FontLimitException.Create(
                "A converted PDF vertical metric exceeds the supported range.");
        }

        int result = (int)rounded;
        return negative ? -result : result;
    }

    static PdfCidVerticalMetric SelectDefault(
        PdfCidVerticalMetric[] metrics)
    {
        System.Collections.Generic.Dictionary<long, int> counts =
            new System.Collections.Generic.Dictionary<long, int>();
        int index = 0;

        while (index < metrics.Length)
        {
            long key = CreateDefaultKey(metrics[index]);

            if (counts.ContainsKey(key))
            {
                counts[key] = counts[key] + 1;
            }
            else
            {
                counts.Add(key, 1);
            }

            index += 1;
        }

        PdfCidVerticalMetric selected = metrics[0];
        int selectedCount = 0;
        index = 0;

        while (index < metrics.Length)
        {
            PdfCidVerticalMetric candidate = metrics[index];
            int count = counts[CreateDefaultKey(candidate)];

            if (count > selectedCount ||
                (count == selectedCount &&
                    IsPreferredTie(candidate, selected)))
            {
                selected = candidate;
                selectedCount = count;
            }

            index += 1;
        }

        return selected;
    }

    static long CreateDefaultKey(PdfCidVerticalMetric metric)
    {
        uint originY = CheckedFontMath.ReinterpretInt32AsUInt32(
            metric.GetOriginY());
        uint displacementY = CheckedFontMath.ReinterpretInt32AsUInt32(
            metric.GetDisplacementY());
        return ((long)originY << 32) | displacementY;
    }

    static bool IsPreferredTie(
        PdfCidVerticalMetric candidate,
        PdfCidVerticalMetric selected)
    {
        bool candidateIntrinsic = IsIntrinsicDefault(candidate);
        bool selectedIntrinsic = IsIntrinsicDefault(selected);

        if (candidateIntrinsic != selectedIntrinsic)
        {
            return candidateIntrinsic;
        }

        if (candidate.GetOriginY() != selected.GetOriginY())
        {
            return candidate.GetOriginY() < selected.GetOriginY();
        }

        return candidate.GetDisplacementY() <
            selected.GetDisplacementY();
    }

    static bool IsIntrinsicDefault(PdfCidVerticalMetric metric)
    {
        return metric.GetOriginY() == 880 &&
            metric.GetDisplacementY() == -1000;
    }

    static PdfCidVerticalWidthEntry[] BuildEntries(
        PdfCidVerticalMetric[] metrics,
        PdfCidVerticalMetric defaultMetric)
    {
        System.Collections.Generic.List<PdfCidVerticalWidthEntry> entries =
            new System.Collections.Generic.List<PdfCidVerticalWidthEntry>();
        int cid = 0;

        while (cid < metrics.Length)
        {
            while (cid < metrics.Length &&
                HasDefaultPair(metrics[cid], defaultMetric))
            {
                cid += 1;
            }

            int segmentEnd = cid;

            while (segmentEnd < metrics.Length &&
                !HasDefaultPair(metrics[segmentEnd], defaultMetric))
            {
                segmentEnd += 1;
            }

            while (cid < segmentEnd)
            {
                int repeatedEnd = FindRepeatedEnd(
                    metrics,
                    cid,
                    segmentEnd);

                if (repeatedEnd - cid >= 2)
                {
                    entries.Add(CreateRangeEntry(
                        cid,
                        repeatedEnd - 1,
                        metrics[cid]));
                    cid = repeatedEnd;
                }
                else
                {
                    int arrayStart = cid;
                    cid += 1;

                    while (cid < segmentEnd &&
                        FindRepeatedEnd(metrics, cid, segmentEnd) - cid < 2)
                    {
                        cid += 1;
                    }

                    entries.Add(CreateArrayEntry(
                        metrics,
                        arrayStart,
                        cid));
                }
            }
        }

        return entries.ToArray();
    }

    static bool HasDefaultPair(
        PdfCidVerticalMetric metric,
        PdfCidVerticalMetric defaultMetric)
    {
        return metric.GetDisplacementY() ==
            defaultMetric.GetDisplacementY() &&
            metric.GetOriginY() == defaultMetric.GetOriginY();
    }

    static int FindRepeatedEnd(
        PdfCidVerticalMetric[] metrics,
        int start,
        int limit)
    {
        int end = start + 1;

        while (end < limit && AreEqual(metrics[end], metrics[start]))
        {
            end += 1;
        }

        return end;
    }

    static bool AreEqual(
        PdfCidVerticalMetric left,
        PdfCidVerticalMetric right)
    {
        return left.GetDisplacementY() == right.GetDisplacementY() &&
            left.GetOriginX() == right.GetOriginX() &&
            left.GetOriginY() == right.GetOriginY();
    }

    static PdfCidVerticalWidthEntry CreateRangeEntry(
        int firstCid,
        int lastCid,
        PdfCidVerticalMetric metric)
    {
        return new PdfCidVerticalWidthEntry(
            PdfCidWidthEntryKind.Range,
            firstCid,
            lastCid,
            metric,
            new PdfCidVerticalMetric[0]);
    }

    static PdfCidVerticalWidthEntry CreateArrayEntry(
        PdfCidVerticalMetric[] metrics,
        int start,
        int end)
    {
        PdfCidVerticalMetric[] values =
            new PdfCidVerticalMetric[end - start];
        System.Array.Copy(metrics, start, values, 0, values.Length);
        return new PdfCidVerticalWidthEntry(
            PdfCidWidthEntryKind.Array,
            start,
            end - 1,
            null,
            values);
    }

    static void ValidateMetrics(PdfCidVerticalMetric[] metrics)
    {
        int index = 0;

        while (index < metrics.Length)
        {
            if (metrics[index] == null)
            {
                throw new System.ArgumentException(
                    "Vertical metrics cannot contain null entries.",
                    "metrics");
            }

            index += 1;
        }
    }
}
