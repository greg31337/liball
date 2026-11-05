namespace ttf;

internal class SfntKernPair
{
    int leftGlyphId;
    int rightGlyphId;
    int value;

    internal SfntKernPair(int leftGlyphId, int rightGlyphId, int value)
    {
        this.leftGlyphId = leftGlyphId;
        this.rightGlyphId = rightGlyphId;
        this.value = value;
    }

    internal int GetLeftGlyphId()
    {
        return leftGlyphId;
    }

    internal int GetRightGlyphId()
    {
        return rightGlyphId;
    }

    internal int GetValue()
    {
        return value;
    }

    internal uint GetKey()
    {
        return ((uint)leftGlyphId << 16) | (uint)rightGlyphId;
    }
}

internal class SfntKernFormat0Data
{
    int searchRange;
    int entrySelector;
    int rangeShift;
    SfntKernPair[] pairs;

    internal SfntKernFormat0Data(
        int searchRange,
        int entrySelector,
        int rangeShift,
        SfntKernPair[] pairs)
    {
        this.searchRange = searchRange;
        this.entrySelector = entrySelector;
        this.rangeShift = rangeShift;
        this.pairs = pairs;
    }

    internal int GetPairCount()
    {
        return pairs.Length;
    }

    internal SfntKernPair GetPair(int index)
    {
        return pairs[index];
    }

    internal int GetSearchRange()
    {
        return searchRange;
    }

    internal int GetEntrySelector()
    {
        return entrySelector;
    }

    internal int GetRangeShift()
    {
        return rangeShift;
    }

    internal int GetKerningValue(int leftGlyphId, int rightGlyphId)
    {
        uint key = ((uint)leftGlyphId << 16) | (uint)rightGlyphId;
        int low = 0;
        int high = pairs.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = pairs[middle].GetKey();

            if (candidate == key)
            {
                return pairs[middle].GetValue();
            }

            if (candidate < key)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return 0;
    }
}

internal class SfntKernClassTable
{
    int firstGlyphId;
    int defaultOffset;
    int[] offsets;

    internal SfntKernClassTable(
        int firstGlyphId,
        int defaultOffset,
        int[] offsets)
    {
        this.firstGlyphId = firstGlyphId;
        this.defaultOffset = defaultOffset;
        this.offsets = offsets;
    }

    internal int GetFirstGlyphId()
    {
        return firstGlyphId;
    }

    internal int GetGlyphCount()
    {
        return offsets.Length;
    }

    internal int GetDefaultOffset()
    {
        return defaultOffset;
    }

    internal int GetOffset(int index)
    {
        return offsets[index];
    }

    internal int GetOffsetForGlyph(int glyphId)
    {
        int index = glyphId - firstGlyphId;

        if (index < 0 || index >= offsets.Length)
        {
            return defaultOffset;
        }

        return offsets[index];
    }
}

internal class SfntKernFormat2Data
{
    int rowWidth;
    int kerningArrayOffset;
    int leftOffsetOrigin;
    SfntKernClassTable leftClassTable;
    SfntKernClassTable rightClassTable;
    int[] values;

    internal SfntKernFormat2Data(
        int rowWidth,
        int kerningArrayOffset,
        int leftOffsetOrigin,
        SfntKernClassTable leftClassTable,
        SfntKernClassTable rightClassTable,
        int[] values)
    {
        this.rowWidth = rowWidth;
        this.kerningArrayOffset = kerningArrayOffset;
        this.leftOffsetOrigin = leftOffsetOrigin;
        this.leftClassTable = leftClassTable;
        this.rightClassTable = rightClassTable;
        this.values = values;
    }

    internal int GetRowWidth()
    {
        return rowWidth;
    }

    internal int GetKerningArrayOffset()
    {
        return kerningArrayOffset;
    }

    internal SfntKernClassTable GetLeftClassTable()
    {
        return leftClassTable;
    }

    internal SfntKernClassTable GetRightClassTable()
    {
        return rightClassTable;
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal int GetValue(int index)
    {
        return values[index];
    }

    internal int GetKerningValue(int leftGlyphId, int rightGlyphId)
    {
        int leftOffset = leftClassTable.GetOffsetForGlyph(leftGlyphId);
        int rightOffset = rightClassTable.GetOffsetForGlyph(rightGlyphId);
        int valueOffset = leftOffset + rightOffset - leftOffsetOrigin;
        return values[valueOffset / 2];
    }
}

internal class SfntKernStateClassTable
{
    int firstGlyphId;
    int[] classCodes;

    internal SfntKernStateClassTable(int firstGlyphId, int[] classCodes)
    {
        this.firstGlyphId = firstGlyphId;
        this.classCodes = classCodes;
    }

    internal int GetFirstGlyphId()
    {
        return firstGlyphId;
    }

    internal int GetGlyphCount()
    {
        return classCodes.Length;
    }

    internal int GetClassCode(int index)
    {
        return classCodes[index];
    }

    internal int GetClassForGlyph(int glyphId)
    {
        int index = glyphId - firstGlyphId;

        if (index < 0 || index >= classCodes.Length)
        {
            return 1;
        }

        return classCodes[index];
    }
}

internal class SfntKernStateEntry
{
    int newStateOffset;
    int newStateIndex;
    int flags;

    internal SfntKernStateEntry(
        int newStateOffset,
        int newStateIndex,
        int flags)
    {
        this.newStateOffset = newStateOffset;
        this.newStateIndex = newStateIndex;
        this.flags = flags;
    }

    internal int GetNewStateOffset()
    {
        return newStateOffset;
    }

    internal int GetNewStateIndex()
    {
        return newStateIndex;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool PushesGlyph()
    {
        return (flags & 0x8000) != 0;
    }

    internal bool DoesNotAdvance()
    {
        return (flags & 0x4000) != 0;
    }

    internal int GetValueOffset()
    {
        return flags & 0x3FFF;
    }
}

internal class SfntKernFormat1Data
{
    int classTableOffset;
    int stateArrayOffset;
    int stateDataOffset;
    int entryTableOffset;
    int valueTableOffset;
    int classCount;
    int stateCount;
    int initialStateIndex;
    SfntKernStateClassTable classTable;
    int[] stateEntries;
    SfntKernStateEntry[] entries;
    int[] values;

    internal SfntKernFormat1Data(
        int classTableOffset,
        int stateArrayOffset,
        int stateDataOffset,
        int entryTableOffset,
        int valueTableOffset,
        int classCount,
        int stateCount,
        int initialStateIndex,
        SfntKernStateClassTable classTable,
        int[] stateEntries,
        SfntKernStateEntry[] entries,
        int[] values)
    {
        this.classTableOffset = classTableOffset;
        this.stateArrayOffset = stateArrayOffset;
        this.stateDataOffset = stateDataOffset;
        this.entryTableOffset = entryTableOffset;
        this.valueTableOffset = valueTableOffset;
        this.classCount = classCount;
        this.stateCount = stateCount;
        this.initialStateIndex = initialStateIndex;
        this.classTable = classTable;
        this.stateEntries = stateEntries;
        this.entries = entries;
        this.values = values;
    }

    internal int GetClassTableOffset()
    {
        return classTableOffset;
    }

    internal int GetStateArrayOffset()
    {
        return stateArrayOffset;
    }

    internal int GetStateDataOffset()
    {
        return stateDataOffset;
    }

    internal int GetEntryTableOffset()
    {
        return entryTableOffset;
    }

    internal int GetValueTableOffset()
    {
        return valueTableOffset;
    }

    internal int GetClassCount()
    {
        return classCount;
    }

    internal int GetStateCount()
    {
        return stateCount;
    }

    internal int GetInitialStateIndex()
    {
        return initialStateIndex;
    }

    internal SfntKernStateClassTable GetClassTable()
    {
        return classTable;
    }

    internal int GetStateEntryIndex(int stateIndex, int classIndex)
    {
        return stateEntries[stateIndex * classCount + classIndex];
    }

    internal int GetEntryCount()
    {
        return entries.Length;
    }

    internal SfntKernStateEntry GetEntry(int index)
    {
        return entries[index];
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal int GetValue(int index)
    {
        return values[index];
    }
}

internal class SfntKernFormat3Data
{
    int[] values;
    int[] leftClasses;
    int[] rightClasses;
    int leftClassCount;
    int rightClassCount;
    int[] valueIndexes;

    internal SfntKernFormat3Data(
        int[] values,
        int[] leftClasses,
        int[] rightClasses,
        int leftClassCount,
        int rightClassCount,
        int[] valueIndexes)
    {
        this.values = values;
        this.leftClasses = leftClasses;
        this.rightClasses = rightClasses;
        this.leftClassCount = leftClassCount;
        this.rightClassCount = rightClassCount;
        this.valueIndexes = valueIndexes;
    }

    internal int GetGlyphCount()
    {
        return leftClasses.Length;
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal int GetValue(int index)
    {
        return values[index];
    }

    internal int GetLeftClassCount()
    {
        return leftClassCount;
    }

    internal int GetRightClassCount()
    {
        return rightClassCount;
    }

    internal int GetLeftClass(int glyphId)
    {
        return leftClasses[glyphId];
    }

    internal int GetRightClass(int glyphId)
    {
        return rightClasses[glyphId];
    }

    internal int GetValueIndex(int leftClass, int rightClass)
    {
        return valueIndexes[leftClass * rightClassCount + rightClass];
    }

    internal int GetKerningValue(int leftGlyphId, int rightGlyphId)
    {
        int leftClass = leftClasses[leftGlyphId];
        int rightClass = rightClasses[rightGlyphId];
        int valueIndex = GetValueIndex(leftClass, rightClass);
        return values[valueIndex];
    }
}

internal class SfntKernSubtable
{
    int length;
    int coverage;
    int tupleIndex;
    int format;
    bool apple;
    SfntKernFormat0Data? format0;
    SfntKernFormat1Data? format1;
    SfntKernFormat2Data? format2;
    SfntKernFormat3Data? format3;

    internal SfntKernSubtable(
        int length,
        int coverage,
        int tupleIndex,
        int format,
        bool apple,
        SfntKernFormat0Data? format0,
        SfntKernFormat1Data? format1,
        SfntKernFormat2Data? format2,
        SfntKernFormat3Data? format3)
    {
        this.length = length;
        this.coverage = coverage;
        this.tupleIndex = tupleIndex;
        this.format = format;
        this.apple = apple;
        this.format0 = format0;
        this.format1 = format1;
        this.format2 = format2;
        this.format3 = format3;
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetCoverage()
    {
        return coverage;
    }

    internal int GetTupleIndex()
    {
        return tupleIndex;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal bool IsAppleFormat()
    {
        return apple;
    }

    internal bool IsHorizontal()
    {
        return apple ? (coverage & 0x8000) == 0 : (coverage & 0x0001) != 0;
    }

    internal bool IsVertical()
    {
        return !IsHorizontal();
    }

    internal bool ContainsMinimumValues()
    {
        return !apple && (coverage & 0x0002) != 0;
    }

    internal bool IsCrossStream()
    {
        return apple ? (coverage & 0x4000) != 0 : (coverage & 0x0004) != 0;
    }

    internal bool OverridesPreviousValues()
    {
        return !apple && (coverage & 0x0008) != 0;
    }

    internal bool IsVariationData()
    {
        return apple && (coverage & 0x2000) != 0;
    }

    internal SfntKernFormat0Data? GetFormat0()
    {
        return format0;
    }

    internal SfntKernFormat1Data? GetFormat1()
    {
        return format1;
    }

    internal SfntKernFormat2Data? GetFormat2()
    {
        return format2;
    }

    internal SfntKernFormat3Data? GetFormat3()
    {
        return format3;
    }
}

internal class SfntKernTable
{
    uint version;
    bool apple;
    SfntKernSubtable[] subtables;

    internal SfntKernTable(uint version, bool apple, SfntKernSubtable[] subtables)
    {
        this.version = version;
        this.apple = apple;
        this.subtables = subtables;
    }

    internal uint GetVersionRaw()
    {
        return version;
    }

    internal bool IsAppleFormat()
    {
        return apple;
    }

    internal int GetSubtableCount()
    {
        return subtables.Length;
    }

    internal SfntKernSubtable GetSubtable(int index)
    {
        return subtables[index];
    }
}
