namespace ttf;

internal class PdfCidAllocation
{
    int[] cidToSourceGlyphIds;
    int[] sourceGlyphToCids;
    int[] cidToSubsetGlyphIds;
    int[] subsetGlyphToCids;
    int[] oldToNewGlyphIds;
    int[] newToOldGlyphIds;

    internal PdfCidAllocation(
        int[] cidToSourceGlyphIds,
        int[] sourceGlyphToCids,
        int[] cidToSubsetGlyphIds,
        int[] subsetGlyphToCids,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds)
    {
        this.cidToSourceGlyphIds = CopyIntegers(cidToSourceGlyphIds);
        this.sourceGlyphToCids = CopyIntegers(sourceGlyphToCids);
        this.cidToSubsetGlyphIds = CopyIntegers(cidToSubsetGlyphIds);
        this.subsetGlyphToCids = CopyIntegers(subsetGlyphToCids);
        this.oldToNewGlyphIds = CopyIntegers(oldToNewGlyphIds);
        this.newToOldGlyphIds = CopyIntegers(newToOldGlyphIds);
    }

    internal int GetCidCount()
    {
        return cidToSourceGlyphIds.Length;
    }

    internal int GetSourceGlyphId(int cid)
    {
        ValidateIndex(cid, cidToSourceGlyphIds.Length, "cid");
        return cidToSourceGlyphIds[cid];
    }

    internal int FindCidForSourceGlyph(int glyphId)
    {
        ValidateIndex(glyphId, sourceGlyphToCids.Length, "glyphId");
        return sourceGlyphToCids[glyphId];
    }

    internal int GetSubsetGlyphId(int cid)
    {
        ValidateIndex(cid, cidToSubsetGlyphIds.Length, "cid");
        return cidToSubsetGlyphIds[cid];
    }

    internal int FindCidForSubsetGlyph(int glyphId)
    {
        ValidateIndex(glyphId, subsetGlyphToCids.Length, "glyphId");
        return subsetGlyphToCids[glyphId];
    }

    internal int[] CopyCidToSourceGlyphIds()
    {
        return CopyIntegers(cidToSourceGlyphIds);
    }

    internal int[] CopySourceGlyphToCids()
    {
        return CopyIntegers(sourceGlyphToCids);
    }

    internal int[] CopyCidToSubsetGlyphIds()
    {
        return CopyIntegers(cidToSubsetGlyphIds);
    }

    internal int[] CopySubsetGlyphToCids()
    {
        return CopyIntegers(subsetGlyphToCids);
    }

    internal int[] CopyOldToNewGlyphIds()
    {
        return CopyIntegers(oldToNewGlyphIds);
    }

    internal int[] CopyNewToOldGlyphIds()
    {
        return CopyIntegers(newToOldGlyphIds);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static void ValidateIndex(
        int index,
        int count,
        string parameterName)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal class PdfCidAllocator
{
    int sourceGlyphCount;
    PdfCidAllocationMode mode;
    int[] sourceGlyphToCids;
    System.Collections.Generic.List<int> cidToSourceGlyphIds;
    bool frozen;

    internal PdfCidAllocator(
        int sourceGlyphCount,
        PdfCidAllocationMode mode)
    {
        if (sourceGlyphCount <= 0 || sourceGlyphCount > ushort.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException(
                "sourceGlyphCount");
        }

        this.sourceGlyphCount = sourceGlyphCount;
        this.mode = mode;
        sourceGlyphToCids = new int[sourceGlyphCount];
        System.Array.Fill(sourceGlyphToCids, -1);
        cidToSourceGlyphIds =
            new System.Collections.Generic.List<int>();
        sourceGlyphToCids[0] = 0;
        cidToSourceGlyphIds.Add(0);
        frozen = false;
    }

    internal static PdfCidAllocator Create(PdfFontUsage usage)
    {
        if (usage == null)
        {
            throw new System.ArgumentNullException("usage");
        }

        PdfCidAllocator allocator = new PdfCidAllocator(
            usage.GetInstance().GetGlyphCount(),
            usage.CopyOptions().GetCidAllocationMode());
        int[] glyphIds = usage.CopyGlyphIdsInUseOrder();
        int index = 0;

        while (index < glyphIds.Length)
        {
            allocator.GetOrAddCid(glyphIds[index]);
            index += 1;
        }

        return allocator;
    }

    internal static PdfCidAllocator Create(int sourceGlyphCount)
    {
        return new PdfCidAllocator(
            sourceGlyphCount,
            PdfCidAllocationMode.Dense);
    }

    internal int GetOrAddCid(int sourceGlyphId)
    {
        ValidateSourceGlyphId(sourceGlyphId);
        int existing = sourceGlyphToCids[sourceGlyphId];

        if (existing >= 0)
        {
            return existing;
        }

        if (frozen)
        {
            throw new System.InvalidOperationException(
                "PDF character-code allocation has been finalized.");
        }

        if (mode == PdfCidAllocationMode.PreserveSourceGlyphIdentifiers)
        {
            while (cidToSourceGlyphIds.Count <= sourceGlyphId)
            {
                cidToSourceGlyphIds.Add(-1);
            }

            sourceGlyphToCids[sourceGlyphId] = sourceGlyphId;
            cidToSourceGlyphIds[sourceGlyphId] = sourceGlyphId;
            return sourceGlyphId;
        }

        if (cidToSourceGlyphIds.Count > ushort.MaxValue)
        {
            throw FontLimitException.Create(
                "PDF font usage exceeds the 16-bit CID space.");
        }

        int cid = cidToSourceGlyphIds.Count;
        sourceGlyphToCids[sourceGlyphId] = cid;
        cidToSourceGlyphIds.Add(sourceGlyphId);
        return cid;
    }

    internal int[] GetOrAddCids(
        int[] sourceGlyphIds,
        int maximumCid)
    {
        if (sourceGlyphIds == null)
        {
            throw new System.ArgumentNullException("sourceGlyphIds");
        }

        if (maximumCid < 0 || maximumCid > ushort.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("maximumCid");
        }

        System.Collections.Generic.HashSet<int> missing =
            new System.Collections.Generic.HashSet<int>();
        int index = 0;

        while (index < sourceGlyphIds.Length)
        {
            int glyphId = sourceGlyphIds[index];
            ValidateSourceGlyphId(glyphId);

            if (sourceGlyphToCids[glyphId] < 0)
            {
                missing.Add(glyphId);
            }

            index += 1;
        }

        if (frozen && missing.Count != 0)
        {
            throw new System.InvalidOperationException(
                "PDF character-code allocation has been finalized.");
        }

        if (mode == PdfCidAllocationMode.Dense &&
            (long)cidToSourceGlyphIds.Count + missing.Count - 1 >
                maximumCid)
        {
            throw FontLimitException.Create(
                "PDF font usage exceeds the selected character-code space.");
        }

        if (mode == PdfCidAllocationMode.PreserveSourceGlyphIdentifiers)
        {
            foreach (int missingGlyphId in missing)
            {
                if (missingGlyphId > maximumCid)
                {
                    throw FontLimitException.Create(
                        "PDF font usage exceeds the selected character-code space.");
                }
            }
        }

        int[] cids = new int[sourceGlyphIds.Length];
        index = 0;

        while (index < sourceGlyphIds.Length)
        {
            cids[index] = GetOrAddCid(sourceGlyphIds[index]);
            index += 1;
        }

        return cids;
    }

    internal int GetSourceGlyphCount()
    {
        return sourceGlyphCount;
    }

    internal int GetCount()
    {
        return cidToSourceGlyphIds.Count;
    }

    internal int[] CopySourceGlyphIdsInCidOrder()
    {
        return cidToSourceGlyphIds.ToArray();
    }

    internal int[] CopyAllocatedSourceGlyphIdsInCidOrder()
    {
        System.Collections.Generic.List<int> allocated =
            new System.Collections.Generic.List<int>();
        int cid = 0;

        while (cid < cidToSourceGlyphIds.Count)
        {
            int sourceGlyphId = cidToSourceGlyphIds[cid];
            if (sourceGlyphId >= 0)
            {
                allocated.Add(sourceGlyphId);
            }
            cid += 1;
        }

        return allocated.ToArray();
    }

    internal int[] CopyAllocatedCids()
    {
        System.Collections.Generic.List<int> allocated =
            new System.Collections.Generic.List<int>();
        int cid = 0;

        while (cid < cidToSourceGlyphIds.Count)
        {
            if (cidToSourceGlyphIds[cid] >= 0)
            {
                allocated.Add(cid);
            }
            cid += 1;
        }

        return allocated.ToArray();
    }

    internal void Freeze()
    {
        frozen = true;
    }

    internal bool IsFrozen()
    {
        return frozen;
    }

    internal PdfCidAllocation BindSubsetGlyphs(
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds)
    {
        ValidateSubsetMaps(oldToNewGlyphIds, newToOldGlyphIds);
        int[] cidToSource = cidToSourceGlyphIds.ToArray();
        int[] cidToSubset = new int[cidToSource.Length];
        int[] subsetToCid = new int[newToOldGlyphIds.Length];
        System.Array.Fill(subsetToCid, -1);
        int cid = 0;

        while (cid < cidToSource.Length)
        {
            int sourceGlyphId = cidToSource[cid];

            if (sourceGlyphId < 0)
            {
                cidToSubset[cid] = 0;
                cid += 1;
                continue;
            }

            int subsetGlyphId = oldToNewGlyphIds[sourceGlyphId];

            if (subsetGlyphId < 0)
            {
                throw new System.ArgumentException(
                    "A used source glyph is missing from the subset glyph map.",
                    "oldToNewGlyphIds");
            }

            cidToSubset[cid] = subsetGlyphId;
            subsetToCid[subsetGlyphId] = cid;
            cid += 1;
        }

        return new PdfCidAllocation(
            cidToSource,
            sourceGlyphToCids,
            cidToSubset,
            subsetToCid,
            oldToNewGlyphIds,
            newToOldGlyphIds);
    }

    void ValidateSourceGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= sourceGlyphCount)
        {
            throw new System.ArgumentOutOfRangeException("sourceGlyphId");
        }
    }

    void ValidateSubsetMaps(
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds)
    {
        if (oldToNewGlyphIds == null || newToOldGlyphIds == null)
        {
            throw new System.ArgumentNullException("glyphMaps");
        }

        if (oldToNewGlyphIds.Length != sourceGlyphCount ||
            newToOldGlyphIds.Length == 0 ||
            oldToNewGlyphIds[0] != 0 ||
            newToOldGlyphIds[0] != 0)
        {
            throw new System.ArgumentException(
                "Subset glyph maps must reserve source and subset glyph zero.",
                "oldToNewGlyphIds");
        }

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
                    "Subset glyph maps are inconsistent.",
                    "oldToNewGlyphIds");
            }

            oldGlyphId += 1;
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
                    "Subset glyph maps are inconsistent.",
                    "newToOldGlyphIds");
            }

            newGlyphIdIndex += 1;
        }
    }
}
