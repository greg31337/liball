namespace ttf;

internal class SfntGvarTuple
{
    double[] coordinates;

    internal SfntGvarTuple(double[] coordinates)
    {
        this.coordinates = coordinates;
    }

    internal int GetAxisCount()
    {
        return coordinates.Length;
    }

    internal double GetCoordinate(int axisIndex)
    {
        if (axisIndex < 0 || axisIndex >= coordinates.Length)
        {
            throw new System.ArgumentOutOfRangeException("axisIndex");
        }

        return coordinates[axisIndex];
    }
}

internal class SfntGvarPointNumbers
{
    bool allPoints;
    int[] pointNumbers;

    internal SfntGvarPointNumbers(bool allPoints, int[] pointNumbers)
    {
        this.allPoints = allPoints;
        this.pointNumbers = pointNumbers;
    }

    internal bool UsesAllPoints()
    {
        return allPoints;
    }

    internal int GetCount()
    {
        return pointNumbers.Length;
    }

    internal int GetPointNumber(int index)
    {
        if (index < 0 || index >= pointNumbers.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return pointNumbers[index];
    }
}

internal class SfntGvarTupleVariation
{
    int variationDataSize;
    int tupleIndex;
    bool embeddedPeak;
    bool intermediateRegion;
    bool privatePointNumbers;
    SfntGvarTuple peakTuple;
    SfntGvarTuple? intermediateStartTuple;
    SfntGvarTuple? intermediateEndTuple;
    SfntGvarPointNumbers pointNumbers;
    int[] xDeltas;
    int[] yDeltas;

    internal SfntGvarTupleVariation(
        int variationDataSize,
        int tupleIndex,
        bool embeddedPeak,
        bool intermediateRegion,
        bool privatePointNumbers,
        SfntGvarTuple peakTuple,
        SfntGvarTuple? intermediateStartTuple,
        SfntGvarTuple? intermediateEndTuple,
        SfntGvarPointNumbers pointNumbers,
        int[] xDeltas,
        int[] yDeltas)
    {
        this.variationDataSize = variationDataSize;
        this.tupleIndex = tupleIndex;
        this.embeddedPeak = embeddedPeak;
        this.intermediateRegion = intermediateRegion;
        this.privatePointNumbers = privatePointNumbers;
        this.peakTuple = peakTuple;
        this.intermediateStartTuple = intermediateStartTuple;
        this.intermediateEndTuple = intermediateEndTuple;
        this.pointNumbers = pointNumbers;
        this.xDeltas = xDeltas;
        this.yDeltas = yDeltas;
    }

    internal int GetVariationDataSize()
    {
        return variationDataSize;
    }

    internal int GetTupleIndex()
    {
        return tupleIndex;
    }

    internal bool HasEmbeddedPeakTuple()
    {
        return embeddedPeak;
    }

    internal bool HasIntermediateRegion()
    {
        return intermediateRegion;
    }

    internal bool HasPrivatePointNumbers()
    {
        return privatePointNumbers;
    }

    internal SfntGvarTuple GetPeakTuple()
    {
        return peakTuple;
    }

    internal SfntGvarTuple? GetIntermediateStartTuple()
    {
        return intermediateStartTuple;
    }

    internal SfntGvarTuple? GetIntermediateEndTuple()
    {
        return intermediateEndTuple;
    }

    internal SfntGvarPointNumbers GetPointNumbers()
    {
        return pointNumbers;
    }

    internal int GetDeltaCount()
    {
        return xDeltas.Length;
    }

    internal int GetXDelta(int index)
    {
        if (index < 0 || index >= xDeltas.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return xDeltas[index];
    }

    internal int GetYDelta(int index)
    {
        if (index < 0 || index >= yDeltas.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return yDeltas[index];
    }

    internal double CalculateScalar(double[] normalizedCoordinates)
    {
        return SfntGvarTupleScalar.Calculate(this, normalizedCoordinates);
    }

    internal SfntGvarDeltaSet CreateAllPointDeltas(SfntSimpleGlyph glyph)
    {
        return SfntGvarIupInterpolator.CreateAllPointDeltas(this, glyph);
    }
}

internal class SfntGlyphVariationData
{
    int glyphId;
    int targetPointCount;
    bool hasSharedPointNumbers;
    SfntGvarPointNumbers? sharedPointNumbers;
    SfntGvarTupleVariation[] tupleVariations;

    internal SfntGlyphVariationData(
        int glyphId,
        int targetPointCount,
        bool hasSharedPointNumbers,
        SfntGvarPointNumbers? sharedPointNumbers,
        SfntGvarTupleVariation[] tupleVariations)
    {
        this.glyphId = glyphId;
        this.targetPointCount = targetPointCount;
        this.hasSharedPointNumbers = hasSharedPointNumbers;
        this.sharedPointNumbers = sharedPointNumbers;
        this.tupleVariations = tupleVariations;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetTargetPointCount()
    {
        return targetPointCount;
    }

    internal bool HasSharedPointNumbers()
    {
        return hasSharedPointNumbers;
    }

    internal SfntGvarPointNumbers? GetSharedPointNumbers()
    {
        return sharedPointNumbers;
    }

    internal int GetTupleVariationCount()
    {
        return tupleVariations.Length;
    }

    internal SfntGvarTupleVariation GetTupleVariation(int index)
    {
        if (index < 0 || index >= tupleVariations.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tupleVariations[index];
    }
}

internal class SfntGvarTupleHeader
{
    int variationDataSize;
    int tupleIndex;
    bool embeddedPeak;
    bool intermediateRegion;
    bool privatePointNumbers;
    SfntGvarTuple peakTuple;
    SfntGvarTuple? startTuple;
    SfntGvarTuple? endTuple;

    internal SfntGvarTupleHeader(
        int variationDataSize,
        int tupleIndex,
        bool embeddedPeak,
        bool intermediateRegion,
        bool privatePointNumbers,
        SfntGvarTuple peakTuple,
        SfntGvarTuple? startTuple,
        SfntGvarTuple? endTuple)
    {
        this.variationDataSize = variationDataSize;
        this.tupleIndex = tupleIndex;
        this.embeddedPeak = embeddedPeak;
        this.intermediateRegion = intermediateRegion;
        this.privatePointNumbers = privatePointNumbers;
        this.peakTuple = peakTuple;
        this.startTuple = startTuple;
        this.endTuple = endTuple;
    }

    internal int GetVariationDataSize()
    {
        return variationDataSize;
    }

    internal int GetTupleIndex()
    {
        return tupleIndex;
    }

    internal bool HasEmbeddedPeakTuple()
    {
        return embeddedPeak;
    }

    internal bool HasIntermediateRegion()
    {
        return intermediateRegion;
    }

    internal bool HasPrivatePointNumbers()
    {
        return privatePointNumbers;
    }

    internal SfntGvarTuple GetPeakTuple()
    {
        return peakTuple;
    }

    internal SfntGvarTuple? GetStartTuple()
    {
        return startTuple;
    }

    internal SfntGvarTuple? GetEndTuple()
    {
        return endTuple;
    }
}

internal class SfntGvarTable
{
    int majorVersion;
    int minorVersion;
    int axisCount;
    int flags;
    int glyphVariationDataArrayOffset;
    SfntGvarTuple[] sharedTuples;
    int[] glyphOffsets;
    FontDataSlice tableData;
    FontLimits limits;

    internal SfntGvarTable(
        int majorVersion,
        int minorVersion,
        int axisCount,
        int flags,
        int glyphVariationDataArrayOffset,
        SfntGvarTuple[] sharedTuples,
        int[] glyphOffsets,
        FontDataSlice tableData,
        FontLimits limits)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.axisCount = axisCount;
        this.flags = flags;
        this.glyphVariationDataArrayOffset = glyphVariationDataArrayOffset;
        this.sharedTuples = sharedTuples;
        this.glyphOffsets = glyphOffsets;
        this.tableData = tableData;
        this.limits = limits;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetAxisCount()
    {
        return axisCount;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool UsesLongOffsets()
    {
        return (flags & 1) != 0;
    }

    internal int GetGlyphVariationDataArrayOffset()
    {
        return glyphVariationDataArrayOffset;
    }

    internal int GetSharedTupleCount()
    {
        return sharedTuples.Length;
    }

    internal SfntGvarTuple GetSharedTuple(int index)
    {
        if (index < 0 || index >= sharedTuples.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sharedTuples[index];
    }

    internal int GetGlyphCount()
    {
        return glyphOffsets.Length - 1;
    }

    internal int GetGlyphDataOffset(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphOffsets[glyphId];
    }

    internal int GetGlyphDataLength(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphOffsets[glyphId + 1] - glyphOffsets[glyphId];
    }

    internal SfntGlyphVariationData? GetGlyphVariationData(
        int glyphId,
        int targetPointCount)
    {
        ValidateGlyphId(glyphId);

        if (targetPointCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("targetPointCount");
        }

        int length = GetGlyphDataLength(glyphId);

        if (length == 0)
        {
            return null;
        }

        int offset = CheckedFontMath.AddOffset(
            glyphVariationDataArrayOffset,
            glyphOffsets[glyphId]);
        return SfntGvarGlyphDataParser.Parse(
            tableData.GetSlice(offset, length),
            glyphId,
            targetPointCount,
            axisCount,
            sharedTuples,
            limits);
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphOffsets.Length - 1)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal static class SfntGvarPackedDataDecoder
{
    const int PointCountWords = 0x80;
    const int PointRunWords = 0x80;
    const int DeltaRunZeros = 0x80;
    const int DeltaRunWords = 0x40;

    internal static SfntGvarPointNumbers ReadPointNumbers(
        BigEndianReader reader,
        int maximumPointNumber,
        SfntVariationParsingBudget budget)
    {
        int countByte = reader.ReadUInt8();

        if (countByte == 0)
        {
            return new SfntGvarPointNumbers(true, new int[0]);
        }

        int count = countByte;

        if ((countByte & PointCountWords) != 0)
        {
            count = ((countByte & 0x7F) << 8) | reader.ReadUInt8();
        }

        if (count == 0)
        {
            throw FontFormatException.Create("A packed point-number count is invalid.");
        }

        budget.AddEntries(count);
        int[] points = new int[count];
        int pointIndex = 0;
        int previousPoint = 0;

        while (pointIndex < points.Length)
        {
            int control = reader.ReadUInt8();
            int runCount = (control & 0x7F) + 1;

            if (runCount > points.Length - pointIndex)
            {
                throw FontFormatException.Create(
                    "A packed point-number run exceeds its declared count.");
            }

            int runIndex = 0;

            while (runIndex < runCount)
            {
                int pointDelta = (control & PointRunWords) != 0 ?
                    reader.ReadUInt16() : reader.ReadUInt8();

                if (pointDelta > 0xFFFF - previousPoint)
                {
                    throw FontFormatException.Create(
                        "A packed point number exceeds the UInt16 range.");
                }

                previousPoint += pointDelta;

                if (previousPoint >= maximumPointNumber)
                {
                    throw FontFormatException.Create(
                        "A packed point number is outside the target glyph.");
                }

                points[pointIndex] = previousPoint;
                pointIndex += 1;
                runIndex += 1;
            }
        }

        return new SfntGvarPointNumbers(false, points);
    }

    internal static int[] ReadDeltas(
        BigEndianReader reader,
        int count,
        SfntVariationParsingBudget budget)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        budget.AddEntries(count);
        int[] deltas = new int[count];
        int deltaIndex = 0;

        while (deltaIndex < deltas.Length)
        {
            int control = reader.ReadUInt8();
            int runCount = (control & 0x3F) + 1;

            if (runCount > deltas.Length - deltaIndex)
            {
                throw FontFormatException.Create(
                    "A packed delta run exceeds its expected count.");
            }

            int runIndex = 0;

            while (runIndex < runCount)
            {
                if ((control & DeltaRunZeros) != 0)
                {
                    deltas[deltaIndex] = 0;
                }
                else if ((control & DeltaRunWords) != 0)
                {
                    deltas[deltaIndex] = reader.ReadInt16();
                }
                else
                {
                    deltas[deltaIndex] = reader.ReadInt8();
                }

                deltaIndex += 1;
                runIndex += 1;
            }
        }

        return deltas;
    }
}

internal static class SfntGvarGlyphDataParser
{
    const int SharedPointNumbers = 0x8000;
    const int TupleCountMask = 0x0FFF;
    const int EmbeddedPeakTuple = 0x8000;
    const int IntermediateRegion = 0x4000;
    const int PrivatePointNumbers = 0x2000;
    const int TupleIndexMask = 0x0FFF;

    internal static SfntGlyphVariationData Parse(
        FontDataSlice data,
        int glyphId,
        int targetPointCount,
        int axisCount,
        SfntGvarTuple[] sharedTuples,
        FontLimits limits)
    {
        BigEndianReader reader = new BigEndianReader(data);
        int encodedTupleCount = reader.ReadUInt16();
        int dataOffset = reader.ReadUInt16();

        if ((encodedTupleCount & 0x7000) != 0)
        {
            throw FontFormatException.Create(
                "A glyph variation tuple count has reserved bits set.");
        }

        int tupleCount = encodedTupleCount & TupleCountMask;

        if (tupleCount == 0)
        {
            throw FontFormatException.Create(
                "A nonempty glyph variation record has no tuple variations.");
        }

        if (tupleCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "A glyph variation record exceeds the configured variation-region limit.");
        }

        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        budget.AddEntries(tupleCount);
        SfntGvarTupleHeader[] headers = new SfntGvarTupleHeader[tupleCount];
        int headerIndex = 0;

        while (headerIndex < headers.Length)
        {
            headers[headerIndex] = ReadHeader(
                reader,
                axisCount,
                sharedTuples,
                budget);
            headerIndex += 1;
        }

        if (dataOffset < reader.GetPosition() || dataOffset > data.GetLength())
        {
            throw FontFormatException.Create(
                "A glyph variation serialized-data offset is invalid.");
        }

        BigEndianReader serializedReader = new BigEndianReader(
            data.GetSlice(dataOffset, data.GetLength() - dataOffset));
        bool hasSharedPoints = (encodedTupleCount & SharedPointNumbers) != 0;
        SfntGvarPointNumbers? sharedPoints = null;

        if (hasSharedPoints)
        {
            sharedPoints = SfntGvarPackedDataDecoder.ReadPointNumbers(
                serializedReader,
                targetPointCount,
                budget);
        }

        int tupleDataOffset = CheckedFontMath.AddOffset(
            dataOffset,
            serializedReader.GetPosition());
        SfntGvarTupleVariation[] variations =
            new SfntGvarTupleVariation[headers.Length];
        int variationIndex = 0;

        while (variationIndex < variations.Length)
        {
            SfntGvarTupleHeader header = headers[variationIndex];
            int dataSize = header.GetVariationDataSize();

            if (dataSize > data.GetLength() - tupleDataOffset)
            {
                throw FontFormatException.Create(
                    "A glyph tuple's serialized variation data is truncated.");
            }

            BigEndianReader tupleReader = new BigEndianReader(
                data.GetSlice(tupleDataOffset, dataSize));
            SfntGvarPointNumbers? points = header.HasPrivatePointNumbers() ?
                SfntGvarPackedDataDecoder.ReadPointNumbers(
                    tupleReader,
                    targetPointCount,
                    budget) : sharedPoints;

            if (points == null)
            {
                points = new SfntGvarPointNumbers(true, new int[0]);
            }

            int deltaCount = points.UsesAllPoints() ?
                targetPointCount : points.GetCount();
            int[] xDeltas = SfntGvarPackedDataDecoder.ReadDeltas(
                tupleReader,
                deltaCount,
                budget);
            int[] yDeltas = SfntGvarPackedDataDecoder.ReadDeltas(
                tupleReader,
                deltaCount,
                budget);

            if (tupleReader.GetRemainingLength() != 0)
            {
                throw FontFormatException.Create(
                    "A glyph tuple has unused serialized variation data.");
            }

            variations[variationIndex] = new SfntGvarTupleVariation(
                dataSize,
                header.GetTupleIndex(),
                header.HasEmbeddedPeakTuple(),
                header.HasIntermediateRegion(),
                header.HasPrivatePointNumbers(),
                header.GetPeakTuple(),
                header.GetStartTuple(),
                header.GetEndTuple(),
                points,
                xDeltas,
                yDeltas);
            tupleDataOffset = CheckedFontMath.AddOffset(
                tupleDataOffset,
                dataSize);
            variationIndex += 1;
        }

        return new SfntGlyphVariationData(
            glyphId,
            targetPointCount,
            hasSharedPoints,
            sharedPoints,
            variations);
    }

    static SfntGvarTupleHeader ReadHeader(
        BigEndianReader reader,
        int axisCount,
        SfntGvarTuple[] sharedTuples,
        SfntVariationParsingBudget budget)
    {
        int variationDataSize = reader.ReadUInt16();
        int encodedTupleIndex = reader.ReadUInt16();

        if ((encodedTupleIndex & 0x1000) != 0)
        {
            throw FontFormatException.Create(
                "A tuple variation header has a reserved flag set.");
        }

        bool embeddedPeak = (encodedTupleIndex & EmbeddedPeakTuple) != 0;
        bool intermediate = (encodedTupleIndex & IntermediateRegion) != 0;
        bool privatePoints = (encodedTupleIndex & PrivatePointNumbers) != 0;
        int tupleIndex = encodedTupleIndex & TupleIndexMask;
        SfntGvarTuple peak;

        if (embeddedPeak)
        {
            peak = ReadTuple(reader, axisCount, budget);
        }
        else
        {
            if (tupleIndex >= sharedTuples.Length)
            {
                throw FontFormatException.Create(
                    "A tuple variation references a missing shared tuple.");
            }

            peak = sharedTuples[tupleIndex];
        }

        SfntGvarTuple? start = null;
        SfntGvarTuple? end = null;

        if (intermediate)
        {
            start = ReadTuple(reader, axisCount, budget);
            end = ReadTuple(reader, axisCount, budget);
        }

        return new SfntGvarTupleHeader(
            variationDataSize,
            tupleIndex,
            embeddedPeak,
            intermediate,
            privatePoints,
            peak,
            start,
            end);
    }

    internal static SfntGvarTuple ReadTuple(
        BigEndianReader reader,
        int axisCount,
        SfntVariationParsingBudget budget)
    {
        budget.AddEntries(axisCount);
        double[] coordinates = new double[axisCount];
        int axisIndex = 0;

        while (axisIndex < coordinates.Length)
        {
            double coordinate = reader.ReadF2Dot14();

            if (coordinate < -1 || coordinate > 1)
            {
                throw FontFormatException.Create(
                    "A gvar tuple coordinate is outside the normalized range.");
            }

            coordinates[axisIndex] = coordinate;
            axisIndex += 1;
        }

        return new SfntGvarTuple(coordinates);
    }
}

internal static class SfntGvarTableParser
{
    const int HeaderLength = 20;
    const int LongOffsets = 1;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Gvar,
        1,
        1,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntGvarTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntGvarTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion tableVersion,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The gvar table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int axisCount = reader.ReadUInt16();
        int sharedTupleCount = reader.ReadUInt16();
        int sharedTuplesOffset = ReadOffset(reader, "The gvar shared-tuples offset is invalid.");
        int glyphCount = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        int glyphDataOffset = ReadOffset(
            reader,
            "The gvar glyph-data array offset is invalid.");

        if (majorVersion != tableVersion.GetMajorVersion() ||
            minorVersion != tableVersion.GetMinorVersion())
        {
            throw FontFormatException.Create("The gvar table version is inconsistent.");
        }

        if ((flags & ~LongOffsets) != 0)
        {
            throw FontFormatException.Create("The gvar table has reserved flags set.");
        }

        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create("The gvar table requires a supported fvar table.");
        }

        if (axisCount != fvar.GetAxisCount())
        {
            throw FontFormatException.Create("The gvar axis count does not match fvar.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create("The gvar table requires a supported maxp table.");
        }

        if (glyphCount != maxp.GetGlyphCount())
        {
            throw FontFormatException.Create("The gvar glyph count does not match maxp.");
        }

        if (sharedTupleCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "The gvar shared-tuple count exceeds the configured variation-region limit.");
        }

        int offsetSize = (flags & LongOffsets) != 0 ? 4 : 2;
        int offsetCount = CheckedFontMath.AddOffset(glyphCount, 1);
        int offsetsLength = CheckedFontMath.MultiplyCount(offsetCount, offsetSize);
        int offsetsEnd = CheckedFontMath.AddOffset(HeaderLength, offsetsLength);

        if (offsetsEnd > tableData.GetLength())
        {
            throw FontFormatException.Create("The gvar glyph offset array is truncated.");
        }

        int tupleCoordinateCount = CheckedFontMath.MultiplyCount(
            sharedTupleCount,
            axisCount);
        int sharedTuplesLength = CheckedFontMath.MultiplyCount(
            tupleCoordinateCount,
            2);

        if (sharedTupleCount > 0 &&
            (sharedTuplesOffset < offsetsEnd ||
            sharedTuplesOffset > tableData.GetLength() - sharedTuplesLength))
        {
            throw FontFormatException.Create("The gvar shared-tuples array is invalid.");
        }

        int sharedTuplesEnd = sharedTupleCount == 0 ?
            offsetsEnd : CheckedFontMath.AddOffset(
                sharedTuplesOffset,
                sharedTuplesLength);

        if (glyphDataOffset < sharedTuplesEnd || glyphDataOffset > tableData.GetLength())
        {
            throw FontFormatException.Create("The gvar glyph-data array offset is invalid.");
        }

        int[] glyphOffsets = ReadGlyphOffsets(
            reader,
            offsetCount,
            (flags & LongOffsets) != 0,
            tableData.GetLength() - glyphDataOffset);
        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        SfntGvarTuple[] sharedTuples = ReadSharedTuples(
            tableData,
            sharedTuplesOffset,
            sharedTupleCount,
            axisCount,
            budget);

        return new SfntGvarTable(
            majorVersion,
            minorVersion,
            axisCount,
            flags,
            glyphDataOffset,
            sharedTuples,
            glyphOffsets,
            tableData,
            limits);
    }

    static int[] ReadGlyphOffsets(
        BigEndianReader reader,
        int count,
        bool longOffsets,
        int availableDataLength)
    {
        int[] offsets = new int[count];
        int index = 0;
        int previous = 0;

        while (index < offsets.Length)
        {
            long encodedOffset = longOffsets ?
                reader.ReadOffset32() : reader.ReadOffset16() * 2L;

            if (encodedOffset > int.MaxValue ||
                encodedOffset < previous ||
                encodedOffset > availableDataLength)
            {
                throw FontFormatException.Create("A gvar glyph data offset is invalid.");
            }

            if (index == 0 && encodedOffset != 0)
            {
                throw FontFormatException.Create("The first gvar glyph data offset is not zero.");
            }

            offsets[index] = (int)encodedOffset;
            previous = offsets[index];
            index += 1;
        }

        return offsets;
    }

    static SfntGvarTuple[] ReadSharedTuples(
        FontDataSlice tableData,
        int offset,
        int count,
        int axisCount,
        SfntVariationParsingBudget budget)
    {
        SfntGvarTuple[] tuples = new SfntGvarTuple[count];

        if (count == 0)
        {
            return tuples;
        }

        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice(offset, tableData.GetLength() - offset));
        int tupleIndex = 0;

        while (tupleIndex < tuples.Length)
        {
            tuples[tupleIndex] = SfntGvarGlyphDataParser.ReadTuple(
                reader,
                axisCount,
                budget);
            tupleIndex += 1;
        }

        return tuples;
    }

    static int ReadOffset(BigEndianReader reader, string message)
    {
        long encodedOffset = reader.ReadOffset32();

        if (encodedOffset > int.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return (int)encodedOffset;
    }
}
