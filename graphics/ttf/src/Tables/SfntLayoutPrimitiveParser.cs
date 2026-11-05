namespace ttf;

internal class SfntLayoutPrimitiveParserContext
{
    FontDataSlice tableData;
    FontLimits limits;

    internal SfntLayoutPrimitiveParserContext(
        FontDataSlice tableData,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.limits = limits;
    }

    internal SfntDeviceOrVariationIndex ParseDeviceOrVariationIndex(
        int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A Device or VariationIndex table is truncated.");
        int firstValue = reader.ReadUInt16();
        int secondValue = reader.ReadUInt16();
        int deltaFormat = reader.ReadUInt16();

        if (deltaFormat == 0x8000)
        {
            return SfntDeviceOrVariationIndex.Create(
                new SfntVariationIndex(firstValue, secondValue));
        }

        if (deltaFormat < 1 || deltaFormat > 3)
        {
            throw FontFormatException.Create(
                "A Device table delta format is invalid.");
        }

        if (secondValue < firstValue)
        {
            throw FontFormatException.Create(
                "A Device table size range is invalid.");
        }

        int bitCount = 1 << deltaFormat;
        int valuesPerWord = 16 / bitCount;
        int deltaCount = secondValue - firstValue + 1;
        int wordCount = (deltaCount + valuesPerWord - 1) /
            valuesPerWord;
        ValidateStoredCount(
            wordCount,
            "A Device table exceeds the configured entry limit.");
        int length = CheckedFontMath.GetRangeEnd(6, wordCount, 2);
        reader = CreateReader(
            offset,
            length,
            "A Device table delta array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] packedValues = new int[wordCount];
        int index = 0;

        while (index < wordCount)
        {
            packedValues[index] = reader.ReadUInt16();
            index += 1;
        }

        return SfntDeviceOrVariationIndex.Create(
            new SfntDeviceTable(
                firstValue,
                secondValue,
                deltaFormat,
                packedValues));
    }

    internal SfntLayoutAnchor ParseAnchor(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "An Anchor table is truncated.");
        int format = reader.ReadUInt16();
        int xCoordinate = reader.ReadInt16();
        int yCoordinate = reader.ReadInt16();

        if (format == 1)
        {
            return new SfntLayoutAnchor(
                format,
                xCoordinate,
                yCoordinate,
                0,
                0,
                0,
                0,
                0,
                null,
                null);
        }

        if (format == 2)
        {
            reader = CreateReader(
                offset,
                8,
                "A format 2 Anchor table is truncated.");
            reader.ReadUInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            int anchorPoint = reader.ReadUInt16();
            return new SfntLayoutAnchor(
                format,
                xCoordinate,
                yCoordinate,
                anchorPoint,
                0,
                0,
                0,
                0,
                null,
                null);
        }

        if (format == 3)
        {
            reader = CreateReader(
                offset,
                10,
                "A format 3 Anchor table is truncated.");
            reader.ReadUInt16();
            reader.ReadInt16();
            reader.ReadInt16();
            int xAdjustmentOffset = reader.ReadUInt16();
            int yAdjustmentOffset = reader.ReadUInt16();
            int xAdjustmentTableOffset = ResolveOptionalOffset(
                offset,
                xAdjustmentOffset,
                10,
                6,
                "An Anchor x Device or VariationIndex offset is invalid.");
            int yAdjustmentTableOffset = ResolveOptionalOffset(
                offset,
                yAdjustmentOffset,
                10,
                6,
                "An Anchor y Device or VariationIndex offset is invalid.");
            SfntDeviceOrVariationIndex? xAdjustment =
                xAdjustmentOffset == 0 ? null :
                ParseDeviceOrVariationIndex(xAdjustmentTableOffset);
            SfntDeviceOrVariationIndex? yAdjustment =
                yAdjustmentOffset == 0 ? null :
                ParseDeviceOrVariationIndex(yAdjustmentTableOffset);
            return new SfntLayoutAnchor(
                format,
                xCoordinate,
                yCoordinate,
                0,
                xAdjustmentOffset,
                yAdjustmentOffset,
                xAdjustmentTableOffset,
                yAdjustmentTableOffset,
                xAdjustment,
                yAdjustment);
        }

        throw FontFormatException.Create(
            "The Anchor table format is not supported.");
    }

    internal SfntMarkArray ParseMarkArray(
        int offset,
        int markClassCount)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A MarkArray header is truncated.");
        int markCount = reader.ReadUInt16();
        ValidateStoredCount(
            markCount,
            "A MarkArray exceeds the configured entry limit.");
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            markCount,
            4);
        reader = CreateReader(
            offset,
            headerLength,
            "A MarkRecord array is truncated.");
        reader.ReadUInt16();
        int[] markClasses = new int[markCount];
        int[] anchorOffsets = new int[markCount];
        int index = 0;

        while (index < markCount)
        {
            markClasses[index] = reader.ReadUInt16();
            anchorOffsets[index] = reader.ReadUInt16();

            if (markClasses[index] >= markClassCount)
            {
                throw FontFormatException.Create(
                    "A MarkRecord class is invalid.");
            }

            index += 1;
        }

        SfntMarkRecord[] records = new SfntMarkRecord[markCount];
        index = 0;

        while (index < markCount)
        {
            int anchorTableOffset = ResolveRequiredOffset(
                offset,
                anchorOffsets[index],
                headerLength,
                6,
                "A MarkRecord Anchor offset is invalid.");
            records[index] = new SfntMarkRecord(
                markClasses[index],
                anchorOffsets[index],
                anchorTableOffset,
                ParseAnchor(anchorTableOffset));
            index += 1;
        }

        return new SfntMarkArray(records);
    }

    internal SfntSequenceLookupRecords ParseSequenceLookupRecords(
        int offset,
        int recordCount,
        int inputGlyphCount,
        int lookupCount)
    {
        ValidateStoredCount(
            recordCount,
            "A sequence lookup array exceeds the configured entry limit.");
        int length = CheckedFontMath.MultiplyCount(recordCount, 4);
        BigEndianReader reader = CreateReader(
            offset,
            length,
            "A SequenceLookup record array is truncated.");
        SfntSequenceLookupRecord[] records =
            new SfntSequenceLookupRecord[recordCount];
        int index = 0;

        while (index < recordCount)
        {
            int sequenceIndex = reader.ReadUInt16();
            int lookupListIndex = reader.ReadUInt16();

            if (sequenceIndex >= inputGlyphCount)
            {
                throw FontFormatException.Create(
                    "A SequenceLookup sequence index is invalid.");
            }

            if (lookupListIndex >= lookupCount)
            {
                throw FontFormatException.Create(
                    "A SequenceLookup lookup-list index is invalid.");
            }

            records[index] = new SfntSequenceLookupRecord(
                sequenceIndex,
                lookupListIndex);
            index += 1;
        }

        return new SfntSequenceLookupRecords(records);
    }

    internal SfntExtensionLookup ParseExtension(
        int offset,
        int extensionLookupType)
    {
        BigEndianReader reader = CreateReader(
            offset,
            8,
            "An Extension lookup subtable is truncated.");
        int format = reader.ReadUInt16();
        int lookupType = reader.ReadUInt16();
        int extensionOffset = ReadUInt32Offset(reader);

        if (format != 1)
        {
            throw FontFormatException.Create(
                "An Extension lookup subtable format is invalid.");
        }

        if (lookupType == 0 || lookupType == extensionLookupType)
        {
            throw FontFormatException.Create(
                "An Extension lookup type is invalid.");
        }

        int extensionTableOffset = ResolveRequiredOffset(
            offset,
            extensionOffset,
            8,
            2,
            "An Extension lookup offset is invalid.");
        return new SfntExtensionLookup(
            lookupType,
            extensionOffset,
            extensionTableOffset);
    }

    BigEndianReader CreateReader(
        int offset,
        int requiredLength,
        string message)
    {
        if (offset < 0 ||
            requiredLength < 0 ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return new BigEndianReader(tableData.GetSlice(
            offset,
            tableData.GetLength() - offset));
    }

    int ResolveRequiredOffset(
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset == 0)
        {
            throw FontFormatException.Create(message);
        }

        return ResolveOffset(
            baseOffset,
            relativeOffset,
            minimumRelativeOffset,
            requiredLength,
            message);
    }

    int ResolveOptionalOffset(
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset == 0)
        {
            return 0;
        }

        return ResolveOffset(
            baseOffset,
            relativeOffset,
            minimumRelativeOffset,
            requiredLength,
            message);
    }

    int ResolveOffset(
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(message);
        }

        int absoluteOffset = CheckedFontMath.AddOffset(
            baseOffset,
            relativeOffset);

        if (absoluteOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return absoluteOffset;
    }

    void ValidateStoredCount(int count, string message)
    {
        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(message);
        }
    }

    static int ReadUInt32Offset(BigEndianReader reader)
    {
        uint value = reader.ReadUInt32();

        if (value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A layout Offset32 value exceeds the supported range.");
        }

        return (int)value;
    }
}

internal static class SfntLayoutPrimitiveParser
{
    internal static SfntDeviceOrVariationIndex ParseDeviceOrVariationIndex(
        FontDataSlice tableData,
        int offset,
        FontLimits limits)
    {
        return CreateContext(tableData, limits)
            .ParseDeviceOrVariationIndex(offset);
    }

    internal static SfntLayoutAnchor ParseAnchor(
        FontDataSlice tableData,
        int offset,
        FontLimits limits)
    {
        return CreateContext(tableData, limits).ParseAnchor(offset);
    }

    internal static SfntMarkArray ParseMarkArray(
        FontDataSlice tableData,
        int offset,
        int markClassCount,
        FontLimits limits)
    {
        if (markClassCount < 0 || markClassCount > 65535)
        {
            throw new System.ArgumentOutOfRangeException("markClassCount");
        }

        return CreateContext(tableData, limits).ParseMarkArray(
            offset,
            markClassCount);
    }

    internal static SfntSequenceLookupRecords ParseSequenceLookupRecords(
        FontDataSlice tableData,
        int offset,
        int recordCount,
        int inputGlyphCount,
        int lookupCount,
        FontLimits limits)
    {
        if (recordCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("recordCount");
        }

        if (inputGlyphCount <= 0 || inputGlyphCount > 65535)
        {
            throw new System.ArgumentOutOfRangeException("inputGlyphCount");
        }

        if (lookupCount < 0 || lookupCount > 65535)
        {
            throw new System.ArgumentOutOfRangeException("lookupCount");
        }

        return CreateContext(tableData, limits).ParseSequenceLookupRecords(
            offset,
            recordCount,
            inputGlyphCount,
            lookupCount);
    }

    internal static SfntExtensionLookup ParseExtension(
        FontDataSlice tableData,
        int offset,
        int extensionLookupType,
        FontLimits limits)
    {
        if (extensionLookupType <= 0 || extensionLookupType > 65535)
        {
            throw new System.ArgumentOutOfRangeException(
                "extensionLookupType");
        }

        return CreateContext(tableData, limits).ParseExtension(
            offset,
            extensionLookupType);
    }

    static SfntLayoutPrimitiveParserContext CreateContext(
        FontDataSlice tableData,
        FontLimits limits)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        return new SfntLayoutPrimitiveParserContext(tableData, limits);
    }
}
