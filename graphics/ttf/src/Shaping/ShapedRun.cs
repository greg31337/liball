namespace ttf;

internal class ShapedClusterRanges
{
    int[] starts;
    int[] ends;

    internal ShapedClusterRanges(int[] starts, int[] ends)
    {
        this.starts = starts;
        this.ends = ends;
    }

    internal int[] GetStarts()
    {
        return starts;
    }

    internal int[] GetEnds()
    {
        return ends;
    }
}

internal static class ShapedRunBuilder
{
    internal static ShapedRun Build(
        ShapingBuffer buffer,
        RuneBuffer source)
    {
        return Build(buffer, source, null);
    }

    internal static ShapedRun Build(
        ShapingBuffer buffer,
        RuneBuffer source,
        SfntGposPositionBuffer? positions)
    {
        ValidateInputs(buffer, source, positions);
        int glyphCount = buffer.GetCount();
        ShapedClusterRanges ranges = BuildClusterRanges(buffer, source);
        int[] clusterStarts = ranges.GetStarts();
        int[] clusterEnds = ranges.GetEnds();
        int clusterCount = clusterStarts.Length;
        int[] glyphClusters = AssignGlyphClusters(
            buffer,
            clusterStarts,
            clusterEnds);
        int[][] clusterGlyphSources = GroupGlyphs(
            glyphClusters,
            clusterCount);
        int[] clusterVisualIndexes = new int[clusterCount];
        int[] visualToLogicalClusters = new int[clusterCount];
        BuildClusterMappings(
            buffer.IsForward(),
            clusterVisualIndexes,
            visualToLogicalClusters);
        int[] logicalToVisualGlyphs = new int[glyphCount];
        int[] visualToLogicalGlyphs = new int[glyphCount];
        int[] logicalGlyphSources = new int[glyphCount];
        int[] visualGlyphSources = new int[glyphCount];
        BuildGlyphMappings(
            clusterGlyphSources,
            visualToLogicalClusters,
            logicalToVisualGlyphs,
            visualToLogicalGlyphs,
            logicalGlyphSources,
            visualGlyphSources);
        int[][] clusterScalars = BuildClusterScalars(
            source,
            clusterStarts,
            clusterEnds);
        ShapedGlyph[] glyphs = BuildGlyphs(
            buffer,
            positions,
            glyphClusters,
            clusterStarts,
            clusterEnds,
            clusterScalars,
            visualToLogicalGlyphs,
            visualGlyphSources);
        ShapedCluster[] clusters = BuildClusters(
            buffer,
            clusterStarts,
            clusterEnds,
            clusterScalars,
            clusterGlyphSources,
            clusterVisualIndexes,
            logicalToVisualGlyphs,
            logicalGlyphSources);
        return new ShapedRun(
            glyphs,
            clusters,
            logicalToVisualGlyphs,
            visualToLogicalGlyphs,
            clusterVisualIndexes,
            visualToLogicalClusters,
            buffer.GetSourceLength(),
            buffer.GetDirection());
    }

    static void ValidateInputs(
        ShapingBuffer buffer,
        RuneBuffer source,
        SfntGposPositionBuffer? positions)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.GetSourceLength() != buffer.GetSourceLength())
        {
            throw new System.ArgumentException(
                "The source and shaping buffer must describe the same UTF-16 length.");
        }

        if (positions != null && positions.GetCount() != buffer.GetCount())
        {
            throw new System.ArgumentException(
                "Position data must describe every shaped glyph.");
        }

        int index = 0;
        while (index < buffer.GetCount())
        {
            if (!buffer.HasGlyphId(index))
            {
                throw new System.InvalidOperationException(
                    "Every shaped item must have a glyph ID.");
            }

            if (positions != null &&
                positions.GetGlyphId(index) != buffer.GetGlyphId(index))
            {
                throw new System.ArgumentException(
                    "Position data glyph IDs must match the shaping buffer.");
            }

            index += 1;
        }
    }

    static ShapedClusterRanges BuildClusterRanges(
        ShapingBuffer buffer,
        RuneBuffer source)
    {
        int segmentCount = source.GetCount() + buffer.GetCount();
        int[] segmentStarts = new int[segmentCount];
        int[] segmentEnds = new int[segmentCount];
        int index = 0;

        while (index < source.GetCount())
        {
            int start = source.GetUtf16Start(index);
            int length = source.GetUtf16Length(index);
            ValidateSourceRange(start, length, source.GetSourceLength());
            segmentStarts[index] = start;
            segmentEnds[index] = start + length;
            index += 1;
        }

        int glyphIndex = 0;
        while (glyphIndex < buffer.GetCount())
        {
            segmentStarts[index] = buffer.GetClusterStart(glyphIndex);
            segmentEnds[index] = buffer.GetClusterEnd(glyphIndex);
            index += 1;
            glyphIndex += 1;
        }

        SortRanges(segmentStarts, segmentEnds);

        if (segmentCount == 0)
        {
            return new ShapedClusterRanges(new int[0], new int[0]);
        }

        int mergedCount = 1;
        index = 1;
        while (index < segmentCount)
        {
            if (segmentStarts[index] < segmentEnds[mergedCount - 1])
            {
                if (segmentEnds[index] > segmentEnds[mergedCount - 1])
                {
                    segmentEnds[mergedCount - 1] = segmentEnds[index];
                }
            }
            else
            {
                segmentStarts[mergedCount] = segmentStarts[index];
                segmentEnds[mergedCount] = segmentEnds[index];
                mergedCount += 1;
            }

            index += 1;
        }

        int[] starts = new int[mergedCount];
        int[] ends = new int[mergedCount];
        System.Array.Copy(segmentStarts, starts, mergedCount);
        System.Array.Copy(segmentEnds, ends, mergedCount);
        return new ShapedClusterRanges(starts, ends);
    }

    static void SortRanges(int[] starts, int[] ends)
    {
        int index = 1;
        while (index < starts.Length)
        {
            int start = starts[index];
            int end = ends[index];
            int target = index;

            while (target > 0 &&
                (starts[target - 1] > start ||
                (starts[target - 1] == start && ends[target - 1] > end)))
            {
                starts[target] = starts[target - 1];
                ends[target] = ends[target - 1];
                target -= 1;
            }

            starts[target] = start;
            ends[target] = end;
            index += 1;
        }
    }

    static int[] AssignGlyphClusters(
        ShapingBuffer buffer,
        int[] clusterStarts,
        int[] clusterEnds)
    {
        int[] assignments = new int[buffer.GetCount()];
        int glyphIndex = 0;
        while (glyphIndex < buffer.GetCount())
        {
            int start = buffer.GetClusterStart(glyphIndex);
            int end = buffer.GetClusterEnd(glyphIndex);
            int clusterIndex = 0;

            while (clusterIndex < clusterStarts.Length &&
                (start < clusterStarts[clusterIndex] ||
                end > clusterEnds[clusterIndex]))
            {
                clusterIndex += 1;
            }

            if (clusterIndex == clusterStarts.Length)
            {
                throw new System.InvalidOperationException(
                    "A glyph could not be assigned to an output cluster.");
            }

            assignments[glyphIndex] = clusterIndex;
            glyphIndex += 1;
        }

        return assignments;
    }

    static int[][] GroupGlyphs(int[] assignments, int clusterCount)
    {
        int[] counts = new int[clusterCount];
        int index = 0;
        while (index < assignments.Length)
        {
            counts[assignments[index]] += 1;
            index += 1;
        }

        int[][] groups = new int[clusterCount][];
        index = 0;
        while (index < clusterCount)
        {
            groups[index] = new int[counts[index]];
            counts[index] = 0;
            index += 1;
        }

        index = 0;
        while (index < assignments.Length)
        {
            int clusterIndex = assignments[index];
            groups[clusterIndex][counts[clusterIndex]] = index;
            counts[clusterIndex] += 1;
            index += 1;
        }

        return groups;
    }

    static void BuildClusterMappings(
        bool forward,
        int[] logicalToVisual,
        int[] visualToLogical)
    {
        int visualIndex = 0;
        while (visualIndex < visualToLogical.Length)
        {
            int logicalIndex = forward ?
                visualIndex : visualToLogical.Length - visualIndex - 1;
            visualToLogical[visualIndex] = logicalIndex;
            logicalToVisual[logicalIndex] = visualIndex;
            visualIndex += 1;
        }
    }

    static void BuildGlyphMappings(
        int[][] clusterGlyphSources,
        int[] visualToLogicalClusters,
        int[] logicalToVisual,
        int[] visualToLogical,
        int[] logicalSources,
        int[] visualSources)
    {
        int logicalIndex = 0;
        int clusterIndex = 0;
        while (clusterIndex < clusterGlyphSources.Length)
        {
            int memberIndex = 0;
            while (memberIndex < clusterGlyphSources[clusterIndex].Length)
            {
                logicalSources[logicalIndex] =
                    clusterGlyphSources[clusterIndex][memberIndex];
                logicalIndex += 1;
                memberIndex += 1;
            }

            clusterIndex += 1;
        }

        int visualIndex = 0;
        int visualClusterIndex = 0;
        while (visualClusterIndex < visualToLogicalClusters.Length)
        {
            clusterIndex = visualToLogicalClusters[visualClusterIndex];
            int memberIndex = 0;
            while (memberIndex < clusterGlyphSources[clusterIndex].Length)
            {
                int sourceIndex =
                    clusterGlyphSources[clusterIndex][memberIndex];
                visualSources[visualIndex] = sourceIndex;
                int mappedLogicalIndex = FindIndex(
                    logicalSources,
                    sourceIndex);
                visualToLogical[visualIndex] = mappedLogicalIndex;
                logicalToVisual[mappedLogicalIndex] = visualIndex;
                visualIndex += 1;
                memberIndex += 1;
            }

            visualClusterIndex += 1;
        }
    }

    static int FindIndex(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }

            index += 1;
        }

        throw new System.InvalidOperationException(
            "A shaped glyph index could not be mapped.");
    }

    static int[][] BuildClusterScalars(
        RuneBuffer source,
        int[] clusterStarts,
        int[] clusterEnds)
    {
        int[][] scalars = new int[clusterStarts.Length][];
        int clusterIndex = 0;
        while (clusterIndex < clusterStarts.Length)
        {
            int count = 0;
            int runeIndex = 0;
            while (runeIndex < source.GetCount())
            {
                if (RangesOverlap(
                    clusterStarts[clusterIndex],
                    clusterEnds[clusterIndex],
                    source.GetUtf16Start(runeIndex),
                    source.GetUtf16End(runeIndex)))
                {
                    count += 1;
                }

                runeIndex += 1;
            }

            scalars[clusterIndex] = new int[count];
            runeIndex = 0;
            int scalarIndex = 0;
            while (runeIndex < source.GetCount())
            {
                if (RangesOverlap(
                    clusterStarts[clusterIndex],
                    clusterEnds[clusterIndex],
                    source.GetUtf16Start(runeIndex),
                    source.GetUtf16End(runeIndex)))
                {
                    scalars[clusterIndex][scalarIndex] =
                        source.GetScalarValue(runeIndex);
                    scalarIndex += 1;
                }

                runeIndex += 1;
            }

            clusterIndex += 1;
        }

        return scalars;
    }

    static ShapedGlyph[] BuildGlyphs(
        ShapingBuffer buffer,
        SfntGposPositionBuffer? positions,
        int[] glyphClusters,
        int[] clusterStarts,
        int[] clusterEnds,
        int[][] clusterScalars,
        int[] visualToLogical,
        int[] visualSources)
    {
        ShapedGlyph[] glyphs = new ShapedGlyph[buffer.GetCount()];
        int visualIndex = 0;
        while (visualIndex < glyphs.Length)
        {
            int sourceIndex = visualSources[visualIndex];
            int clusterIndex = glyphClusters[sourceIndex];
            int logicalIndex = visualToLogical[visualIndex];
            ShapedGlyphFlags flags = GetFlags(buffer, sourceIndex);
            double xAdvance = positions == null ? 0 :
                positions.GetXAdvance(sourceIndex);
            double yAdvance = positions == null ? 0 :
                positions.GetYAdvance(sourceIndex);
            double xOffset = positions == null ? 0 :
                positions.GetXPlacement(sourceIndex);
            double yOffset = positions == null ? 0 :
                positions.GetYPlacement(sourceIndex);
            glyphs[visualIndex] = new ShapedGlyph(
                buffer.GetGlyphId(sourceIndex),
                clusterIndex,
                clusterStarts[clusterIndex],
                clusterEnds[clusterIndex] - clusterStarts[clusterIndex],
                clusterScalars[clusterIndex],
                buffer.GetLigatureComponentCount(sourceIndex),
                buffer.GetLigatureComponentIndex(sourceIndex),
                logicalIndex,
                visualIndex,
                flags,
                xAdvance,
                yAdvance,
                xOffset,
                yOffset);
            visualIndex += 1;
        }

        return glyphs;
    }

    static ShapedGlyphFlags GetFlags(
        ShapingBuffer buffer,
        int index)
    {
        ShapedGlyphFlags flags = ShapedGlyphFlags.None;
        if (buffer.IsUnsafeToBreak(index))
        {
            flags = AddFlag(flags, ShapedGlyphFlags.UnsafeToBreak);
        }

        if (buffer.IsDefaultIgnorable(index))
        {
            flags = AddFlag(flags, ShapedGlyphFlags.DefaultIgnorable);
        }

        if (buffer.HasLigature(index))
        {
            flags = AddFlag(flags, ShapedGlyphFlags.Ligature);
        }

        if (buffer.GetAttachmentType(index) == ShapingAttachmentType.Mark)
        {
            flags = AddFlag(flags, ShapedGlyphFlags.MarkAttachment);
        }
        else if (buffer.GetAttachmentType(index) ==
            ShapingAttachmentType.Cursive)
        {
            flags = AddFlag(flags, ShapedGlyphFlags.CursiveAttachment);
        }

        return flags;
    }

    static ShapedGlyphFlags AddFlag(
        ShapedGlyphFlags flags,
        ShapedGlyphFlags value)
    {
        return (ShapedGlyphFlags)((int)flags | (int)value);
    }

    static ShapedCluster[] BuildClusters(
        ShapingBuffer buffer,
        int[] starts,
        int[] ends,
        int[][] scalars,
        int[][] clusterGlyphSources,
        int[] clusterVisualIndexes,
        int[] logicalToVisualGlyphs,
        int[] logicalGlyphSources)
    {
        ShapedCluster[] clusters = new ShapedCluster[starts.Length];
        int logicalIndex = 0;
        while (logicalIndex < clusters.Length)
        {
            int[] glyphIndexes =
                new int[clusterGlyphSources[logicalIndex].Length];
            bool unsafeToBreak = false;
            int memberIndex = 0;
            while (memberIndex < glyphIndexes.Length)
            {
                int sourceIndex =
                    clusterGlyphSources[logicalIndex][memberIndex];
                int glyphLogicalIndex = FindIndex(
                    logicalGlyphSources,
                    sourceIndex);
                glyphIndexes[memberIndex] =
                    logicalToVisualGlyphs[glyphLogicalIndex];
                unsafeToBreak = unsafeToBreak ||
                    buffer.IsUnsafeToBreak(sourceIndex);
                memberIndex += 1;
            }

            clusters[logicalIndex] = new ShapedCluster(
                starts[logicalIndex],
                ends[logicalIndex] - starts[logicalIndex],
                scalars[logicalIndex],
                glyphIndexes,
                logicalIndex,
                clusterVisualIndexes[logicalIndex],
                unsafeToBreak);
            logicalIndex += 1;
        }

        return clusters;
    }

    static bool RangesOverlap(
        int firstStart,
        int firstEnd,
        int secondStart,
        int secondEnd)
    {
        return firstStart < secondEnd && secondStart < firstEnd;
    }

    static void ValidateSourceRange(
        int start,
        int length,
        int sourceLength)
    {
        if (start < 0 || length <= 0 ||
            start > sourceLength || length > sourceLength - start)
        {
            throw new System.ArgumentException(
                "The source contains an invalid UTF-16 range.");
        }
    }
}
