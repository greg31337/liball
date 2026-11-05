namespace ttf;

internal static class SfntColrV1Parser
{
    const int HeaderLength = 34;
    const int BaseGlyphPaintRecordLength = 6;
    const int ClipRecordLength = 7;

    internal static SfntColrTable Parse(
        FontDataSlice tableData,
        BigEndianReader headerReader,
        int baseGlyphRecordCount,
        long baseGlyphRecordsOffset,
        long layerRecordsOffset,
        int layerRecordCount,
        FontLimits limits,
        SfntTableCache cache)
    {
        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The COLR version 1 header is truncated.");
        }

        long baseGlyphListOffsetValue = headerReader.ReadOffset32();
        long layerListOffsetValue = headerReader.ReadOffset32();
        long clipListOffsetValue = headerReader.ReadOffset32();
        long varIndexMapOffsetValue = headerReader.ReadOffset32();
        long variationStoreOffsetValue = headerReader.ReadOffset32();
        int baseGlyphListOffset = ResolveTopLevelOffset(
            tableData,
            baseGlyphListOffsetValue,
            false,
            4,
            "The COLR BaseGlyphList offset is invalid.");
        int layerListOffset = ResolveTopLevelOffset(
            tableData,
            layerListOffsetValue,
            true,
            4,
            "The COLR LayerList offset is invalid.");
        int clipListOffset = ResolveTopLevelOffset(
            tableData,
            clipListOffsetValue,
            true,
            5,
            "The COLR ClipList offset is invalid.");
        int varIndexMapOffset = ResolveTopLevelOffset(
            tableData,
            varIndexMapOffsetValue,
            true,
            4,
            "The COLR DeltaSetIndexMap offset is invalid.");
        int variationStoreOffset = ResolveTopLevelOffset(
            tableData,
            variationStoreOffsetValue,
            true,
            8,
            "The COLR ItemVariationStore offset is invalid.");
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The COLR table requires a supported maxp table.");
        }

        SfntCpalTable? cpal = SfntCpalTableParser.FindTable(cache);

        if (cpal == null)
        {
            throw FontFormatException.Create(
                "The COLR table requires a supported CPAL table.");
        }

        int glyphCount = maxp.GetGlyphCount();
        SfntColrParsingBudget parsingBudget = new SfntColrParsingBudget(
            limits.GetMaxCoverageEntries());
        SfntColrLayerRecord[] layerRecords = ParseCompatibilityLayerRecords(
            tableData,
            layerRecordsOffset,
            layerRecordCount,
            glyphCount,
            cpal.GetPaletteEntryCount(),
            parsingBudget);
        SfntColrBaseGlyphRecord[] baseGlyphRecords =
            ParseCompatibilityBaseGlyphRecords(
                tableData,
                baseGlyphRecordsOffset,
                baseGlyphRecordCount,
                glyphCount,
                layerRecords,
                parsingBudget);
        SfntColrVariationData variationData = ParseVariationData(
            tableData,
            varIndexMapOffset,
            variationStoreOffset,
            limits,
            cache);
        SfntColrPaintParsingContext context = new SfntColrPaintParsingContext(
            tableData,
            glyphCount,
            cpal.GetPaletteEntryCount(),
            limits,
            variationData,
            parsingBudget);
        SfntColrBaseGlyphPaintData baseGlyphPaintData = ParseBaseGlyphList(
            tableData,
            baseGlyphListOffset,
            glyphCount,
            limits,
            context,
            parsingBudget);
        int[] layerPaintOffsets = ParseLayerList(
            tableData,
            layerListOffset,
            limits,
            context,
            parsingBudget);
        context.SetLayerPaintCount(layerPaintOffsets.Length);
        SfntColrClipList? clipList = ParseClipList(
            tableData,
            clipListOffset,
            glyphCount,
            limits,
            variationData,
            parsingBudget);
        context.ParseAll();
        SfntColrPaint[] paints = context.CopyPaintsSortedByOffset();
        SfntColrBaseGlyphPaintRecord[] baseGlyphPaintRecords =
            CreateBaseGlyphPaintRecords(
                baseGlyphPaintData.GetGlyphIds(),
                baseGlyphPaintData.GetPaintOffsets(),
                context);
        ValidateColrGlyphReferences(paints, baseGlyphPaintRecords);
        SfntColrLayerList? layerList = CreateLayerList(
            layerListOffset,
            layerPaintOffsets,
            context);
        return SfntColrTable.Create(
            1,
            glyphCount,
            baseGlyphRecords,
            layerRecords,
            cpal,
            baseGlyphPaintRecords,
            layerList,
            clipList,
            paints,
            variationData);
    }

    static SfntColrLayerRecord[] ParseCompatibilityLayerRecords(
        FontDataSlice tableData,
        long offset,
        int count,
        int glyphCount,
        int paletteEntryCount,
        SfntColrParsingBudget parsingBudget)
    {
        if (count == 0)
        {
            if (offset != 0)
            {
                ValidateArrayOffset(
                    tableData,
                    offset,
                    0,
                    4,
                    "The empty COLR compatibility layer array offset is invalid.");
            }

            return new SfntColrLayerRecord[0];
        }

        parsingBudget.AddEntries(count);

        ValidateArrayOffset(
            tableData,
            offset,
            count,
            4,
            "The COLR compatibility layer array offset is invalid.");
        return SfntColrTableParser.ParseLayerRecords(
            tableData,
            offset,
            count,
            glyphCount,
            paletteEntryCount);
    }

    static SfntColrBaseGlyphRecord[] ParseCompatibilityBaseGlyphRecords(
        FontDataSlice tableData,
        long offset,
        int count,
        int glyphCount,
        SfntColrLayerRecord[] layerRecords,
        SfntColrParsingBudget parsingBudget)
    {
        if (count == 0)
        {
            if (offset != 0)
            {
                ValidateArrayOffset(
                    tableData,
                    offset,
                    0,
                    6,
                    "The empty COLR compatibility base-glyph array offset is invalid.");
            }

            return new SfntColrBaseGlyphRecord[0];
        }

        parsingBudget.AddEntries(count);

        ValidateArrayOffset(
            tableData,
            offset,
            count,
            6,
            "The COLR compatibility base-glyph array offset is invalid.");
        return SfntColrTableParser.ParseBaseGlyphRecords(
            tableData,
            offset,
            count,
            glyphCount,
            layerRecords);
    }

    static SfntColrVariationData ParseVariationData(
        FontDataSlice tableData,
        int mapOffset,
        int storeOffset,
        FontLimits limits,
        SfntTableCache cache)
    {
        if (mapOffset != 0 && storeOffset == 0)
        {
            throw FontFormatException.Create(
                "A COLR DeltaSetIndexMap requires an ItemVariationStore.");
        }

        if (storeOffset == 0)
        {
            return new SfntColrVariationData(null, null);
        }

        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create(
                "A variable COLR table requires a supported fvar table.");
        }

        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        SfntDeltaSetIndexMap? map = mapOffset == 0 ? null :
            SfntDeltaSetIndexMapParser.Parse(
                tableData,
                mapOffset,
                HeaderLength,
                budget);
        SfntItemVariationStore store = SfntItemVariationStoreParser.Parse(
            tableData,
            storeOffset,
            HeaderLength,
            fvar.GetAxisCount(),
            limits,
            budget);
        return new SfntColrVariationData(map, store);
    }

    static SfntColrBaseGlyphPaintData ParseBaseGlyphList(
        FontDataSlice tableData,
        int offset,
        int glyphCount,
        FontLimits limits,
        SfntColrPaintParsingContext context,
        SfntColrParsingBudget parsingBudget)
    {
        BigEndianReader reader = CreateReader(tableData, offset);
        int count = ReadBoundedCount(
            reader.ReadUInt32(),
            limits,
            "The COLR BaseGlyphList count exceeds the configured coverage-entry limit.");
        parsingBudget.AddEntries(count);
        int recordsLength = CheckedFontMath.MultiplyCount(
            count,
            BaseGlyphPaintRecordLength);
        int headerLength = CheckedFontMath.AddOffset(4, recordsLength);

        if (headerLength > tableData.GetLength() - offset)
        {
            throw FontFormatException.Create(
                "The COLR BaseGlyphList records are truncated.");
        }

        int[] glyphIds = new int[count];
        int[] paintOffsets = new int[count];
        int previousGlyphId = -1;
        int index = 0;

        while (index < count)
        {
            int glyphId = reader.ReadUInt16();
            long relativePaintOffset = reader.ReadOffset32();

            if (glyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A COLR BaseGlyphPaintRecord references a missing glyph.");
            }

            if (glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "The COLR BaseGlyphPaintRecords are not strictly sorted.");
            }

            int paintOffset = ResolveRelativeOffset(
                tableData,
                offset,
                relativePaintOffset,
                headerLength,
                1,
                "A COLR base-glyph paint offset is invalid.");
            glyphIds[index] = glyphId;
            paintOffsets[index] = paintOffset;
            context.AddPaintOffset(paintOffset);
            previousGlyphId = glyphId;
            index += 1;
        }

        return new SfntColrBaseGlyphPaintData(glyphIds, paintOffsets);
    }

    static int[] ParseLayerList(
        FontDataSlice tableData,
        int offset,
        FontLimits limits,
        SfntColrPaintParsingContext context,
        SfntColrParsingBudget parsingBudget)
    {
        if (offset == 0)
        {
            return new int[0];
        }

        BigEndianReader reader = CreateReader(tableData, offset);
        int count = ReadBoundedCount(
            reader.ReadUInt32(),
            limits,
            "The COLR LayerList count exceeds the configured coverage-entry limit.");
        parsingBudget.AddEntries(count);
        int offsetsLength = CheckedFontMath.MultiplyCount(count, 4);
        int headerLength = CheckedFontMath.AddOffset(4, offsetsLength);

        if (headerLength > tableData.GetLength() - offset)
        {
            throw FontFormatException.Create("The COLR LayerList is truncated.");
        }

        int[] paintOffsets = new int[count];
        int index = 0;

        while (index < paintOffsets.Length)
        {
            paintOffsets[index] = ResolveRelativeOffset(
                tableData,
                offset,
                reader.ReadOffset32(),
                headerLength,
                1,
                "A COLR layer-list paint offset is invalid.");
            context.AddPaintOffset(paintOffsets[index]);
            index += 1;
        }

        return paintOffsets;
    }

    static SfntColrClipList? ParseClipList(
        FontDataSlice tableData,
        int offset,
        int glyphCount,
        FontLimits limits,
        SfntColrVariationData variationData,
        SfntColrParsingBudget parsingBudget)
    {
        if (offset == 0)
        {
            return null;
        }

        BigEndianReader reader = CreateReader(tableData, offset);
        int format = reader.ReadUInt8();

        if (format != 1)
        {
            throw FontFormatException.Create("The COLR ClipList format is invalid.");
        }

        int count = ReadBoundedCount(
            reader.ReadUInt32(),
            limits,
            "The COLR clip count exceeds the configured coverage-entry limit.");
        parsingBudget.AddEntries(count);
        int recordsLength = CheckedFontMath.MultiplyCount(
            count,
            ClipRecordLength);
        int headerLength = CheckedFontMath.AddOffset(5, recordsLength);

        if (headerLength > tableData.GetLength() - offset)
        {
            throw FontFormatException.Create("The COLR ClipList is truncated.");
        }

        SfntColrClipRecord[] records = new SfntColrClipRecord[count];
        int previousEndGlyphId = -1;
        int index = 0;

        while (index < records.Length)
        {
            int startGlyphId = reader.ReadUInt16();
            int endGlyphId = reader.ReadUInt16();
            int clipBoxOffset = ResolveRelativeOffset(
                tableData,
                offset,
                reader.ReadOffset24(),
                headerLength,
                1,
                "A COLR ClipBox offset is invalid.");

            if (startGlyphId > endGlyphId || endGlyphId >= glyphCount ||
                startGlyphId <= previousEndGlyphId)
            {
                throw FontFormatException.Create(
                    "The COLR clip ranges are invalid or overlapping.");
            }

            records[index] = new SfntColrClipRecord(
                startGlyphId,
                endGlyphId,
                clipBoxOffset,
                ParseClipBox(tableData, clipBoxOffset, variationData));
            previousEndGlyphId = endGlyphId;
            index += 1;
        }

        return new SfntColrClipList(format, records);
    }

    static SfntColrClipBox ParseClipBox(
        FontDataSlice tableData,
        int offset,
        SfntColrVariationData variationData)
    {
        BigEndianReader reader = CreateReader(tableData, offset);
        int format = reader.ReadUInt8();

        if (format != 1 && format != 2)
        {
            return new SfntColrClipBox(
                format,
                false,
                -1,
                new SfntColrVariableValue[0]);
        }

        int length = format == 1 ? 9 : 13;
        RequireLength(tableData, offset, length, "A COLR ClipBox is truncated.");
        long[] rawValues = ReadInt16Values(reader, 4);
        long variationIndexBase = format == 2 ? reader.ReadUInt32() : -1;
        SfntColrVariableValue[] values = CreateValues(
            rawValues,
            CreateDivisors(4, 1),
            -1,
            variationIndexBase,
            format == 2,
            variationData);

        if (values[0].GetValue() > values[2].GetValue() ||
            values[1].GetValue() > values[3].GetValue())
        {
            throw FontFormatException.Create("A COLR ClipBox has inverted bounds.");
        }

        return new SfntColrClipBox(
            format,
            true,
            variationIndexBase,
            values);
    }

    static SfntColrBaseGlyphPaintRecord[] CreateBaseGlyphPaintRecords(
        int[] glyphIds,
        int[] paintOffsets,
        SfntColrPaintParsingContext context)
    {
        SfntColrBaseGlyphPaintRecord[] records =
            new SfntColrBaseGlyphPaintRecord[glyphIds.Length];
        int index = 0;

        while (index < records.Length)
        {
            records[index] = new SfntColrBaseGlyphPaintRecord(
                glyphIds[index],
                paintOffsets[index],
                context.FindPaint(paintOffsets[index]));
            index += 1;
        }

        return records;
    }

    static SfntColrLayerList? CreateLayerList(
        int offset,
        int[] paintOffsets,
        SfntColrPaintParsingContext context)
    {
        if (offset == 0)
        {
            return null;
        }

        SfntColrPaint[] paints = new SfntColrPaint[paintOffsets.Length];
        int index = 0;

        while (index < paints.Length)
        {
            paints[index] = context.FindPaint(paintOffsets[index]);
            index += 1;
        }

        return new SfntColrLayerList(offset, paints);
    }

    static void ValidateColrGlyphReferences(
        SfntColrPaint[] paints,
        SfntColrBaseGlyphPaintRecord[] baseGlyphPaintRecords)
    {
        int index = 0;

        while (index < paints.Length)
        {
            if (paints[index].GetFormat() == 11 &&
                FindBaseGlyphPaintRecord(
                    baseGlyphPaintRecords,
                    paints[index].GetGlyphId()) == null)
            {
                throw FontFormatException.Create(
                    "A PaintColrGlyph references a missing BaseGlyphPaintRecord.");
            }

            index += 1;
        }
    }

    static SfntColrBaseGlyphPaintRecord? FindBaseGlyphPaintRecord(
        SfntColrBaseGlyphPaintRecord[] records,
        int glyphId)
    {
        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = records[middle].GetGlyphId();

            if (candidate == glyphId)
            {
                return records[middle];
            }

            if (candidate < glyphId)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    internal static int ResolveRelativeOffset(
        FontDataSlice tableData,
        int baseOffset,
        long relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        long absoluteOffset = (long)baseOffset + relativeOffset;

        if (relativeOffset < minimumRelativeOffset ||
            absoluteOffset > int.MaxValue ||
            absoluteOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return (int)absoluteOffset;
    }

    internal static void RequireLength(
        FontDataSlice tableData,
        int offset,
        int length,
        string message)
    {
        if (offset < 0 || length < 0 || offset > tableData.GetLength() - length)
        {
            throw FontFormatException.Create(message);
        }
    }

    internal static BigEndianReader CreateReader(
        FontDataSlice tableData,
        int offset)
    {
        return new BigEndianReader(
            tableData.GetSlice(offset, tableData.GetLength() - offset));
    }

    internal static long[] ReadInt16Values(BigEndianReader reader, int count)
    {
        long[] values = new long[count];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = reader.ReadInt16();
            index += 1;
        }

        return values;
    }

    internal static double[] CreateDivisors(int count, double divisor)
    {
        double[] divisors = new double[count];
        int index = 0;

        while (index < divisors.Length)
        {
            divisors[index] = divisor;
            index += 1;
        }

        return divisors;
    }

    internal static SfntColrVariableValue[] CreateValues(
        long[] rawValues,
        double[] divisors,
        int alphaIndex,
        long variationIndexBase,
        bool variable,
        SfntColrVariationData variationData)
    {
        if (rawValues.Length != divisors.Length)
        {
            throw new System.ArgumentException(
                "The COLR value and divisor counts do not match.",
                "divisors");
        }

        if (variable && !variationData.HasStore())
        {
            throw FontFormatException.Create(
                "A variable COLR structure requires an ItemVariationStore.");
        }

        SfntColrVariableValue[] values =
            new SfntColrVariableValue[rawValues.Length];
        int index = 0;

        while (index < values.Length)
        {
            long variationIndex = -1;

            if (variable)
            {
                if (variationIndexBase == uint.MaxValue)
                {
                    variationIndex = -1;
                }
                else
                {
                    variationIndex = variationIndexBase + index;

                    if (variationIndex > uint.MaxValue)
                    {
                        throw FontFormatException.Create(
                            "A COLR variation index sequence overflows.");
                    }
                }
            }

            bool isAlpha = index == alphaIndex;
            values[index] = new SfntColrVariableValue(
                rawValues[index],
                divisors[index],
                0,
                1,
                isAlpha,
                isAlpha,
                variationIndex,
                variationData);
            index += 1;
        }

        return values;
    }

    static int ResolveTopLevelOffset(
        FontDataSlice tableData,
        long offset,
        bool optional,
        int requiredLength,
        string message)
    {
        if (offset == 0 && optional)
        {
            return 0;
        }

        if (offset < HeaderLength || offset > int.MaxValue ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return (int)offset;
    }

    static int ReadBoundedCount(
        uint encodedCount,
        FontLimits limits,
        string message)
    {
        if (encodedCount > int.MaxValue ||
            encodedCount > (uint)limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(message);
        }

        return (int)encodedCount;
    }

    static void ValidateArrayOffset(
        FontDataSlice tableData,
        long offset,
        int count,
        int recordLength,
        string message)
    {
        long length = (long)count * recordLength;

        if (offset < HeaderLength || offset > int.MaxValue ||
            offset > tableData.GetLength() - length)
        {
            throw FontFormatException.Create(message);
        }
    }
}

internal class SfntColrParsingBudget
{
    int maximumEntries;
    int entries;

    internal SfntColrParsingBudget(int maximumEntries)
    {
        this.maximumEntries = maximumEntries;
    }

    internal void AddEntries(int count)
    {
        if (count < 0 || count > maximumEntries - entries)
        {
            throw FontLimitException.Create(
                "COLR structures exceed the configured coverage-entry limit.");
        }

        entries += count;
    }
}

internal class SfntColrBaseGlyphPaintData
{
    int[] glyphIds;
    int[] paintOffsets;

    internal SfntColrBaseGlyphPaintData(
        int[] glyphIds,
        int[] paintOffsets)
    {
        this.glyphIds = glyphIds;
        this.paintOffsets = paintOffsets;
    }

    internal int[] GetGlyphIds()
    {
        return glyphIds;
    }

    internal int[] GetPaintOffsets()
    {
        return paintOffsets;
    }
}

internal class SfntColrChildValuesData
{
    int[] childPaintOffsets;
    long variationIndexBase;
    SfntColrVariableValue[] values;

    internal SfntColrChildValuesData(
        int[] childPaintOffsets,
        long variationIndexBase,
        SfntColrVariableValue[] values)
    {
        this.childPaintOffsets = childPaintOffsets;
        this.variationIndexBase = variationIndexBase;
        this.values = values;
    }

    internal int[] GetChildPaintOffsets()
    {
        return childPaintOffsets;
    }

    internal long GetVariationIndexBase()
    {
        return variationIndexBase;
    }

    internal SfntColrVariableValue[] GetValues()
    {
        return values;
    }
}

internal class SfntColrPaintParsingContext
{
    FontDataSlice tableData;
    int glyphCount;
    int paletteEntryCount;
    int maximumPaintCount;
    int layerPaintCount;
    SfntColrVariationData variationData;
    SfntColrParsingBudget parsingBudget;
    System.Collections.Generic.Dictionary<int, SfntColrPaint> paints;
    System.Collections.Generic.List<int> pendingOffsets;

    internal SfntColrPaintParsingContext(
        FontDataSlice tableData,
        int glyphCount,
        int paletteEntryCount,
        FontLimits limits,
        SfntColrVariationData variationData,
        SfntColrParsingBudget parsingBudget)
    {
        this.tableData = tableData;
        this.glyphCount = glyphCount;
        this.paletteEntryCount = paletteEntryCount;
        maximumPaintCount = limits.GetMaxCoverageEntries();
        this.variationData = variationData;
        this.parsingBudget = parsingBudget;
        paints = new System.Collections.Generic.Dictionary<int, SfntColrPaint>();
        pendingOffsets = new System.Collections.Generic.List<int>();
    }

    internal void SetLayerPaintCount(int count)
    {
        layerPaintCount = count;
    }

    internal void AddPaintOffset(int offset)
    {
        if (!paints.ContainsKey(offset))
        {
            pendingOffsets.Add(offset);
        }
    }

    internal void ParseAll()
    {
        int pendingIndex = 0;

        while (pendingIndex < pendingOffsets.Count)
        {
            int offset = pendingOffsets[pendingIndex];

            if (!paints.ContainsKey(offset))
            {
                parsingBudget.AddEntries(1);
                SfntColrPaint paint = ParsePaint(offset);
                paints.Add(offset, paint);
                int childIndex = 0;

                while (childIndex < paint.GetChildPaintOffsetCount())
                {
                    AddPaintOffset(paint.GetChildPaintOffset(childIndex));
                    childIndex += 1;
                }
            }

            pendingIndex += 1;
        }
    }

    internal SfntColrPaint FindPaint(int offset)
    {
        if (!paints.ContainsKey(offset))
        {
            throw new System.InvalidOperationException(
                "A parsed COLR paint offset cannot be resolved.");
        }

        return paints[offset];
    }

    internal SfntColrPaint[] CopyPaintsSortedByOffset()
    {
        SfntColrPaint[] result = new SfntColrPaint[paints.Count];
        int resultIndex = 0;

        foreach (System.Collections.Generic.KeyValuePair<int, SfntColrPaint> pair
            in paints)
        {
            result[resultIndex] = pair.Value;
            resultIndex += 1;
        }

        int index = 1;

        while (index < result.Length)
        {
            SfntColrPaint value = result[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                result[insertionIndex - 1].GetOffset() > value.GetOffset())
            {
                result[insertionIndex] = result[insertionIndex - 1];
                insertionIndex -= 1;
            }

            result[insertionIndex] = value;
            index += 1;
        }

        return result;
    }

    SfntColrPaint ParsePaint(int offset)
    {
        BigEndianReader reader = SfntColrV1Parser.CreateReader(tableData, offset);
        int format = reader.ReadUInt8();
        bool supported = format >= 1 && format <= 32;
        bool variable = IsVariableFormat(format);
        int firstLayerIndex = -1;
        int layerCount = 0;
        int glyphId = -1;
        int paletteEntryIndex = -1;
        long variationIndexBase = -1;
        int compositeModeRaw = -1;
        SfntColrCompositeMode compositeMode = SfntColrCompositeMode.Clear;
        int[] childPaintOffsets = new int[0];
        SfntColrVariableValue[] values = new SfntColrVariableValue[0];
        SfntColrColorLine? colorLine = null;
        SfntColrAffineTransform? affineTransform = null;
        SfntColrChildValuesData? childValuesData = null;

        switch (format)
        {
            case 1:
                SfntColrV1Parser.RequireLength(
                    tableData, offset, 6, "A PaintColrLayers table is truncated.");
                layerCount = reader.ReadUInt8();
                long encodedFirstLayerIndex = reader.ReadUInt32();

                if (encodedFirstLayerIndex > int.MaxValue ||
                    encodedFirstLayerIndex + layerCount > layerPaintCount)
                {
                    throw FontFormatException.Create(
                        "A PaintColrLayers table references missing layers.");
                }

                firstLayerIndex = (int)encodedFirstLayerIndex;
                break;
            case 2:
            case 3:
                int solidLength = format == 2 ? 5 : 9;
                SfntColrV1Parser.RequireLength(
                    tableData, offset, solidLength, "A PaintSolid table is truncated.");
                paletteEntryIndex = reader.ReadUInt16();
                ValidatePaletteEntry(paletteEntryIndex);
                long[] solidRawValues = new long[] { reader.ReadInt16() };
                variationIndexBase = format == 3 ? reader.ReadUInt32() : -1;
                values = SfntColrV1Parser.CreateValues(
                    solidRawValues,
                    SfntColrV1Parser.CreateDivisors(1, 16384),
                    0,
                    variationIndexBase,
                    variable,
                    variationData);
                break;
            case 4:
            case 5:
                int linearLength = format == 4 ? 16 : 20;
                SfntColrV1Parser.RequireLength(
                    tableData, offset, linearLength, "A linear-gradient paint is truncated.");
                int linearColorLineOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), linearLength,
                    "A linear-gradient ColorLine offset is invalid.");
                long[] linearRawValues = SfntColrV1Parser.ReadInt16Values(reader, 6);
                variationIndexBase = format == 5 ? reader.ReadUInt32() : -1;
                values = SfntColrV1Parser.CreateValues(
                    linearRawValues,
                    SfntColrV1Parser.CreateDivisors(6, 1),
                    -1,
                    variationIndexBase,
                    variable,
                    variationData);
                colorLine = ParseColorLine(linearColorLineOffset, variable);
                break;
            case 6:
            case 7:
                int radialLength = format == 6 ? 16 : 20;
                SfntColrV1Parser.RequireLength(
                    tableData, offset, radialLength, "A radial-gradient paint is truncated.");
                int radialColorLineOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), radialLength,
                    "A radial-gradient ColorLine offset is invalid.");
                long[] radialRawValues = new long[6];
                radialRawValues[0] = reader.ReadInt16();
                radialRawValues[1] = reader.ReadInt16();
                radialRawValues[2] = reader.ReadUInt16();
                radialRawValues[3] = reader.ReadInt16();
                radialRawValues[4] = reader.ReadInt16();
                radialRawValues[5] = reader.ReadUInt16();
                variationIndexBase = format == 7 ? reader.ReadUInt32() : -1;
                values = SfntColrV1Parser.CreateValues(
                    radialRawValues,
                    SfntColrV1Parser.CreateDivisors(6, 1),
                    -1,
                    variationIndexBase,
                    variable,
                    variationData);
                colorLine = ParseColorLine(radialColorLineOffset, variable);
                break;
            case 8:
            case 9:
                int sweepLength = format == 8 ? 12 : 16;
                SfntColrV1Parser.RequireLength(
                    tableData, offset, sweepLength, "A sweep-gradient paint is truncated.");
                int sweepColorLineOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), sweepLength,
                    "A sweep-gradient ColorLine offset is invalid.");
                long[] sweepRawValues = SfntColrV1Parser.ReadInt16Values(reader, 4);
                variationIndexBase = format == 9 ? reader.ReadUInt32() : -1;
                double[] sweepDivisors = new double[] { 1, 1, 16384, 16384 };
                values = SfntColrV1Parser.CreateValues(
                    sweepRawValues,
                    sweepDivisors,
                    -1,
                    variationIndexBase,
                    variable,
                    variationData);
                colorLine = ParseColorLine(sweepColorLineOffset, variable);
                break;
            case 10:
                SfntColrV1Parser.RequireLength(
                    tableData, offset, 6, "A PaintGlyph table is truncated.");
                childPaintOffsets = new int[] { ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), 6,
                    "A PaintGlyph child offset is invalid.") };
                glyphId = reader.ReadUInt16();
                ValidateGlyphId(glyphId, "A PaintGlyph references a missing glyph.");
                break;
            case 11:
                SfntColrV1Parser.RequireLength(
                    tableData, offset, 3, "A PaintColrGlyph table is truncated.");
                glyphId = reader.ReadUInt16();
                ValidateGlyphId(glyphId, "A PaintColrGlyph references a missing glyph.");
                break;
            case 12:
            case 13:
                SfntColrV1Parser.RequireLength(
                    tableData, offset, 7, "A PaintTransform table is truncated.");
                childPaintOffsets = new int[] { ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), 7,
                    "A PaintTransform child offset is invalid.") };
                int transformOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), 7,
                    "A PaintTransform matrix offset is invalid.");
                affineTransform = ParseAffineTransform(
                    transformOffset,
                    format == 13);
                variable = format == 13;
                variationIndexBase = affineTransform.IsVariable() ?
                    affineTransform.GetVariationIndexBase() : -1;
                break;
            case 14:
            case 15:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 14 ? 8 : 12,
                    2, new double[] { 1, 1 }, variable,
                    "A PaintTranslate table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 16:
            case 17:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 16 ? 8 : 12,
                    2, SfntColrV1Parser.CreateDivisors(2, 16384), variable,
                    "A PaintScale table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 18:
            case 19:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 18 ? 12 : 16,
                    4, new double[] { 16384, 16384, 1, 1 }, variable,
                    "A centered PaintScale table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 20:
            case 21:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 20 ? 6 : 10,
                    1, SfntColrV1Parser.CreateDivisors(1, 16384), variable,
                    "A PaintScaleUniform table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 22:
            case 23:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 22 ? 10 : 14,
                    3, new double[] { 16384, 1, 1 }, variable,
                    "A centered PaintScaleUniform table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 24:
            case 25:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 24 ? 6 : 10,
                    1, SfntColrV1Parser.CreateDivisors(1, 16384), variable,
                    "A PaintRotate table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 26:
            case 27:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 26 ? 10 : 14,
                    3, new double[] { 16384, 1, 1 }, variable,
                    "A centered PaintRotate table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 28:
            case 29:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 28 ? 8 : 12,
                    2, SfntColrV1Parser.CreateDivisors(2, 16384), variable,
                    "A PaintSkew table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 30:
            case 31:
                childValuesData = ParseChildAndValues(
                    reader, offset, format == 30 ? 12 : 16,
                    4, new double[] { 16384, 16384, 1, 1 }, variable,
                    "A centered PaintSkew table is truncated.");
                childPaintOffsets = childValuesData.GetChildPaintOffsets();
                variationIndexBase = childValuesData.GetVariationIndexBase();
                values = childValuesData.GetValues();
                break;
            case 32:
                SfntColrV1Parser.RequireLength(
                    tableData, offset, 8, "A PaintComposite table is truncated.");
                int sourcePaintOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), 8,
                    "A PaintComposite source offset is invalid.");
                compositeModeRaw = reader.ReadUInt8();
                compositeMode = compositeModeRaw <= 27 ?
                    (SfntColrCompositeMode)compositeModeRaw :
                    SfntColrCompositeMode.Clear;
                int backdropPaintOffset = ResolvePaintRelativeOffset(
                    offset, reader.ReadOffset24(), 8,
                    "A PaintComposite backdrop offset is invalid.");
                childPaintOffsets = new int[] {
                    sourcePaintOffset,
                    backdropPaintOffset
                };
                break;
            default:
                break;
        }

        return new SfntColrPaint(
            offset,
            format,
            supported,
            variable,
            firstLayerIndex,
            layerCount,
            glyphId,
            paletteEntryIndex,
            variationIndexBase,
            compositeModeRaw,
            compositeMode,
            childPaintOffsets,
            values,
            colorLine,
            affineTransform);
    }

    SfntColrChildValuesData ParseChildAndValues(
        BigEndianReader reader,
        int offset,
        int length,
        int valueCount,
        double[] divisors,
        bool variable,
        string message)
    {
        SfntColrV1Parser.RequireLength(tableData, offset, length, message);
        int[] childPaintOffsets = new int[] { ResolvePaintRelativeOffset(
            offset,
            reader.ReadOffset24(),
            length,
            "A COLR transform child-paint offset is invalid.") };
        long[] rawValues = SfntColrV1Parser.ReadInt16Values(reader, valueCount);
        long variationIndexBase = variable ? reader.ReadUInt32() : -1;
        SfntColrVariableValue[] values = SfntColrV1Parser.CreateValues(
            rawValues,
            divisors,
            -1,
            variationIndexBase,
            variable,
            variationData);
        return new SfntColrChildValuesData(
            childPaintOffsets,
            variationIndexBase,
            values);
    }

    SfntColrColorLine ParseColorLine(int offset, bool variable)
    {
        BigEndianReader reader = SfntColrV1Parser.CreateReader(tableData, offset);
        int extendRaw = reader.ReadUInt8();
        int stopCount = reader.ReadUInt16();

        if (stopCount > maximumPaintCount)
        {
            throw FontLimitException.Create(
                "A COLR color-stop count exceeds the configured coverage-entry limit.");
        }

        parsingBudget.AddEntries(stopCount);

        int stopLength = variable ? 10 : 6;
        int length = CheckedFontMath.AddOffset(
            3,
            CheckedFontMath.MultiplyCount(stopCount, stopLength));
        SfntColrV1Parser.RequireLength(
            tableData, offset, length, "A COLR ColorLine is truncated.");
        SfntColrColorStop[] stops = new SfntColrColorStop[stopCount];
        int index = 0;

        while (index < stops.Length)
        {
            long stopOffsetRaw = reader.ReadInt16();
            int paletteEntryIndex = reader.ReadUInt16();
            long alphaRaw = reader.ReadInt16();
            long variationIndexBase = variable ? reader.ReadUInt32() : -1;
            ValidatePaletteEntry(paletteEntryIndex);
            SfntColrVariableValue[] values = SfntColrV1Parser.CreateValues(
                new long[] { stopOffsetRaw, alphaRaw },
                SfntColrV1Parser.CreateDivisors(2, 16384),
                1,
                variationIndexBase,
                variable,
                variationData);
            stops[index] = new SfntColrColorStop(
                paletteEntryIndex,
                values[0],
                values[1],
                variationIndexBase);
            index += 1;
        }

        SfntColrExtendMode extendMode = extendRaw <= 2 ?
            (SfntColrExtendMode)extendRaw : SfntColrExtendMode.Pad;
        return new SfntColrColorLine(
            extendRaw,
            extendMode,
            variable,
            stops);
    }

    SfntColrAffineTransform ParseAffineTransform(int offset, bool variable)
    {
        int length = variable ? 28 : 24;
        SfntColrV1Parser.RequireLength(
            tableData, offset, length, "A COLR affine transform is truncated.");
        BigEndianReader reader = SfntColrV1Parser.CreateReader(tableData, offset);
        long[] rawValues = new long[6];
        int index = 0;

        while (index < rawValues.Length)
        {
            rawValues[index] = reader.ReadInt32();
            index += 1;
        }

        long variationIndexBase = variable ? reader.ReadUInt32() : -1;
        SfntColrVariableValue[] values = SfntColrV1Parser.CreateValues(
            rawValues,
            SfntColrV1Parser.CreateDivisors(6, 65536),
            -1,
            variationIndexBase,
            variable,
            variationData);
        return new SfntColrAffineTransform(
            variable,
            variationIndexBase,
            values);
    }

    int ResolvePaintRelativeOffset(
        int paintOffset,
        long relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        return SfntColrV1Parser.ResolveRelativeOffset(
            tableData,
            paintOffset,
            relativeOffset,
            minimumRelativeOffset,
            1,
            message);
    }

    void ValidateGlyphId(int glyphId, string message)
    {
        if (glyphId >= glyphCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    void ValidatePaletteEntry(int paletteEntryIndex)
    {
        if (paletteEntryIndex != 0xFFFF &&
            paletteEntryIndex >= paletteEntryCount)
        {
            throw FontFormatException.Create(
                "A COLR paint references a missing CPAL entry.");
        }
    }

    static bool IsVariableFormat(int format)
    {
        switch (format)
        {
            case 3:
            case 5:
            case 7:
            case 9:
            case 13:
            case 15:
            case 17:
            case 19:
            case 21:
            case 23:
            case 25:
            case 27:
            case 29:
            case 31:
                return true;
            default:
                return false;
        }
    }
}
