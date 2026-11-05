namespace ttf;

internal class FontSubsetGlyphAssignment
{
    int[] oldToNewGlyphIds;
    int[] newToOldGlyphIds;

    internal FontSubsetGlyphAssignment(
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds)
    {
        this.oldToNewGlyphIds = CopyIntegers(oldToNewGlyphIds);
        this.newToOldGlyphIds = CopyIntegers(newToOldGlyphIds);
        ValidateBidirectionalMaps();
    }

    internal int[] CopyOldToNewGlyphIds()
    {
        return CopyIntegers(oldToNewGlyphIds);
    }

    internal int[] CopyNewToOldGlyphIds()
    {
        return CopyIntegers(newToOldGlyphIds);
    }

    internal int GetOutputGlyphCount()
    {
        return newToOldGlyphIds.Length;
    }

    void ValidateBidirectionalMaps()
    {
        int oldGlyphId = 0;

        while (oldGlyphId < oldToNewGlyphIds.Length)
        {
            int newGlyphId = oldToNewGlyphIds[oldGlyphId];

            if (newGlyphId < -1 ||
                newGlyphId >= newToOldGlyphIds.Length ||
                (newGlyphId >= 0 &&
                    newToOldGlyphIds[newGlyphId] != oldGlyphId))
            {
                throw new System.ArgumentException(
                    "Old-to-new and new-to-old glyph maps are inconsistent.");
            }

            oldGlyphId++;
        }

        int newGlyphIdIndex = 0;
        while (newGlyphIdIndex < newToOldGlyphIds.Length)
        {
            int mappedOldGlyphId = newToOldGlyphIds[newGlyphIdIndex];

            if (mappedOldGlyphId < -1 ||
                mappedOldGlyphId >= oldToNewGlyphIds.Length ||
                (mappedOldGlyphId >= 0 &&
                    oldToNewGlyphIds[mappedOldGlyphId] != newGlyphIdIndex))
            {
                throw new System.ArgumentException(
                    "New-to-old and old-to-new glyph maps are inconsistent.");
            }

            newGlyphIdIndex++;
        }
    }

    static int[] CopyIntegers(int[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal static class FontSubsetGlyphAssignmentBuilder
{
    internal static FontSubsetGlyphAssignment Create(
        int originalGlyphCount,
        int[] retainedGlyphIds,
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode retentionMode)
    {
        if (originalGlyphCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "originalGlyphCount");
        }

        if (retainedGlyphIds == null)
        {
            throw new System.ArgumentNullException("retainedGlyphIds");
        }

        ValidateModes(embeddingMode, retentionMode);

        if (embeddingMode == FontSubsetEmbeddingMode.FullFont)
        {
            return CreateIdentity(originalGlyphCount);
        }

        int[] sortedGlyphIds = CopyAndSortRetainedGlyphs(
            retainedGlyphIds,
            originalGlyphCount);

        if (sortedGlyphIds.Length == 0 || sortedGlyphIds[0] != 0)
        {
            throw FontFormatException.Create(
                "A subset glyph assignment must retain glyph zero.");
        }

        if (retentionMode == FontSubsetGlyphRetentionMode.Preserve)
        {
            return CreatePreserved(originalGlyphCount, sortedGlyphIds);
        }

        return CreateCompact(originalGlyphCount, sortedGlyphIds);
    }

    static FontSubsetGlyphAssignment CreateIdentity(int glyphCount)
    {
        int[] oldToNew = new int[glyphCount];
        int[] newToOld = new int[glyphCount];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            oldToNew[glyphId] = glyphId;
            newToOld[glyphId] = glyphId;
            glyphId++;
        }

        return new FontSubsetGlyphAssignment(oldToNew, newToOld);
    }

    static FontSubsetGlyphAssignment CreateCompact(
        int originalGlyphCount,
        int[] retainedGlyphIds)
    {
        int[] oldToNew = CreateMissingMap(originalGlyphCount);
        int[] newToOld = new int[retainedGlyphIds.Length];
        int newGlyphId = 0;

        while (newGlyphId < retainedGlyphIds.Length)
        {
            int oldGlyphId = retainedGlyphIds[newGlyphId];
            oldToNew[oldGlyphId] = newGlyphId;
            newToOld[newGlyphId] = oldGlyphId;
            newGlyphId++;
        }

        return new FontSubsetGlyphAssignment(oldToNew, newToOld);
    }

    static FontSubsetGlyphAssignment CreatePreserved(
        int originalGlyphCount,
        int[] retainedGlyphIds)
    {
        int outputGlyphCount = retainedGlyphIds[
            retainedGlyphIds.Length - 1] + 1;
        int[] oldToNew = CreateMissingMap(originalGlyphCount);
        int[] newToOld = CreateMissingMap(outputGlyphCount);
        int index = 0;

        while (index < retainedGlyphIds.Length)
        {
            int glyphId = retainedGlyphIds[index];
            oldToNew[glyphId] = glyphId;
            newToOld[glyphId] = glyphId;
            index++;
        }

        return new FontSubsetGlyphAssignment(oldToNew, newToOld);
    }

    static int[] CopyAndSortRetainedGlyphs(
        int[] retainedGlyphIds,
        int originalGlyphCount)
    {
        int[] sorted = new int[retainedGlyphIds.Length];
        System.Array.Copy(
            retainedGlyphIds,
            sorted,
            retainedGlyphIds.Length);
        System.Array.Sort(sorted);
        int index = 0;

        while (index < sorted.Length)
        {
            if (sorted[index] < 0 || sorted[index] >= originalGlyphCount)
            {
                throw new System.ArgumentOutOfRangeException(
                    "retainedGlyphIds");
            }

            if (index > 0 && sorted[index] == sorted[index - 1])
            {
                throw new System.ArgumentException(
                    "Retained glyph IDs must be unique.",
                    "retainedGlyphIds");
            }

            index++;
        }

        return sorted;
    }

    static int[] CreateMissingMap(int count)
    {
        int[] values = new int[count];
        System.Array.Fill(values, -1);
        return values;
    }

    static void ValidateModes(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode retentionMode)
    {
        if (embeddingMode < FontSubsetEmbeddingMode.Subset ||
            embeddingMode > FontSubsetEmbeddingMode.FullFont)
        {
            throw new System.ArgumentOutOfRangeException("embeddingMode");
        }

        if (retentionMode < FontSubsetGlyphRetentionMode.Compact ||
            retentionMode > FontSubsetGlyphRetentionMode.Preserve)
        {
            throw new System.ArgumentOutOfRangeException("retentionMode");
        }
    }
}
