namespace ttf;

internal class FontSubsetCmapEntry
{
    int codePoint;
    int glyphId;

    internal FontSubsetCmapEntry(int codePoint, int glyphId)
    {
        this.codePoint = codePoint;
        this.glyphId = glyphId;
    }

    internal int GetCodePoint()
    {
        return codePoint;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }
}

internal class FontSubsetCmapVariationEntry
{
    int codePoint;
    int variationSelector;
    int glyphId;

    internal FontSubsetCmapVariationEntry(
        int codePoint,
        int variationSelector,
        int glyphId)
    {
        this.codePoint = codePoint;
        this.variationSelector = variationSelector;
        this.glyphId = glyphId;
    }

    internal int GetCodePoint()
    {
        return codePoint;
    }

    internal int GetVariationSelector()
    {
        return variationSelector;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }
}

internal class FontSubsetCmapSubtable
{
    int platformId;
    int encodingId;
    byte[] data;

    internal FontSubsetCmapSubtable(
        int platformId,
        int encodingId,
        byte[] data)
    {
        this.platformId = platformId;
        this.encodingId = encodingId;
        this.data = data;
    }

    internal int GetPlatformId()
    {
        return platformId;
    }

    internal int GetEncodingId()
    {
        return encodingId;
    }

    internal byte[] GetData()
    {
        return data;
    }
}

internal static class FontSubsetCmapBuilder
{
    const int MaximumFormat4Length = 65535;

    internal static byte[] Create(
        FontInstance instance,
        FontSubsetUnicodeMapping[] mappings,
        int[] oldToNewGlyphIds,
        int[] requestedCodePoints)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (mappings == null)
        {
            throw new System.ArgumentNullException("mappings");
        }

        if (oldToNewGlyphIds == null)
        {
            throw new System.ArgumentNullException("oldToNewGlyphIds");
        }

        if (requestedCodePoints == null)
        {
            throw new System.ArgumentNullException("requestedCodePoints");
        }

        if (oldToNewGlyphIds.Length != instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The glyph map does not match the font instance.",
                "oldToNewGlyphIds");
        }

        FontLimits limits = instance.GetFace()
            .GetTableCache()
            .GetLimits();
        System.Collections.Generic.List<FontSubsetCmapEntry> ordinary =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        System.Collections.Generic.List<FontSubsetCmapVariationEntry>
            variations =
                new System.Collections.Generic.List<
                    FontSubsetCmapVariationEntry>();
        CollectMappings(
            mappings,
            oldToNewGlyphIds,
            ordinary,
            variations,
            limits.GetMaxCoverageEntries());
        FontSubsetCmapEntry[] ordinaryEntries = SortOrdinary(ordinary);
        FontSubsetCmapVariationEntry[] variationEntries =
            SortVariations(variations);
        bool preserveSymbol = ShouldPreserveSymbol(
            instance,
            requestedCodePoints);
        return BuildTable(
            ordinaryEntries,
            variationEntries,
            preserveSymbol,
            limits.GetMaxOutputBytes());
    }

    static void CollectMappings(
        FontSubsetUnicodeMapping[] mappings,
        int[] oldToNewGlyphIds,
        System.Collections.Generic.List<FontSubsetCmapEntry> ordinary,
        System.Collections.Generic.List<FontSubsetCmapVariationEntry>
            variations,
        int maximumEntries)
    {
        int index = 0;

        while (index < mappings.Length)
        {
            FontSubsetUnicodeMapping mapping = mappings[index];
            int oldGlyphId = mapping.GetGlyphId();

            if (oldGlyphId < 0 || oldGlyphId >= oldToNewGlyphIds.Length)
            {
                throw FontFormatException.Create(
                    "A subset Unicode mapping references an invalid glyph.");
            }

            int newGlyphId = oldToNewGlyphIds[oldGlyphId];

            if (newGlyphId < 0)
            {
                throw FontFormatException.Create(
                    "A subset Unicode mapping references an omitted glyph.");
            }

            int[] sequence = mapping.CopyUnicodeSequence();

            if (newGlyphId != 0 && sequence.Length == 1)
            {
                EnsureCapacity(
                    ordinary.Count + variations.Count,
                    maximumEntries);
                ordinary.Add(new FontSubsetCmapEntry(
                    sequence[0],
                    newGlyphId));
            }
            else if (newGlyphId != 0 && sequence.Length == 2 &&
                IsVariationSelector(sequence[1]))
            {
                EnsureCapacity(
                    ordinary.Count + variations.Count,
                    maximumEntries);
                variations.Add(new FontSubsetCmapVariationEntry(
                    sequence[0],
                    sequence[1],
                    newGlyphId));
            }

            index++;
        }
    }

    static FontSubsetCmapEntry[] SortOrdinary(
        System.Collections.Generic.List<FontSubsetCmapEntry> entries)
    {
        entries.Sort((left, right) =>
        {
            int comparison = left.GetCodePoint().CompareTo(
                right.GetCodePoint());
            return comparison != 0 ? comparison :
                left.GetGlyphId().CompareTo(right.GetGlyphId());
        });
        System.Collections.Generic.List<FontSubsetCmapEntry> unique =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        int index = 0;

        while (index < entries.Count)
        {
            FontSubsetCmapEntry entry = entries[index];
            ValidateUnicodeScalar(entry.GetCodePoint());

            if (unique.Count != 0 &&
                unique[unique.Count - 1].GetCodePoint() ==
                    entry.GetCodePoint())
            {
                if (unique[unique.Count - 1].GetGlyphId() !=
                    entry.GetGlyphId())
                {
                    throw FontFormatException.Create(
                        "A subset cmap has conflicting nominal mappings.");
                }
            }
            else
            {
                unique.Add(entry);
            }

            index++;
        }

        return unique.ToArray();
    }

    static FontSubsetCmapVariationEntry[] SortVariations(
        System.Collections.Generic.List<FontSubsetCmapVariationEntry> entries)
    {
        entries.Sort((left, right) =>
        {
            int selectorComparison = left.GetVariationSelector().CompareTo(
                right.GetVariationSelector());

            if (selectorComparison != 0)
            {
                return selectorComparison;
            }

            int codePointComparison = left.GetCodePoint().CompareTo(
                right.GetCodePoint());
            return codePointComparison != 0 ? codePointComparison :
                left.GetGlyphId().CompareTo(right.GetGlyphId());
        });
        System.Collections.Generic.List<FontSubsetCmapVariationEntry> unique =
            new System.Collections.Generic.List<
                FontSubsetCmapVariationEntry>();
        int index = 0;

        while (index < entries.Count)
        {
            FontSubsetCmapVariationEntry entry = entries[index];
            ValidateUnicodeScalar(entry.GetCodePoint());

            if (!IsVariationSelector(entry.GetVariationSelector()))
            {
                throw FontFormatException.Create(
                    "A subset cmap variation selector is invalid.");
            }

            if (unique.Count != 0 &&
                SameVariationKey(unique[unique.Count - 1], entry))
            {
                if (unique[unique.Count - 1].GetGlyphId() !=
                    entry.GetGlyphId())
                {
                    throw FontFormatException.Create(
                        "A subset cmap has conflicting variation mappings.");
                }
            }
            else
            {
                unique.Add(entry);
            }

            index++;
        }

        return unique.ToArray();
    }

    static byte[] BuildTable(
        FontSubsetCmapEntry[] ordinary,
        FontSubsetCmapVariationEntry[] variations,
        bool preserveSymbol,
        int maximumOutputBytes)
    {
        System.Collections.Generic.List<FontSubsetCmapSubtable> subtables =
            new System.Collections.Generic.List<FontSubsetCmapSubtable>();
        FontSubsetCmapEntry[] bmp = SelectBmpEntries(ordinary);
        bool canWriteBmp = CanWriteFormat4(bmp);
        bool requiresFull = RequiresFullRepertoire(ordinary) ||
            (!canWriteBmp && ordinary.Length != 0);

        if (canWriteBmp && (bmp.Length != 0 || ordinary.Length == 0))
        {
            subtables.Add(new FontSubsetCmapSubtable(
                3,
                1,
                BuildFormat4(bmp, maximumOutputBytes)));
        }

        if (requiresFull)
        {
            subtables.Add(new FontSubsetCmapSubtable(
                3,
                10,
                BuildFormat12(ordinary, maximumOutputBytes)));
        }

        if (variations.Length != 0)
        {
            subtables.Add(new FontSubsetCmapSubtable(
                0,
                5,
                BuildFormat14(
                    ordinary,
                    variations,
                    maximumOutputBytes)));
        }

        if (preserveSymbol && ordinary.Length != 0)
        {
            FontSubsetCmapEntry[] symbolEntries = CreateSymbolEntries(
                ordinary);
            byte[] symbolData = CanWriteFormat4(symbolEntries) ?
                BuildFormat4(symbolEntries, maximumOutputBytes) :
                BuildFormat12(symbolEntries, maximumOutputBytes);
            subtables.Add(new FontSubsetCmapSubtable(
                3,
                0,
                symbolData));
        }

        subtables.Sort((left, right) =>
        {
            int comparison = left.GetPlatformId().CompareTo(
                right.GetPlatformId());
            return comparison != 0 ? comparison :
                left.GetEncodingId().CompareTo(right.GetEncodingId());
        });
        return WriteCmap(subtables, maximumOutputBytes);
    }

    static byte[] BuildFormat4(
        FontSubsetCmapEntry[] entries,
        int maximumOutputBytes)
    {
        FontSubsetCmapEntry[] starts = CreateSequentialGroupStarts(entries);
        FontSubsetCmapEntry[] ends = CreateSequentialGroupEnds(entries);
        int segmentCount = starts.Length + 1;
        int length = 16 + segmentCount * 8;
        BigEndianWriter writer = BigEndianWriter.Create(
            length,
            maximumOutputBytes);
        writer.WriteUInt16(4);
        writer.WriteUInt16((ushort)length);
        writer.WriteUInt16(0);
        writer.WriteUInt16((ushort)(segmentCount * 2));
        int maximumPowerOfTwo = MaximumPowerOfTwo(segmentCount);
        writer.WriteUInt16((ushort)(maximumPowerOfTwo * 2));
        writer.WriteUInt16((ushort)Log2(maximumPowerOfTwo));
        writer.WriteUInt16((ushort)(segmentCount * 2 -
            maximumPowerOfTwo * 2));
        int index = 0;

        while (index < ends.Length)
        {
            writer.WriteUInt16((ushort)ends[index].GetCodePoint());
            index++;
        }

        writer.WriteUInt16(0xFFFF);
        writer.WriteUInt16(0);
        index = 0;

        while (index < starts.Length)
        {
            writer.WriteUInt16((ushort)starts[index].GetCodePoint());
            index++;
        }

        writer.WriteUInt16(0xFFFF);
        index = 0;

        while (index < starts.Length)
        {
            int delta = starts[index].GetGlyphId() -
                starts[index].GetCodePoint();
            uint encodedDelta = CheckedFontMath.ReinterpretInt32AsUInt32(delta);
            writer.WriteUInt16((ushort)(encodedDelta & 0xFFFFU));
            index++;
        }

        writer.WriteUInt16(1);
        index = 0;

        while (index < segmentCount)
        {
            writer.WriteUInt16(0);
            index++;
        }

        return writer.CopyBytes();
    }

    static byte[] BuildFormat12(
        FontSubsetCmapEntry[] entries,
        int maximumOutputBytes)
    {
        FontSubsetCmapEntry[] starts = CreateSequentialGroupStarts(entries);
        FontSubsetCmapEntry[] ends = CreateSequentialGroupEnds(entries);
        int length = 16 + starts.Length * 12;
        BigEndianWriter writer = BigEndianWriter.Create(
            length,
            maximumOutputBytes);
        writer.WriteUInt16(12);
        writer.WriteUInt16(0);
        writer.WriteUInt32((uint)length);
        writer.WriteUInt32(0);
        writer.WriteUInt32((uint)starts.Length);
        int index = 0;

        while (index < starts.Length)
        {
            writer.WriteUInt32((uint)starts[index].GetCodePoint());
            writer.WriteUInt32((uint)ends[index].GetCodePoint());
            writer.WriteUInt32((uint)starts[index].GetGlyphId());
            index++;
        }

        return writer.CopyBytes();
    }

    static byte[] BuildFormat14(
        FontSubsetCmapEntry[] ordinary,
        FontSubsetCmapVariationEntry[] variations,
        int maximumOutputBytes)
    {
        int selectorCount = CountVariationSelectors(variations);
        byte[][] defaultTables = new byte[selectorCount][];
        byte[][] nonDefaultTables = new byte[selectorCount][];
        int[] selectors = new int[selectorCount];
        int variationIndex = 0;
        int selectorIndex = 0;

        while (variationIndex < variations.Length)
        {
            int selector = variations[variationIndex].GetVariationSelector();
            int start = variationIndex;

            while (variationIndex < variations.Length &&
                variations[variationIndex].GetVariationSelector() == selector)
            {
                variationIndex++;
            }

            selectors[selectorIndex] = selector;
            defaultTables[selectorIndex] = BuildDefaultVariationTable(
                ordinary,
                variations,
                start,
                variationIndex,
                maximumOutputBytes);
            nonDefaultTables[selectorIndex] =
                BuildNonDefaultVariationTable(
                    ordinary,
                    variations,
                    start,
                    variationIndex,
                    maximumOutputBytes);
            selectorIndex++;
        }

        int headerLength = 10 + selectorCount * 11;
        int length = headerLength;
        selectorIndex = 0;

        while (selectorIndex < selectorCount)
        {
            length += defaultTables[selectorIndex].Length;
            length += nonDefaultTables[selectorIndex].Length;
            selectorIndex++;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            length,
            maximumOutputBytes);
        writer.WriteUInt16(14);
        writer.WriteUInt32((uint)length);
        writer.WriteUInt32((uint)selectorCount);
        int childOffset = headerLength;
        selectorIndex = 0;

        while (selectorIndex < selectorCount)
        {
            writer.WriteUInt24((uint)selectors[selectorIndex]);
            int defaultLength = defaultTables[selectorIndex].Length;
            int nonDefaultLength = nonDefaultTables[selectorIndex].Length;
            writer.WriteUInt32(defaultLength == 0 ? 0U :
                (uint)childOffset);
            childOffset += defaultLength;
            writer.WriteUInt32(nonDefaultLength == 0 ? 0U :
                (uint)childOffset);
            childOffset += nonDefaultLength;
            selectorIndex++;
        }

        selectorIndex = 0;

        while (selectorIndex < selectorCount)
        {
            writer.WriteBytes(defaultTables[selectorIndex]);
            writer.WriteBytes(nonDefaultTables[selectorIndex]);
            selectorIndex++;
        }

        return writer.CopyBytes();
    }

    static byte[] BuildDefaultVariationTable(
        FontSubsetCmapEntry[] ordinary,
        FontSubsetCmapVariationEntry[] variations,
        int start,
        int end,
        int maximumOutputBytes)
    {
        System.Collections.Generic.List<int> codePoints =
            new System.Collections.Generic.List<int>();
        int index = start;

        while (index < end)
        {
            FontSubsetCmapVariationEntry variation = variations[index];

            if (FindGlyphId(ordinary, variation.GetCodePoint()) ==
                variation.GetGlyphId())
            {
                codePoints.Add(variation.GetCodePoint());
            }

            index++;
        }

        if (codePoints.Count == 0)
        {
            return new byte[0];
        }

        int rangeCount = CountDefaultRanges(codePoints);
        BigEndianWriter writer = BigEndianWriter.Create(
            4 + rangeCount * 4,
            maximumOutputBytes);
        writer.WriteUInt32((uint)rangeCount);
        index = 0;

        while (index < codePoints.Count)
        {
            int rangeStart = codePoints[index];
            int rangeEnd = rangeStart;

            while (index + 1 < codePoints.Count &&
                codePoints[index + 1] == rangeEnd + 1 &&
                codePoints[index + 1] - rangeStart <= 255)
            {
                index++;
                rangeEnd = codePoints[index];
            }

            writer.WriteUInt24((uint)rangeStart);
            writer.WriteUInt8((byte)(rangeEnd - rangeStart));
            index++;
        }

        return writer.CopyBytes();
    }

    static byte[] BuildNonDefaultVariationTable(
        FontSubsetCmapEntry[] ordinary,
        FontSubsetCmapVariationEntry[] variations,
        int start,
        int end,
        int maximumOutputBytes)
    {
        int count = 0;
        int index = start;

        while (index < end)
        {
            FontSubsetCmapVariationEntry variation = variations[index];

            if (FindGlyphId(ordinary, variation.GetCodePoint()) !=
                variation.GetGlyphId())
            {
                count++;
            }

            index++;
        }

        if (count == 0)
        {
            return new byte[0];
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            4 + count * 5,
            maximumOutputBytes);
        writer.WriteUInt32((uint)count);
        index = start;

        while (index < end)
        {
            FontSubsetCmapVariationEntry variation = variations[index];

            if (FindGlyphId(ordinary, variation.GetCodePoint()) !=
                variation.GetGlyphId())
            {
                writer.WriteUInt24((uint)variation.GetCodePoint());
                writer.WriteUInt16((ushort)variation.GetGlyphId());
            }

            index++;
        }

        return writer.CopyBytes();
    }

    static byte[] WriteCmap(
        System.Collections.Generic.List<FontSubsetCmapSubtable> subtables,
        int maximumOutputBytes)
    {
        int headerLength = 4 + subtables.Count * 8;
        int totalLength = headerLength;
        int index = 0;

        while (index < subtables.Count)
        {
            totalLength += subtables[index].GetData().Length;
            index++;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            totalLength,
            maximumOutputBytes);
        writer.WriteUInt16(0);
        writer.WriteUInt16((ushort)subtables.Count);
        int offset = headerLength;
        index = 0;

        while (index < subtables.Count)
        {
            FontSubsetCmapSubtable subtable = subtables[index];
            writer.WriteUInt16((ushort)subtable.GetPlatformId());
            writer.WriteUInt16((ushort)subtable.GetEncodingId());
            writer.WriteUInt32((uint)offset);
            offset += subtable.GetData().Length;
            index++;
        }

        index = 0;

        while (index < subtables.Count)
        {
            writer.WriteBytes(subtables[index].GetData());
            index++;
        }

        return writer.CopyBytes();
    }

    static FontSubsetCmapEntry[] SelectBmpEntries(
        FontSubsetCmapEntry[] entries)
    {
        System.Collections.Generic.List<FontSubsetCmapEntry> bmp =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        int index = 0;

        while (index < entries.Length)
        {
            int codePoint = entries[index].GetCodePoint();

            if (codePoint <= 0xFFFE)
            {
                bmp.Add(entries[index]);
            }

            index++;
        }

        return bmp.ToArray();
    }

    static FontSubsetCmapEntry[] CreateSymbolEntries(
        FontSubsetCmapEntry[] entries)
    {
        System.Collections.Generic.List<FontSubsetCmapEntry> symbol =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        int index = 0;

        while (index < entries.Length)
        {
            int codePoint = entries[index].GetCodePoint();

            if (codePoint <= 0xFF)
            {
                codePoint += 0xF000;
            }

            symbol.Add(new FontSubsetCmapEntry(
                codePoint,
                entries[index].GetGlyphId()));
            index++;
        }

        return SortOrdinary(symbol);
    }

    static FontSubsetCmapEntry[] CreateSequentialGroupStarts(
        FontSubsetCmapEntry[] entries)
    {
        System.Collections.Generic.List<FontSubsetCmapEntry> starts =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        int index = 0;

        while (index < entries.Length)
        {
            starts.Add(entries[index]);
            int previousCodePoint = entries[index].GetCodePoint();
            int previousGlyphId = entries[index].GetGlyphId();
            index++;

            while (index < entries.Length &&
                entries[index].GetCodePoint() == previousCodePoint + 1 &&
                entries[index].GetGlyphId() == previousGlyphId + 1)
            {
                previousCodePoint = entries[index].GetCodePoint();
                previousGlyphId = entries[index].GetGlyphId();
                index++;
            }
        }

        return starts.ToArray();
    }

    static FontSubsetCmapEntry[] CreateSequentialGroupEnds(
        FontSubsetCmapEntry[] entries)
    {
        System.Collections.Generic.List<FontSubsetCmapEntry> ends =
            new System.Collections.Generic.List<FontSubsetCmapEntry>();
        int index = 0;

        while (index < entries.Length)
        {
            FontSubsetCmapEntry end = entries[index];
            int previousCodePoint = end.GetCodePoint();
            int previousGlyphId = end.GetGlyphId();
            index++;

            while (index < entries.Length &&
                entries[index].GetCodePoint() == previousCodePoint + 1 &&
                entries[index].GetGlyphId() == previousGlyphId + 1)
            {
                end = entries[index];
                previousCodePoint = end.GetCodePoint();
                previousGlyphId = end.GetGlyphId();
                index++;
            }

            ends.Add(end);
        }

        return ends.ToArray();
    }

    static bool CanWriteFormat4(FontSubsetCmapEntry[] entries)
    {
        FontSubsetCmapEntry[] starts = CreateSequentialGroupStarts(entries);
        return 16L + (starts.Length + 1L) * 8L <=
            MaximumFormat4Length;
    }

    static bool RequiresFullRepertoire(FontSubsetCmapEntry[] entries)
    {
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetCodePoint() > 0xFFFE)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool ShouldPreserveSymbol(
        FontInstance instance,
        int[] requestedCodePoints)
    {
        SfntCmapMapping? cmap = instance.GetFace().GetCmapMapping();

        if (cmap == null || !cmap.IsSymbolMapping())
        {
            return false;
        }

        int index = 0;

        while (index < requestedCodePoints.Length)
        {
            if (cmap.MapUnicode(requestedCodePoints[index]) != 0)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static int CountVariationSelectors(
        FontSubsetCmapVariationEntry[] entries)
    {
        int count = 0;
        int previous = -1;
        int index = 0;

        while (index < entries.Length)
        {
            int selector = entries[index].GetVariationSelector();

            if (selector != previous)
            {
                count++;
                previous = selector;
            }

            index++;
        }

        return count;
    }

    static int CountDefaultRanges(
        System.Collections.Generic.List<int> codePoints)
    {
        int count = 0;
        int index = 0;

        while (index < codePoints.Count)
        {
            int start = codePoints[index];
            int end = start;
            count++;

            while (index + 1 < codePoints.Count &&
                codePoints[index + 1] == end + 1 &&
                codePoints[index + 1] - start <= 255)
            {
                index++;
                end = codePoints[index];
            }

            index++;
        }

        return count;
    }

    static int FindGlyphId(
        FontSubsetCmapEntry[] entries,
        int codePoint)
    {
        int low = 0;
        int high = entries.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = entries[middle].GetCodePoint();

            if (codePoint < candidate)
            {
                high = middle - 1;
            }
            else if (codePoint > candidate)
            {
                low = middle + 1;
            }
            else
            {
                return entries[middle].GetGlyphId();
            }
        }

        return 0;
    }

    static bool SameVariationKey(
        FontSubsetCmapVariationEntry left,
        FontSubsetCmapVariationEntry right)
    {
        return left.GetVariationSelector() == right.GetVariationSelector() &&
            left.GetCodePoint() == right.GetCodePoint();
    }

    static int MaximumPowerOfTwo(int value)
    {
        int result = 1;

        while (result <= value / 2)
        {
            result *= 2;
        }

        return result;
    }

    static int Log2(int value)
    {
        int result = 0;

        while (value > 1)
        {
            value /= 2;
            result++;
        }

        return result;
    }

    static void EnsureCapacity(int count, int maximumEntries)
    {
        if (count >= maximumEntries)
        {
            throw FontLimitException.Create(
                "Subset cmap mappings exceed the configured entry limit.");
        }
    }

    static void ValidateUnicodeScalar(int value)
    {
        if (value < 0 || value > 0x10FFFF ||
            (value >= 0xD800 && value <= 0xDFFF))
        {
            throw FontFormatException.Create(
                "A subset cmap mapping is not a Unicode scalar value.");
        }
    }

    static bool IsVariationSelector(int value)
    {
        return (value >= 0xFE00 && value <= 0xFE0F) ||
            (value >= 0xE0100 && value <= 0xE01EF);
    }
}
