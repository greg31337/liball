namespace ttf;

internal enum SfntLayoutTraversalDirection
{
    Forward = 0,
    Backward = 1
}

internal class SfntLayoutGlyphBuffer
{
    int[] glyphIds;
    int[] clusterStarts;
    int[] clusterLengths;
    int[] ligatureComponentCounts;
    int[] ligatureComponentIndexes;

    internal static SfntLayoutGlyphBuffer Create(int[] glyphIds)
    {
        if (glyphIds == null)
        {
            throw new System.ArgumentNullException("glyphIds");
        }

        int[] clusterStarts = new int[glyphIds.Length];
        int[] clusterLengths = new int[glyphIds.Length];
        int[] ligatureComponentCounts = new int[glyphIds.Length];
        int[] ligatureComponentIndexes = new int[glyphIds.Length];
        int index = 0;

        while (index < glyphIds.Length)
        {
            clusterStarts[index] = index;
            clusterLengths[index] = 1;
            ligatureComponentCounts[index] = 1;
            ligatureComponentIndexes[index] = 0;
            index += 1;
        }

        return new SfntLayoutGlyphBuffer(
            glyphIds,
            clusterStarts,
            clusterLengths,
            ligatureComponentCounts,
            ligatureComponentIndexes);
    }

    internal SfntLayoutGlyphBuffer(
        int[] glyphIds,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] ligatureComponentCounts,
        int[] ligatureComponentIndexes)
    {
        if (glyphIds == null ||
            clusterStarts == null ||
            clusterLengths == null ||
            ligatureComponentCounts == null ||
            ligatureComponentIndexes == null)
        {
            throw new System.ArgumentNullException("glyphIds");
        }

        int count = glyphIds.Length;

        if (clusterStarts.Length != count ||
            clusterLengths.Length != count ||
            ligatureComponentCounts.Length != count ||
            ligatureComponentIndexes.Length != count)
        {
            throw new System.ArgumentException(
                "Glyph-buffer metadata arrays must have equal lengths.");
        }

        this.glyphIds = new int[count];
        this.clusterStarts = new int[count];
        this.clusterLengths = new int[count];
        this.ligatureComponentCounts = new int[count];
        this.ligatureComponentIndexes = new int[count];
        int index = 0;

        while (index < count)
        {
            ValidateGlyphId(glyphIds[index]);

            if (clusterStarts[index] < 0 ||
                clusterLengths[index] <= 0 ||
                clusterLengths[index] >
                int.MaxValue - clusterStarts[index] ||
                ligatureComponentCounts[index] <= 0 ||
                ligatureComponentIndexes[index] < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    "clusterStarts");
            }

            this.glyphIds[index] = glyphIds[index];
            this.clusterStarts[index] = clusterStarts[index];
            this.clusterLengths[index] = clusterLengths[index];
            this.ligatureComponentCounts[index] =
                ligatureComponentCounts[index];
            this.ligatureComponentIndexes[index] =
                ligatureComponentIndexes[index];
            index += 1;
        }
    }

    internal static SfntLayoutGlyphBuffer Create(
        int[] glyphIds,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] ligatureComponentCounts,
        int[] ligatureComponentIndexes)
    {
        return new SfntLayoutGlyphBuffer(
            glyphIds,
            clusterStarts,
            clusterLengths,
            ligatureComponentCounts,
            ligatureComponentIndexes);
    }

    internal int GetCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int index)
    {
        ValidateIndex(index);
        return glyphIds[index];
    }

    internal void SetGlyphId(int index, int glyphId)
    {
        ValidateIndex(index);
        ValidateGlyphId(glyphId);
        glyphIds[index] = glyphId;
    }

    internal int GetClusterStart(int index)
    {
        ValidateIndex(index);
        return clusterStarts[index];
    }

    internal int GetClusterLength(int index)
    {
        ValidateIndex(index);
        return clusterLengths[index];
    }

    internal int GetLigatureComponentCount(int index)
    {
        ValidateIndex(index);
        return ligatureComponentCounts[index];
    }

    internal int GetLigatureComponentIndex(int index)
    {
        ValidateIndex(index);
        return ligatureComponentIndexes[index];
    }

    internal void ReplaceWithSequence(int index, int[] substituteGlyphIds)
    {
        ValidateIndex(index);

        if (substituteGlyphIds == null)
        {
            throw new System.ArgumentNullException("substituteGlyphIds");
        }

        if (substituteGlyphIds.Length == 0)
        {
            throw new System.ArgumentException(
                "A multiple substitution cannot delete a glyph.");
        }

        int substituteIndex = 0;

        while (substituteIndex < substituteGlyphIds.Length)
        {
            ValidateGlyphId(substituteGlyphIds[substituteIndex]);
            substituteIndex += 1;
        }

        int replacementCount = substituteGlyphIds.Length;
        int newCount = CheckedFontMath.AddCounts(
            glyphIds.Length - 1,
            replacementCount);
        int[] newGlyphIds = new int[newCount];
        int[] newClusterStarts = new int[newCount];
        int[] newClusterLengths = new int[newCount];
        int[] newComponentCounts = new int[newCount];
        int[] newComponentIndexes = new int[newCount];
        int oldIndex = 0;
        int newIndex = 0;

        while (oldIndex < glyphIds.Length)
        {
            if (oldIndex == index)
            {
                substituteIndex = 0;

                while (substituteIndex < replacementCount)
                {
                    newGlyphIds[newIndex] =
                        substituteGlyphIds[substituteIndex];
                    newClusterStarts[newIndex] = clusterStarts[oldIndex];
                    newClusterLengths[newIndex] = clusterLengths[oldIndex];
                    newComponentCounts[newIndex] = 1;
                    newComponentIndexes[newIndex] =
                        CheckedFontMath.AddCounts(
                            ligatureComponentIndexes[oldIndex],
                            substituteIndex);
                    substituteIndex += 1;
                    newIndex += 1;
                }
            }
            else
            {
                CopyGlyph(
                    oldIndex,
                    newIndex,
                    newGlyphIds,
                    newClusterStarts,
                    newClusterLengths,
                    newComponentCounts,
                    newComponentIndexes);
                newIndex += 1;
            }

            oldIndex += 1;
        }

        SetArrays(
            newGlyphIds,
            newClusterStarts,
            newClusterLengths,
            newComponentCounts,
            newComponentIndexes);
    }

    internal int ReplaceWithLigature(
        int[] matchedIndices,
        int ligatureGlyphId)
    {
        if (matchedIndices == null)
        {
            throw new System.ArgumentNullException("matchedIndices");
        }

        if (matchedIndices.Length == 0)
        {
            throw new System.ArgumentException(
                "A ligature substitution requires matched glyphs.");
        }

        ValidateGlyphId(ligatureGlyphId);
        bool[] matched = new bool[glyphIds.Length];
        int minimumIndex = glyphIds.Length;
        int maximumIndex = -1;
        int totalComponentCount = 0;
        int matchIndex = 0;
        int matchDirection = 0;

        while (matchIndex < matchedIndices.Length)
        {
            int matchedIndex = matchedIndices[matchIndex];
            ValidateIndex(matchedIndex);

            if (matched[matchedIndex])
            {
                throw new System.ArgumentException(
                    "Ligature match indices must be unique.");
            }

            if (matchIndex > 0)
            {
                int currentDirection = matchedIndex >
                    matchedIndices[matchIndex - 1] ? 1 : -1;

                if (matchDirection == 0)
                {
                    matchDirection = currentDirection;
                }
                else if (currentDirection != matchDirection)
                {
                    throw new System.ArgumentException(
                        "Ligature match indices must be monotonic.");
                }
            }

            matched[matchedIndex] = true;
            minimumIndex = System.Math.Min(minimumIndex, matchedIndex);
            maximumIndex = System.Math.Max(maximumIndex, matchedIndex);
            totalComponentCount = CheckedFontMath.AddCounts(
                totalComponentCount,
                ligatureComponentCounts[matchedIndex]);
            matchIndex += 1;
        }

        int mergedClusterStart = clusterStarts[minimumIndex];
        int mergedClusterEnd = CheckedFontMath.AddOffset(
            clusterStarts[minimumIndex],
            clusterLengths[minimumIndex]);
        int spanIndex = minimumIndex + 1;

        while (spanIndex <= maximumIndex)
        {
            mergedClusterStart = System.Math.Min(
                mergedClusterStart,
                clusterStarts[spanIndex]);
            mergedClusterEnd = System.Math.Max(
                mergedClusterEnd,
                CheckedFontMath.AddOffset(
                    clusterStarts[spanIndex],
                    clusterLengths[spanIndex]));
            spanIndex += 1;
        }

        AssignSkippedComponentIndexes(matchedIndices, matched);
        int firstIndex = matchedIndices[0];
        int newCount = glyphIds.Length - matchedIndices.Length + 1;
        int[] newGlyphIds = new int[newCount];
        int[] newClusterStarts = new int[newCount];
        int[] newClusterLengths = new int[newCount];
        int[] newComponentCounts = new int[newCount];
        int[] newComponentIndexes = new int[newCount];
        int oldIndex = 0;
        int newIndex = 0;
        int ligatureIndex = -1;

        while (oldIndex < glyphIds.Length)
        {
            if (oldIndex == firstIndex)
            {
                ligatureIndex = newIndex;
                newGlyphIds[newIndex] = ligatureGlyphId;
                newClusterStarts[newIndex] = mergedClusterStart;
                newClusterLengths[newIndex] =
                    mergedClusterEnd - mergedClusterStart;
                newComponentCounts[newIndex] = totalComponentCount;
                newComponentIndexes[newIndex] =
                    ligatureComponentIndexes[oldIndex];
                newIndex += 1;
            }
            else if (!matched[oldIndex])
            {
                CopyGlyph(
                    oldIndex,
                    newIndex,
                    newGlyphIds,
                    newClusterStarts,
                    newClusterLengths,
                    newComponentCounts,
                    newComponentIndexes);

                if (oldIndex >= minimumIndex && oldIndex <= maximumIndex)
                {
                    newClusterStarts[newIndex] = mergedClusterStart;
                    newClusterLengths[newIndex] =
                        mergedClusterEnd - mergedClusterStart;
                }

                newIndex += 1;
            }

            oldIndex += 1;
        }

        SetArrays(
            newGlyphIds,
            newClusterStarts,
            newClusterLengths,
            newComponentCounts,
            newComponentIndexes);
        return ligatureIndex;
    }

    void AssignSkippedComponentIndexes(
        int[] matchedIndices,
        bool[] matched)
    {
        int cumulativeComponentCount = 0;
        int matchIndex = 0;

        while (matchIndex < matchedIndices.Length - 1)
        {
            int currentIndex = matchedIndices[matchIndex];
            int nextIndex = matchedIndices[matchIndex + 1];
            cumulativeComponentCount = CheckedFontMath.AddCounts(
                cumulativeComponentCount,
                ligatureComponentCounts[currentIndex]);
            int step = nextIndex > currentIndex ? 1 : -1;
            int skippedIndex = currentIndex + step;

            while (skippedIndex != nextIndex)
            {
                if (!matched[skippedIndex])
                {
                    ligatureComponentIndexes[skippedIndex] =
                        cumulativeComponentCount - 1;
                }

                skippedIndex += step;
            }

            matchIndex += 1;
        }
    }

    void CopyGlyph(
        int sourceIndex,
        int targetIndex,
        int[] targetGlyphIds,
        int[] targetClusterStarts,
        int[] targetClusterLengths,
        int[] targetComponentCounts,
        int[] targetComponentIndexes)
    {
        targetGlyphIds[targetIndex] = glyphIds[sourceIndex];
        targetClusterStarts[targetIndex] = clusterStarts[sourceIndex];
        targetClusterLengths[targetIndex] = clusterLengths[sourceIndex];
        targetComponentCounts[targetIndex] =
            ligatureComponentCounts[sourceIndex];
        targetComponentIndexes[targetIndex] =
            ligatureComponentIndexes[sourceIndex];
    }

    void SetArrays(
        int[] newGlyphIds,
        int[] newClusterStarts,
        int[] newClusterLengths,
        int[] newComponentCounts,
        int[] newComponentIndexes)
    {
        glyphIds = newGlyphIds;
        clusterStarts = newClusterStarts;
        clusterLengths = newClusterLengths;
        ligatureComponentCounts = newComponentCounts;
        ligatureComponentIndexes = newComponentIndexes;
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    static void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal class SfntLookupGlyphFilter
{
    SfntLayoutLookup lookup;
    SfntGdefTable? gdef;
    SfntGdefMarkGlyphSets? markGlyphSets;
    int markAttachmentType;

    internal SfntLookupGlyphFilter(
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef)
    {
        if (lookup == null)
        {
            throw new System.ArgumentNullException("lookup");
        }

        this.lookup = lookup;
        this.gdef = gdef;
        markAttachmentType = lookup.GetMarkAttachmentType();
        ValidateDependencies();
    }

    internal bool IsIgnored(int glyphId)
    {
        if (glyphId < 0 || glyphId > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        int glyphClass = gdef == null ? 0 :
            gdef.GetGlyphClass(glyphId);

        if (glyphClass == 1)
        {
            return lookup.IgnoresBaseGlyphs();
        }

        if (glyphClass == 2)
        {
            return lookup.IgnoresLigatures();
        }

        if (glyphClass != 3)
        {
            return false;
        }

        if (lookup.IgnoresMarks())
        {
            return true;
        }

        if (lookup.UsesMarkFilteringSet())
        {
            System.ArgumentNullException.ThrowIfNull(markGlyphSets);
            return !markGlyphSets.Covers(
                lookup.GetMarkFilteringSet(),
                glyphId);
        }

        if (markAttachmentType != 0)
        {
            System.ArgumentNullException.ThrowIfNull(gdef);
            return gdef.GetMarkAttachmentClass(glyphId) !=
                markAttachmentType;
        }

        return false;
    }

    void ValidateDependencies()
    {
        bool usesGlyphClasses = lookup.IgnoresBaseGlyphs() ||
            lookup.IgnoresLigatures() ||
            lookup.IgnoresMarks() ||
            lookup.UsesMarkFilteringSet() ||
            markAttachmentType != 0;

        if (usesGlyphClasses &&
            (gdef == null || gdef.GetGlyphClasses() == null))
        {
            throw FontFormatException.Create(
                "A lookup glyph filter requires GDEF glyph classes.");
        }

        if (markAttachmentType != 0 &&
            (gdef == null || gdef.GetMarkAttachmentClasses() == null))
        {
            throw FontFormatException.Create(
                "A lookup mark attachment filter requires GDEF mark attachment classes.");
        }

        if (lookup.UsesMarkFilteringSet())
        {
            System.ArgumentNullException.ThrowIfNull(gdef);
            markGlyphSets = gdef.GetMarkGlyphSets();

            if (markGlyphSets == null)
            {
                throw FontFormatException.Create(
                    "A lookup mark filtering set requires GDEF mark glyph sets.");
            }

            int setIndex = lookup.GetMarkFilteringSet();

            if (setIndex < 0 || setIndex >= markGlyphSets.GetSetCount())
            {
                throw FontFormatException.Create(
                    "A lookup mark filtering set index is invalid.");
            }
        }
    }
}

internal class SfntLookupGlyphIterator
{
    SfntLayoutGlyphBuffer? buffer;
    SfntLookupGlyphFilter? filter;
    SfntLayoutTraversalDirection direction;
    int minimumIndex;
    int maximumIndex;
    int index;
    bool hasCurrent;

    internal static SfntLookupGlyphIterator Create(
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        return new SfntLookupGlyphIterator(
            buffer,
            lookup,
            gdef,
            direction,
            0,
            buffer.GetCount());
    }

    internal SfntLookupGlyphIterator(
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int minimumIndex,
        int maximumIndex)
    {
        Initialize(
            buffer,
            lookup,
            gdef,
            direction,
            minimumIndex,
            maximumIndex);
    }

    internal static SfntLookupGlyphIterator Create(
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int minimumIndex,
        int maximumIndex)
    {
        return new SfntLookupGlyphIterator(
            buffer,
            lookup,
            gdef,
            direction,
            minimumIndex,
            maximumIndex);
    }

    void Initialize(
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutLookup lookup,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int minimumIndex,
        int maximumIndex)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        ValidateDirection(direction);

        if (minimumIndex < 0 ||
            maximumIndex < minimumIndex ||
            maximumIndex > buffer.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("minimumIndex");
        }

        this.buffer = buffer;
        filter = new SfntLookupGlyphFilter(lookup, gdef);
        this.direction = direction;
        this.minimumIndex = minimumIndex;
        this.maximumIndex = maximumIndex;
        ResetToBoundary();
    }

    internal SfntLayoutTraversalDirection GetDirection()
    {
        return direction;
    }

    internal int GetMinimumIndex()
    {
        return minimumIndex;
    }

    internal int GetMaximumIndex()
    {
        return maximumIndex;
    }

    internal bool HasCurrent()
    {
        return hasCurrent;
    }

    internal int GetIndex()
    {
        ValidateCurrent();
        return index;
    }

    internal int GetGlyphId()
    {
        ValidateCurrent();
        System.ArgumentNullException.ThrowIfNull(buffer);
        return buffer.GetGlyphId(index);
    }

    internal bool IsIgnored(int glyphIndex)
    {
        if (glyphIndex < minimumIndex || glyphIndex >= maximumIndex)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        System.ArgumentNullException.ThrowIfNull(buffer);
        System.ArgumentNullException.ThrowIfNull(filter);
        return filter.IsIgnored(buffer.GetGlyphId(glyphIndex));
    }

    internal void ResetToBoundary()
    {
        index = direction == SfntLayoutTraversalDirection.Forward ?
            minimumIndex - 1 : maximumIndex;
        hasCurrent = false;
    }

    internal void Reset(int anchorIndex)
    {
        if (anchorIndex < minimumIndex - 1 ||
            anchorIndex > maximumIndex)
        {
            throw new System.ArgumentOutOfRangeException("anchorIndex");
        }

        index = anchorIndex;
        hasCurrent = false;
    }

    internal bool MoveNext()
    {
        int step = direction == SfntLayoutTraversalDirection.Forward ?
            1 : -1;
        int candidate = index + step;

        while (candidate >= minimumIndex && candidate < maximumIndex)
        {
            System.ArgumentNullException.ThrowIfNull(buffer);
            System.ArgumentNullException.ThrowIfNull(filter);
            if (!filter.IsIgnored(buffer.GetGlyphId(candidate)))
            {
                index = candidate;
                hasCurrent = true;
                return true;
            }

            candidate += step;
        }

        index = direction == SfntLayoutTraversalDirection.Forward ?
            maximumIndex : minimumIndex - 1;
        hasCurrent = false;
        return false;
    }

    static void ValidateDirection(
        SfntLayoutTraversalDirection direction)
    {
        if (direction != SfntLayoutTraversalDirection.Forward &&
            direction != SfntLayoutTraversalDirection.Backward)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }
    }

    void ValidateCurrent()
    {
        if (!hasCurrent)
        {
            throw new System.InvalidOperationException(
                "The lookup glyph iterator is not positioned on a glyph.");
        }
    }
}
