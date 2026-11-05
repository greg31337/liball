namespace ttf;

internal class SfntCmapCoverageRange
{
    int startUnicodeValue;
    int endUnicodeValue;

    internal SfntCmapCoverageRange(int startUnicodeValue, int endUnicodeValue)
    {
        this.startUnicodeValue = startUnicodeValue;
        this.endUnicodeValue = endUnicodeValue;
    }

    internal int GetStartUnicodeValue()
    {
        return startUnicodeValue;
    }

    internal int GetEndUnicodeValue()
    {
        return endUnicodeValue;
    }

    internal bool ContainsUnicodeValue(int unicodeValue)
    {
        return unicodeValue >= startUnicodeValue &&
            unicodeValue <= endUnicodeValue;
    }
}

internal class SfntCmapMapping
{
    SfntCmapEncodingRecord[] sources;
    int[] sourceRanks;
    bool symbol;
    SfntCmapFormat14Data? variations;
    SfntCmapCoverageRange[] coverageRanges;
    int glyphCount;
    int maximumCandidateCount;

    internal SfntCmapMapping(
        SfntCmapEncodingRecord[] sources,
        int[] sourceRanks,
        bool symbol,
        SfntCmapFormat14Data? variations,
        SfntCmapCoverageRange[] coverageRanges,
        int glyphCount,
        int maximumCandidateCount)
    {
        this.sources = sources;
        this.sourceRanks = sourceRanks;
        this.symbol = symbol;
        this.variations = variations;
        this.coverageRanges = coverageRanges;
        this.glyphCount = glyphCount;
        this.maximumCandidateCount = maximumCandidateCount;
    }

    internal int GetSourceCount()
    {
        return sources.Length;
    }

    internal SfntCmapEncodingRecord GetSource(int index)
    {
        if (index < 0 || index >= sources.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sources[index];
    }

    internal int GetSourceRank(int index)
    {
        if (index < 0 || index >= sourceRanks.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sourceRanks[index];
    }

    internal bool IsSymbolMapping()
    {
        return symbol;
    }

    internal SfntCmapFormat14Data? GetVariations()
    {
        return variations;
    }

    internal int MapUnicode(int unicodeValue)
    {
        if (!IsUnicodeScalar(unicodeValue))
        {
            return 0;
        }

        int index = 0;

        while (index < sources.Length)
        {
            int glyphId = MapSourceUnicode(index, unicodeValue);

            if (glyphId == 0 && symbol && unicodeValue <= 0xFF)
            {
                glyphId = MapSourceUnicode(index, 0xF000 + unicodeValue);
            }

            if (glyphId != 0)
            {
                return glyphId;
            }

            index += 1;
        }

        return 0;
    }

    internal int MapVariation(int unicodeValue, int variationSelector)
    {
        if (variations == null || !IsUnicodeScalar(unicodeValue))
        {
            return 0;
        }

        SfntCmapVariationSelectorRecord? selector =
            variations.FindSelectorRecord(variationSelector);

        if (selector == null)
        {
            return 0;
        }

        SfntCmapVariationMapping? mapping =
            selector.FindNonDefaultMapping(unicodeValue);

        if (mapping != null)
        {
            return mapping.GetGlyphId();
        }

        if (selector.IsDefaultUnicodeValue(unicodeValue))
        {
            return MapUnicode(unicodeValue);
        }

        return 0;
    }

    internal int GetCoverageRangeCount()
    {
        return coverageRanges.Length;
    }

    internal SfntCmapCoverageRange GetCoverageRange(int index)
    {
        if (index < 0 || index >= coverageRanges.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return coverageRanges[index];
    }

    internal bool CoversUnicodeValue(int unicodeValue)
    {
        int low = 0;
        int high = coverageRanges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapCoverageRange range = coverageRanges[middle];

            if (unicodeValue < range.GetStartUnicodeValue())
            {
                high = middle - 1;
            }
            else if (unicodeValue > range.GetEndUnicodeValue())
            {
                low = middle + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    internal int[] GetUnicodeCandidates(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        if (glyphId == 0 || sources.Length == 0)
        {
            return new int[0];
        }

        System.Collections.Generic.List<int> candidates =
            new System.Collections.Generic.List<int>();
        int sourceIndex = 0;

        while (sourceIndex < sources.Length)
        {
            if (symbol)
            {
                CollectSymbolCandidates(sourceIndex, glyphId, candidates);
            }

            CollectSourceCandidates(sourceIndex, glyphId, candidates);
            sourceIndex += 1;
        }

        return candidates.ToArray();
    }

    void CollectSymbolCandidates(
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        int unicodeValue = 0;

        while (unicodeValue <= 0xFF)
        {
            if (MapUnicode(unicodeValue) == glyphId)
            {
                AddEffectiveCandidate(
                    sourceIndex,
                    glyphId,
                    unicodeValue,
                    candidates);
            }

            unicodeValue += 1;
        }
    }

    void CollectSourceCandidates(
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        SfntCmapEncodingRecord record = sources[sourceIndex];
        int codePage = LegacyEncodingData.GetCodePage(
            record.GetPlatformId(),
            record.GetEncodingId());
        SfntCmapSubtable subtable = record.GetSubtable();

        if (LegacyEncodingData.IsCodePageSupported(codePage))
        {
            CollectLegacyCandidates(
                sourceIndex,
                glyphId,
                codePage,
                candidates);
            return;
        }

        int format = subtable.GetFormat();

        if (format == 0)
        {
            CollectScannedCandidates(
                subtable,
                0,
                0xFF,
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 2 || format == 4)
        {
            CollectScannedCandidates(
                subtable,
                0,
                0xFFFF,
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 6)
        {
            SfntCmapFormat6Data? data = subtable.GetFormat6();
            System.ArgumentNullException.ThrowIfNull(data);
            CollectScannedCandidates(
                subtable,
                data.GetFirstCode(),
                data.GetFirstCode() + data.GetEntryCount() - 1L,
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 8)
        {
            CollectSequentialGroupCandidates(
                subtable.GetFormat8(),
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 10)
        {
            SfntCmapFormat10Data? data = subtable.GetFormat10();
            System.ArgumentNullException.ThrowIfNull(data);
            CollectScannedCandidates(
                subtable,
                data.GetStartCharacterCode(),
                data.GetStartCharacterCode() + data.GetEntryCount() - 1L,
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 12)
        {
            CollectGroupedSequentialCandidates(
                subtable.GetFormat12(),
                sourceIndex,
                glyphId,
                candidates);
        }
        else if (format == 13)
        {
            CollectConstantGroupCandidates(
                subtable.GetFormat13(),
                sourceIndex,
                glyphId,
                candidates);
        }
    }

    void CollectLegacyCandidates(
        int sourceIndex,
        int glyphId,
        int codePage,
        System.Collections.Generic.List<int> candidates)
    {
        int index = 0;

        while (index < LegacyEncodingData.GetUnicodeValueCount(codePage))
        {
            int unicodeValue = LegacyEncodingData.GetUnicodeValue(
                codePage,
                index);

            if (MapSourceUnicode(sourceIndex, unicodeValue) == glyphId)
            {
                AddEffectiveCandidate(
                    sourceIndex,
                    glyphId,
                    unicodeValue,
                    candidates);
            }

            index += 1;
        }
    }

    void CollectScannedCandidates(
        SfntCmapSubtable subtable,
        long start,
        long end,
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        long characterCode = start;

        while (characterCode <= end)
        {
            if (subtable.MapCharacterCode(characterCode) == glyphId &&
                characterCode <= int.MaxValue)
            {
                AddNativeCandidate(
                    sourceIndex,
                    glyphId,
                    (int)characterCode,
                    candidates);
            }

            characterCode += 1;
        }
    }

    void CollectSequentialGroupCandidates(
        SfntCmapFormat8Data? data,
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(data);
        while (index < data.GetGroupCount())
        {
            SfntCmapGroup group = data.GetGroup(index);
            long glyphOffset = glyphId - (long)group.GetStartGlyphId();
            long encodedCharacterCode =
                group.GetStartCharacterCode() + glyphOffset;

            if (glyphOffset >= 0 &&
                encodedCharacterCode <= group.GetEndCharacterCode())
            {
                int unicodeValue = DecodeFormat8CharacterCode(
                    encodedCharacterCode);
                AddNativeCandidate(
                    sourceIndex,
                    glyphId,
                    unicodeValue,
                    candidates);
            }

            index += 1;
        }
    }

    void CollectGroupedSequentialCandidates(
        SfntCmapGroupedData? data,
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(data);
        while (index < data.GetGroupCount())
        {
            SfntCmapGroup group = data.GetGroup(index);
            long glyphOffset = glyphId - (long)group.GetStartGlyphId();
            long characterCode = group.GetStartCharacterCode() + glyphOffset;

            if (glyphOffset >= 0 &&
                characterCode <= group.GetEndCharacterCode() &&
                characterCode <= int.MaxValue)
            {
                AddNativeCandidate(
                    sourceIndex,
                    glyphId,
                    (int)characterCode,
                    candidates);
            }

            index += 1;
        }
    }

    void CollectConstantGroupCandidates(
        SfntCmapGroupedData? data,
        int sourceIndex,
        int glyphId,
        System.Collections.Generic.List<int> candidates)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(data);
        while (index < data.GetGroupCount())
        {
            SfntCmapGroup group = data.GetGroup(index);

            if (group.GetStartGlyphId() == glyphId)
            {
                long start = group.GetStartCharacterCode();
                long end = group.GetEndCharacterCode();
                long characterCode = start < 0 ? 0 : start;
                long boundedEnd = end > 0x10FFFF ? 0x10FFFF : end;

                while (characterCode <= boundedEnd)
                {
                    AddNativeCandidate(
                        sourceIndex,
                        glyphId,
                        (int)characterCode,
                        candidates);
                    characterCode += 1;
                }
            }

            index += 1;
        }
    }

    void AddNativeCandidate(
        int sourceIndex,
        int glyphId,
        int unicodeValue,
        System.Collections.Generic.List<int> candidates)
    {
        if (symbol && unicodeValue <= 0xFF)
        {
            return;
        }

        AddEffectiveCandidate(
            sourceIndex,
            glyphId,
            unicodeValue,
            candidates);
    }

    void AddEffectiveCandidate(
        int sourceIndex,
        int glyphId,
        int unicodeValue,
        System.Collections.Generic.List<int> candidates)
    {
        if (!IsUnicodeScalar(unicodeValue) ||
            FindMappingSourceIndex(unicodeValue, glyphId) != sourceIndex)
        {
            return;
        }

        if (candidates.Count >= maximumCandidateCount)
        {
            throw FontLimitException.Create(
                "The reverse cmap candidate count exceeds the configured coverage limit.");
        }

        candidates.Add(unicodeValue);
    }

    static int DecodeFormat8CharacterCode(long characterCode)
    {
        if (characterCode <= 0xFFFF)
        {
            return (int)characterCode;
        }

        int highSurrogate = (int)(characterCode >> 16);
        int lowSurrogate = (int)(characterCode & 0xFFFF);

        if (highSurrogate < 0xD800 || highSurrogate > 0xDBFF ||
            lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
        {
            return -1;
        }

        return 0x10000 + ((highSurrogate - 0xD800) << 10) +
            lowSurrogate - 0xDC00;
    }

    internal int FindMappingSourceIndex(int unicodeValue, int glyphId)
    {
        int sourceIndex = 0;

        while (sourceIndex < sources.Length)
        {
            int mappedGlyphId = MapSourceUnicode(sourceIndex, unicodeValue);

            if (mappedGlyphId == 0 && symbol && unicodeValue <= 0xFF)
            {
                mappedGlyphId = MapSourceUnicode(
                    sourceIndex,
                    0xF000 + unicodeValue);
            }

            if (mappedGlyphId != 0)
            {
                if (mappedGlyphId == glyphId)
                {
                    return sourceIndex;
                }

                return -1;
            }

            sourceIndex += 1;
        }

        return -1;
    }

    internal bool IsSymbolRemapped(int sourceIndex, int unicodeValue)
    {
        if (!symbol || sourceIndex < 0 || sourceIndex >= sources.Length ||
            unicodeValue < 0 || unicodeValue > 0xFF)
        {
            return false;
        }

        return MapSourceUnicode(sourceIndex, unicodeValue) == 0 &&
            MapSourceUnicode(sourceIndex, 0xF000 + unicodeValue) != 0;
    }

    int MapSourceUnicode(int sourceIndex, int unicodeValue)
    {
        SfntCmapEncodingRecord record = sources[sourceIndex];
        int codePage = LegacyEncodingData.GetCodePage(
            record.GetPlatformId(),
            record.GetEncodingId());
        int characterCode = unicodeValue;

        if (LegacyEncodingData.IsCodePageSupported(codePage))
        {
            return MapLegacyUnicode(
                record.GetSubtable(),
                codePage,
                unicodeValue);
        }

        return record.GetSubtable().MapCharacterCode(characterCode);
    }

    static int MapLegacyUnicode(
        SfntCmapSubtable subtable,
        int codePage,
        int unicodeValue)
    {
        int encodedCount = LegacyEncodingData.GetEncodedValueCount(
            codePage,
            unicodeValue);
        int index = 0;

        while (index < encodedCount)
        {
            int encodedValue = LegacyEncodingData.GetEncodedValue(
                codePage,
                unicodeValue,
                index);
            int glyphId = subtable.MapCharacterCode(encodedValue);

            if (glyphId != 0)
            {
                return glyphId;
            }

            index += 1;
        }

        return 0;
    }

    static bool IsUnicodeScalar(int value)
    {
        return value >= 0 && value <= 0x10FFFF &&
            (value < 0xD800 || value > 0xDFFF);
    }
}

internal static class SfntCmapMappingBuilder
{
    const int SymbolRank = 0;
    const int WindowsFullRank = 100;
    const int UnicodeLastResortRank = 110;
    const int UnicodeFullRank = 120;
    const int WindowsBmpRank = 200;
    const int UnicodeBmpRank = 210;
    const int UnicodeIsoRank = 220;
    const int UnicodeElevenRank = 230;
    const int UnicodeTenRank = 240;
    const int WindowsLegacyRank = 300;
    const int MacintoshLegacyRank = 400;

    internal static SfntCmapMapping Build(
        SfntCmapEncodingRecord[] records,
        int glyphCount,
        FontLimits limits)
    {
        SfntCmapEncodingRecord? symbolRecord = FindRecord(records, 3, 0);
        SfntCmapEncodingRecord[]? sources;
        int[] sourceRanks;
        bool symbol = symbolRecord != null;

        if (symbol)
        {
            System.ArgumentNullException.ThrowIfNull(symbolRecord);
            sources = new SfntCmapEncodingRecord[] { symbolRecord };
            sourceRanks = new int[] { SymbolRank };
        }
        else
        {
            int sourceCount = CountMappingSources(records);
            sources = new SfntCmapEncodingRecord[sourceCount];
            sourceRanks = new int[sourceCount];
            int targetIndex = 0;
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                3,
                10,
                WindowsFullRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                6,
                UnicodeLastResortRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                4,
                UnicodeFullRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                3,
                1,
                WindowsBmpRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                3,
                UnicodeBmpRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                2,
                UnicodeIsoRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                1,
                UnicodeElevenRank);
            targetIndex = AddRankedRecord(
                records,
                sources,
                sourceRanks,
                targetIndex,
                0,
                0,
                UnicodeTenRank);
            targetIndex = AddLegacyRecords(
                records,
                sources,
                sourceRanks,
                targetIndex,
                3);
            AddLegacyRecords(
                records,
                sources,
                sourceRanks,
                targetIndex,
                1);
        }

        SfntCmapFormat14Data? variations = FindVariations(records);
        SfntCmapCoverageRange[] coverage = BuildCoverage(
            sources,
            symbol,
            GetMaximumMergedRangeCount(limits.GetMaxCoverageEntries()));
        return new SfntCmapMapping(
            sources,
            sourceRanks,
            symbol,
            variations,
            coverage,
            glyphCount,
            limits.GetMaxCoverageEntries());
    }

    static int GetMaximumMergedRangeCount(int maximumCoverageEntries)
    {
        long maximumRangeCount = maximumCoverageEntries * 16L + 65536L;

        if (maximumRangeCount > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)maximumRangeCount;
    }

    static int CountMappingSources(SfntCmapEncodingRecord[] records)
    {
        int count = 0;
        int index = 0;

        while (index < records.Length)
        {
            if (GetRank(records[index]) >= 0)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static int AddRankedRecord(
        SfntCmapEncodingRecord[] records,
        SfntCmapEncodingRecord[] sources,
        int[] ranks,
        int targetIndex,
        int platformId,
        int encodingId,
        int rank)
    {
        SfntCmapEncodingRecord? record = FindRecord(
            records,
            platformId,
            encodingId);

        if (record != null)
        {
            sources[targetIndex] = record;
            ranks[targetIndex] = rank;
            targetIndex += 1;
        }

        return targetIndex;
    }

    static int AddLegacyRecords(
        SfntCmapEncodingRecord[] records,
        SfntCmapEncodingRecord[] sources,
        int[] ranks,
        int targetIndex,
        int platformId)
    {
        int index = 0;

        while (index < records.Length)
        {
            SfntCmapEncodingRecord record = records[index];
            int rank = GetLegacyRank(record);

            if (record.GetPlatformId() == platformId && rank >= 0)
            {
                sources[targetIndex] = record;
                ranks[targetIndex] = rank;
                targetIndex += 1;
            }

            index += 1;
        }

        return targetIndex;
    }

    static SfntCmapEncodingRecord? FindRecord(
        SfntCmapEncodingRecord[] records,
        int platformId,
        int encodingId)
    {
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].GetPlatformId() == platformId &&
                records[index].GetEncodingId() == encodingId)
            {
                return records[index];
            }

            index += 1;
        }

        return null;
    }

    static int GetRank(SfntCmapEncodingRecord record)
    {
        int platformId = record.GetPlatformId();
        int encodingId = record.GetEncodingId();

        if (platformId == 3 && encodingId == 10)
        {
            return WindowsFullRank;
        }

        if (platformId == 0 && encodingId == 6)
        {
            return UnicodeLastResortRank;
        }

        if (platformId == 0 && encodingId == 4)
        {
            return UnicodeFullRank;
        }

        if (platformId == 3 && encodingId == 1)
        {
            return WindowsBmpRank;
        }

        if (platformId == 0 && encodingId == 3)
        {
            return UnicodeBmpRank;
        }

        if (platformId == 0 && encodingId == 2)
        {
            return UnicodeIsoRank;
        }

        if (platformId == 0 && encodingId == 1)
        {
            return UnicodeElevenRank;
        }

        if (platformId == 0 && encodingId == 0)
        {
            return UnicodeTenRank;
        }

        return GetLegacyRank(record);
    }

    static int GetLegacyRank(SfntCmapEncodingRecord record)
    {
        int platformId = record.GetPlatformId();
        int encodingId = record.GetEncodingId();
        int codePage = LegacyEncodingData.GetCodePage(platformId, encodingId);

        if (!LegacyEncodingData.IsCodePageSupported(codePage))
        {
            return -1;
        }

        if (platformId == 3)
        {
            return WindowsLegacyRank + encodingId;
        }

        if (platformId == 1)
        {
            return MacintoshLegacyRank + encodingId;
        }

        return -1;
    }

    static SfntCmapFormat14Data? FindVariations(
        SfntCmapEncodingRecord[] records)
    {
        SfntCmapEncodingRecord? record = FindRecord(records, 0, 5);

        if (record == null)
        {
            return null;
        }

        return record.GetSubtable().GetFormat14();
    }

    static SfntCmapCoverageRange[] BuildCoverage(
        SfntCmapEncodingRecord[] sources,
        bool symbol,
        int maximumRangeCount)
    {
        SfntCmapCoverageRange[] merged = new SfntCmapCoverageRange[0];
        int index = 0;

        while (index < sources.Length)
        {
            SfntCmapCoverageRange[] sourceRanges = CollectCoverage(
                sources[index],
                symbol,
                maximumRangeCount);
            merged = MergeCoverage(
                merged,
                sourceRanges,
                maximumRangeCount);
            index += 1;
        }

        return merged;
    }

    static SfntCmapCoverageRange[] CollectCoverage(
        SfntCmapEncodingRecord record,
        bool symbol,
        int maximumRangeCount)
    {
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges =
            new System.Collections.Generic.List<SfntCmapCoverageRange>();
        int codePage = LegacyEncodingData.GetCodePage(
            record.GetPlatformId(),
            record.GetEncodingId());
        SfntCmapSubtable subtable = record.GetSubtable();

        if (LegacyEncodingData.IsCodePageSupported(codePage))
        {
            CollectLegacyCoverage(
                subtable,
                codePage,
                ranges,
                maximumRangeCount);
            return ranges.ToArray();
        }

        int format = subtable.GetFormat();

        if (format == 0)
        {
            CollectMappedRuns(subtable, 0, 255, ranges, maximumRangeCount);
        }
        else if (format == 2 || format == 4)
        {
            CollectMappedRuns(subtable, 0, 0xFFFF, ranges, maximumRangeCount);
        }
        else if (format == 6)
        {
            SfntCmapFormat6Data? data = subtable.GetFormat6();
            System.ArgumentNullException.ThrowIfNull(data);
            CollectMappedRuns(
                subtable,
                data.GetFirstCode(),
                data.GetFirstCode() + data.GetEntryCount() - 1L,
                ranges,
                maximumRangeCount);
        }
        else if (format == 8)
        {
            CollectFormat8Ranges(
                subtable.GetFormat8(),
                ranges,
                maximumRangeCount);
        }
        else if (format == 10)
        {
            SfntCmapFormat10Data? data = subtable.GetFormat10();
            System.ArgumentNullException.ThrowIfNull(data);
            CollectMappedRuns(
                subtable,
                data.GetStartCharacterCode(),
                data.GetStartCharacterCode() + data.GetEntryCount() - 1L,
                ranges,
                maximumRangeCount);
        }
        else if (format == 12)
        {
            CollectGroupedRanges(
                subtable.GetFormat12(),
                false,
                ranges,
                maximumRangeCount);
        }
        else if (format == 13)
        {
            CollectGroupedRanges(
                subtable.GetFormat13(),
                true,
                ranges,
                maximumRangeCount);
        }

        if (symbol)
        {
            CollectSymbolAliases(subtable, ranges, maximumRangeCount);
            ranges.Sort((left, right) => CompareRanges(left, right));
            return MergeSortedRanges(ranges.ToArray(), maximumRangeCount);
        }

        return ranges.ToArray();
    }

    static void CollectLegacyCoverage(
        SfntCmapSubtable subtable,
        int codePage,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        int runStart = -1;
        int runEnd = -1;
        int index = 0;

        while (index < LegacyEncodingData.GetUnicodeValueCount(codePage))
        {
            int unicodeValue = LegacyEncodingData.GetUnicodeValue(
                codePage,
                index);
            int glyphId = MapLegacyUnicode(
                subtable,
                codePage,
                unicodeValue);

            if (glyphId != 0 && IsUnicodeScalar(unicodeValue))
            {
                if (runStart < 0)
                {
                    runStart = unicodeValue;
                    runEnd = unicodeValue;
                }
                else if (unicodeValue == runEnd + 1)
                {
                    runEnd = unicodeValue;
                }
                else
                {
                    AddRange(ranges, runStart, runEnd, maximumRangeCount);
                    runStart = unicodeValue;
                    runEnd = unicodeValue;
                }
            }
            else if (runStart >= 0)
            {
                AddRange(ranges, runStart, runEnd, maximumRangeCount);
                runStart = -1;
                runEnd = -1;
            }

            index += 1;
        }

        if (runStart >= 0)
        {
            AddRange(ranges, runStart, runEnd, maximumRangeCount);
        }
    }

    static int MapLegacyUnicode(
        SfntCmapSubtable subtable,
        int codePage,
        int unicodeValue)
    {
        int encodedCount = LegacyEncodingData.GetEncodedValueCount(
            codePage,
            unicodeValue);
        int index = 0;

        while (index < encodedCount)
        {
            int encodedValue = LegacyEncodingData.GetEncodedValue(
                codePage,
                unicodeValue,
                index);
            int glyphId = subtable.MapCharacterCode(encodedValue);

            if (glyphId != 0)
            {
                return glyphId;
            }

            index += 1;
        }

        return 0;
    }

    static void CollectMappedRuns(
        SfntCmapSubtable subtable,
        long start,
        long end,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        if (end < 0 || start > 0x10FFFF || start > end)
        {
            return;
        }

        long boundedStart = start < 0 ? 0 : start;
        long boundedEnd = end > 0x10FFFF ? 0x10FFFF : end;
        CollectMappedScalarRuns(
            subtable,
            boundedStart,
            boundedEnd < 0xD800 ? boundedEnd : 0xD7FF,
            ranges,
            maximumRangeCount);
        CollectMappedScalarRuns(
            subtable,
            boundedStart > 0xDFFF ? boundedStart : 0xE000,
            boundedEnd,
            ranges,
            maximumRangeCount);
    }

    static void CollectMappedScalarRuns(
        SfntCmapSubtable subtable,
        long start,
        long end,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        if (start > end)
        {
            return;
        }

        long runStart = -1;
        long unicodeValue = start;

        while (unicodeValue <= end)
        {
            int glyphId = subtable.MapCharacterCode(unicodeValue);

            if (glyphId != 0 && runStart < 0)
            {
                runStart = unicodeValue;
            }
            else if (glyphId == 0 && runStart >= 0)
            {
                AddRange(
                    ranges,
                    (int)runStart,
                    (int)unicodeValue - 1,
                    maximumRangeCount);
                runStart = -1;
            }

            unicodeValue += 1;
        }

        if (runStart >= 0)
        {
            AddRange(ranges, (int)runStart, (int)end, maximumRangeCount);
        }
    }

    static void CollectGroupedRanges(
        SfntCmapGroupedData? data,
        bool constantGlyph,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(data);
        while (index < data.GetGroupCount())
        {
            SfntCmapGroup group = data.GetGroup(index);
            long start = group.GetStartCharacterCode();
            long end = group.GetEndCharacterCode();

            if (constantGlyph)
            {
                if (group.GetStartGlyphId() != 0)
                {
                    AddUnicodeRange(ranges, start, end, maximumRangeCount);
                }
            }
            else
            {
                if (group.GetStartGlyphId() == 0)
                {
                    start += 1;
                }

                AddUnicodeRange(ranges, start, end, maximumRangeCount);
            }

            index += 1;
        }
    }

    static void CollectFormat8Ranges(
        SfntCmapFormat8Data? data,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(data);
        while (index < data.GetGroupCount())
        {
            SfntCmapGroup group = data.GetGroup(index);
            long rawStart = group.GetStartCharacterCode();
            long rawEnd = group.GetEndCharacterCode();

            if (group.GetStartGlyphId() == 0)
            {
                rawStart += 1;
            }

            if (rawStart <= 0xFFFF)
            {
                AddUnicodeRange(ranges, rawStart, rawEnd, maximumRangeCount);
            }
            else
            {
                int startHigh = (int)(rawStart >> 16);
                int endHigh = (int)(rawEnd >> 16);
                int high = startHigh;

                while (high <= endHigh)
                {
                    long lowStart = high == startHigh ? rawStart & 0xFFFF : 0;
                    long lowEnd = high == endHigh ? rawEnd & 0xFFFF : 0xFFFF;

                    if (lowStart < 0xDC00)
                    {
                        lowStart = 0xDC00;
                    }

                    if (lowEnd > 0xDFFF)
                    {
                        lowEnd = 0xDFFF;
                    }

                    if (high >= 0xD800 && high <= 0xDBFF &&
                        lowStart <= lowEnd)
                    {
                        int unicodeStart = ConvertSurrogatePair(
                            high,
                            (int)lowStart);
                        int unicodeEnd = ConvertSurrogatePair(
                            high,
                            (int)lowEnd);
                        AddRange(
                            ranges,
                            unicodeStart,
                            unicodeEnd,
                            maximumRangeCount);
                    }

                    high += 1;
                }
            }

            index += 1;
        }
    }

    static int ConvertSurrogatePair(int high, int low)
    {
        return 0x10000 + ((high - 0xD800) << 10) + low - 0xDC00;
    }

    static bool IsUnicodeScalar(int value)
    {
        return value >= 0 && value <= 0x10FFFF &&
            (value < 0xD800 || value > 0xDFFF);
    }

    static void CollectSymbolAliases(
        SfntCmapSubtable subtable,
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int maximumRangeCount)
    {
        int runStart = -1;
        int unicodeValue = 0;

        while (unicodeValue <= 0xFF)
        {
            int glyphId = subtable.MapCharacterCode(unicodeValue);

            if (glyphId == 0)
            {
                glyphId = subtable.MapCharacterCode(0xF000 + unicodeValue);
            }

            if (glyphId != 0 && runStart < 0)
            {
                runStart = unicodeValue;
            }
            else if (glyphId == 0 && runStart >= 0)
            {
                AddRange(
                    ranges,
                    runStart,
                    unicodeValue - 1,
                    maximumRangeCount);
                runStart = -1;
            }

            unicodeValue += 1;
        }

        if (runStart >= 0)
        {
            AddRange(ranges, runStart, 0xFF, maximumRangeCount);
        }
    }

    static void AddUnicodeRange(
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        long start,
        long end,
        int maximumRangeCount)
    {
        if (end < 0 || start > 0x10FFFF || start > end)
        {
            return;
        }

        int boundedStart = start < 0 ? 0 : (int)start;
        int boundedEnd = end > 0x10FFFF ? 0x10FFFF : (int)end;

        if (boundedStart <= 0xD7FF)
        {
            int firstEnd = boundedEnd < 0xD7FF ? boundedEnd : 0xD7FF;
            AddRange(ranges, boundedStart, firstEnd, maximumRangeCount);
        }

        if (boundedEnd >= 0xE000)
        {
            int secondStart = boundedStart > 0xE000 ? boundedStart : 0xE000;
            AddRange(ranges, secondStart, boundedEnd, maximumRangeCount);
        }
    }

    static void AddRange(
        System.Collections.Generic.List<SfntCmapCoverageRange> ranges,
        int start,
        int end,
        int maximumRangeCount)
    {
        if (start > end)
        {
            return;
        }

        if (ranges.Count >= maximumRangeCount)
        {
            throw FontLimitException.Create(
                "The merged cmap range count exceeds the configured coverage limit.");
        }

        ranges.Add(new SfntCmapCoverageRange(start, end));
    }

    static int CompareRanges(
        SfntCmapCoverageRange left,
        SfntCmapCoverageRange right)
    {
        if (left.GetStartUnicodeValue() < right.GetStartUnicodeValue())
        {
            return -1;
        }

        if (left.GetStartUnicodeValue() > right.GetStartUnicodeValue())
        {
            return 1;
        }

        return left.GetEndUnicodeValue() - right.GetEndUnicodeValue();
    }

    static SfntCmapCoverageRange[] MergeCoverage(
        SfntCmapCoverageRange[] first,
        SfntCmapCoverageRange[] second,
        int maximumRangeCount)
    {
        SfntCmapCoverageRange[] ordered =
            new SfntCmapCoverageRange[first.Length + second.Length];
        int firstIndex = 0;
        int secondIndex = 0;
        int targetIndex = 0;

        while (firstIndex < first.Length || secondIndex < second.Length)
        {
            if (secondIndex >= second.Length ||
                firstIndex < first.Length &&
                CompareRanges(first[firstIndex], second[secondIndex]) <= 0)
            {
                ordered[targetIndex] = first[firstIndex];
                firstIndex += 1;
            }
            else
            {
                ordered[targetIndex] = second[secondIndex];
                secondIndex += 1;
            }

            targetIndex += 1;
        }

        return MergeSortedRanges(ordered, maximumRangeCount);
    }

    static SfntCmapCoverageRange[] MergeSortedRanges(
        SfntCmapCoverageRange[] ordered,
        int maximumRangeCount)
    {
        System.Collections.Generic.List<SfntCmapCoverageRange> merged =
            new System.Collections.Generic.List<SfntCmapCoverageRange>();

        if (ordered.Length == 0)
        {
            return merged.ToArray();
        }

        int start = ordered[0].GetStartUnicodeValue();
        int end = ordered[0].GetEndUnicodeValue();
        int index = 1;

        while (index < ordered.Length)
        {
            SfntCmapCoverageRange range = ordered[index];

            if (range.GetStartUnicodeValue() <= end + 1)
            {
                if (range.GetEndUnicodeValue() > end)
                {
                    end = range.GetEndUnicodeValue();
                }
            }
            else
            {
                AddRange(merged, start, end, maximumRangeCount);
                start = range.GetStartUnicodeValue();
                end = range.GetEndUnicodeValue();
            }

            index += 1;
        }

        AddRange(merged, start, end, maximumRangeCount);
        return merged.ToArray();
    }
}
