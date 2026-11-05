namespace ttf;

internal class SfntCvarTupleVariation
{
    int variationDataSize;
    int tupleIndex;
    bool intermediateRegion;
    bool privatePointNumbers;
    SfntGvarTuple peakTuple;
    SfntGvarTuple? intermediateStartTuple;
    SfntGvarTuple? intermediateEndTuple;
    SfntGvarPointNumbers controlValueNumbers;
    int[] deltas;

    internal SfntCvarTupleVariation(
        int variationDataSize,
        int tupleIndex,
        bool intermediateRegion,
        bool privatePointNumbers,
        SfntGvarTuple peakTuple,
        SfntGvarTuple? intermediateStartTuple,
        SfntGvarTuple? intermediateEndTuple,
        SfntGvarPointNumbers controlValueNumbers,
        int[] deltas)
    {
        this.variationDataSize = variationDataSize;
        this.tupleIndex = tupleIndex;
        this.intermediateRegion = intermediateRegion;
        this.privatePointNumbers = privatePointNumbers;
        this.peakTuple = peakTuple;
        this.intermediateStartTuple = intermediateStartTuple;
        this.intermediateEndTuple = intermediateEndTuple;
        this.controlValueNumbers = controlValueNumbers;
        this.deltas = deltas;
    }

    internal int GetVariationDataSize()
    {
        return variationDataSize;
    }

    internal int GetTupleIndex()
    {
        return tupleIndex;
    }

    internal bool HasIntermediateRegion()
    {
        return intermediateRegion;
    }

    internal bool HasPrivateControlValueNumbers()
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

    internal SfntGvarPointNumbers GetControlValueNumbers()
    {
        return controlValueNumbers;
    }

    internal int GetDeltaCount()
    {
        return deltas.Length;
    }

    internal int GetDelta(int index)
    {
        if (index < 0 || index >= deltas.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return deltas[index];
    }

    internal double CalculateScalar(double[] normalizedCoordinates)
    {
        return SfntGvarTupleScalar.Calculate(
            peakTuple,
            intermediateStartTuple,
            intermediateEndTuple,
            intermediateRegion,
            normalizedCoordinates);
    }
}

internal class SfntCvarTable
{
    int majorVersion;
    int minorVersion;
    int controlValueCount;
    bool hasSharedControlValueNumbers;
    SfntGvarPointNumbers? sharedControlValueNumbers;
    SfntCvarTupleVariation[] variations;

    internal SfntCvarTable(
        int majorVersion,
        int minorVersion,
        int controlValueCount,
        bool hasSharedControlValueNumbers,
        SfntGvarPointNumbers? sharedControlValueNumbers,
        SfntCvarTupleVariation[] variations)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.controlValueCount = controlValueCount;
        this.hasSharedControlValueNumbers = hasSharedControlValueNumbers;
        this.sharedControlValueNumbers = sharedControlValueNumbers;
        this.variations = variations;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetControlValueCount()
    {
        return controlValueCount;
    }

    internal bool HasSharedControlValueNumbers()
    {
        return hasSharedControlValueNumbers;
    }

    internal SfntGvarPointNumbers? GetSharedControlValueNumbers()
    {
        return sharedControlValueNumbers;
    }

    internal int GetTupleVariationCount()
    {
        return variations.Length;
    }

    internal SfntCvarTupleVariation GetTupleVariation(int index)
    {
        if (index < 0 || index >= variations.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return variations[index];
    }

    internal byte[] Apply(byte[] controlValueData, double[] normalizedCoordinates)
    {
        if (controlValueData == null)
        {
            throw new System.ArgumentNullException("controlValueData");
        }

        if (controlValueData.Length / 2 != controlValueCount)
        {
            throw new System.ArgumentException(
                "The control-value data does not match the cvar table.",
                "controlValueData");
        }

        ValidateCoordinates(normalizedCoordinates);
        double[] accumulatedDeltas = new double[controlValueCount];
        int variationIndex = 0;

        while (variationIndex < variations.Length)
        {
            ApplyVariation(
                variations[variationIndex],
                normalizedCoordinates,
                accumulatedDeltas);
            variationIndex += 1;
        }

        byte[] result = controlValueData.AsSpan().ToArray();
        int controlValueIndex = 0;

        while (controlValueIndex < controlValueCount)
        {
            int offset = controlValueIndex * 2;
            int originalBits = (result[offset] << 8) | result[offset + 1];
            int original = originalBits < 32768
                ? originalBits
                : originalBits - 65536;
            double varied = original + accumulatedDeltas[controlValueIndex];
            int rounded = (int)System.Math.Round(
                varied,
                System.MidpointRounding.AwayFromZero);

            if (rounded < short.MinValue || rounded > short.MaxValue)
            {
                throw FontFormatException.Create(
                    "A varied cvar control value exceeds the FWORD range.");
            }

            int encoded = rounded < 0 ? rounded + 65536 : rounded;
            result[offset] = (byte)((encoded >> 8) & 0xFF);
            result[offset + 1] = (byte)(encoded & 0xFF);
            controlValueIndex += 1;
        }

        return result;
    }

    void ValidateCoordinates(double[] normalizedCoordinates)
    {
        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        int axisCount = variations[0].GetPeakTuple().GetAxisCount();

        if (normalizedCoordinates.Length != axisCount)
        {
            throw new System.ArgumentException(
                "cvar coordinates do not match the variation axes.",
                "normalizedCoordinates");
        }

        int axisIndex = 0;

        while (axisIndex < normalizedCoordinates.Length)
        {
            double coordinate = normalizedCoordinates[axisIndex];

            if (!double.IsFinite(coordinate) || coordinate < -1 || coordinate > 1)
            {
                throw new System.ArgumentOutOfRangeException(
                    "normalizedCoordinates");
            }

            axisIndex += 1;
        }
    }

    static void ApplyVariation(
        SfntCvarTupleVariation variation,
        double[] normalizedCoordinates,
        double[] accumulatedDeltas)
    {
        double scalar = variation.CalculateScalar(normalizedCoordinates);

        if (scalar == 0)
        {
            return;
        }

        SfntGvarPointNumbers numbers = variation.GetControlValueNumbers();
        int deltaIndex = 0;

        while (deltaIndex < variation.GetDeltaCount())
        {
            int controlValueIndex = numbers.UsesAllPoints() ?
                deltaIndex : numbers.GetPointNumber(deltaIndex);
            accumulatedDeltas[controlValueIndex] +=
                variation.GetDelta(deltaIndex) * scalar;
            deltaIndex += 1;
        }
    }
}

internal static class SfntCvarTableParser
{
    const int HeaderLength = 8;
    const int SharedControlValueNumbers = 0x8000;
    const int TupleCountMask = 0x0FFF;
    const int EmbeddedPeakTuple = 0x8000;
    const int IntermediateRegion = 0x4000;
    const int PrivateControlValueNumbers = 0x2000;
    const int TupleIndexMask = 0x0FFF;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cvar,
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

    internal static SfntCvarTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntCvarTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The cvar table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int encodedTupleCount = reader.ReadUInt16();
        int dataOffset = reader.ReadOffset16();

        if (majorVersion != version.GetMajorVersion() ||
            minorVersion != version.GetMinorVersion())
        {
            throw FontFormatException.Create("The cvar table version is inconsistent.");
        }

        if ((encodedTupleCount & 0x7000) != 0)
        {
            throw FontFormatException.Create(
                "The cvar tuple count has reserved bits set.");
        }

        int tupleCount = encodedTupleCount & TupleCountMask;

        if (tupleCount == 0)
        {
            throw FontFormatException.Create(
                "The cvar table has no tuple variations.");
        }

        if (tupleCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "The cvar table exceeds the configured variation-region limit.");
        }

        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create(
                "The cvar table requires a supported fvar table.");
        }

        SfntTableData? controlValueTable = cache.FindRawTable(FontTags.Cvt);

        if (controlValueTable == null ||
            controlValueTable.GetDataSlice().GetLength() < 2)
        {
            throw FontFormatException.Create(
                "The cvar table requires a nonempty cvt table.");
        }

        int controlValueCount =
            controlValueTable.GetDataSlice().GetLength() / 2;
        int axisCount = fvar.GetAxisCount();
        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        budget.AddEntries(tupleCount);
        SfntGvarTupleHeader[] headers = new SfntGvarTupleHeader[tupleCount];
        int headerIndex = 0;

        while (headerIndex < headers.Length)
        {
            headers[headerIndex] = ReadHeader(reader, axisCount, budget);
            headerIndex += 1;
        }

        if (dataOffset < reader.GetPosition() || dataOffset > tableData.GetLength())
        {
            throw FontFormatException.Create(
                "The cvar serialized-data offset is invalid.");
        }

        BigEndianReader serializedReader = new BigEndianReader(
            tableData.GetSlice(dataOffset, tableData.GetLength() - dataOffset));
        bool hasSharedNumbers =
            (encodedTupleCount & SharedControlValueNumbers) != 0;
        SfntGvarPointNumbers? sharedNumbers = null;

        if (hasSharedNumbers)
        {
            sharedNumbers = SfntGvarPackedDataDecoder.ReadPointNumbers(
                serializedReader,
                controlValueCount,
                budget);
        }

        int tupleDataOffset = CheckedFontMath.AddOffset(
            dataOffset,
            serializedReader.GetPosition());
        SfntCvarTupleVariation[] variations =
            new SfntCvarTupleVariation[tupleCount];
        int variationIndex = 0;

        while (variationIndex < variations.Length)
        {
            SfntGvarTupleHeader header = headers[variationIndex];
            int dataSize = header.GetVariationDataSize();

            if (dataSize > tableData.GetLength() - tupleDataOffset)
            {
                throw FontFormatException.Create(
                    "A cvar tuple's serialized variation data is truncated.");
            }

            BigEndianReader tupleReader = new BigEndianReader(
                tableData.GetSlice(tupleDataOffset, dataSize));
            SfntGvarPointNumbers? numbers =
                header.HasPrivatePointNumbers() ?
                SfntGvarPackedDataDecoder.ReadPointNumbers(
                    tupleReader,
                    controlValueCount,
                    budget) : sharedNumbers;

            if (numbers == null)
            {
                numbers = new SfntGvarPointNumbers(true, new int[0]);
            }

            int deltaCount = numbers.UsesAllPoints() ?
                controlValueCount : numbers.GetCount();
            int[] deltas = SfntGvarPackedDataDecoder.ReadDeltas(
                tupleReader,
                deltaCount,
                budget);

            if (tupleReader.GetRemainingLength() != 0)
            {
                throw FontFormatException.Create(
                    "A cvar tuple has unused serialized variation data.");
            }

            variations[variationIndex] = new SfntCvarTupleVariation(
                dataSize,
                header.GetTupleIndex(),
                header.HasIntermediateRegion(),
                header.HasPrivatePointNumbers(),
                header.GetPeakTuple(),
                header.GetStartTuple(),
                header.GetEndTuple(),
                numbers,
                deltas);
            tupleDataOffset = CheckedFontMath.AddOffset(
                tupleDataOffset,
                dataSize);
            variationIndex += 1;
        }

        if (tupleDataOffset != tableData.GetLength())
        {
            throw FontFormatException.Create(
                "The cvar table has unused serialized variation data.");
        }

        return new SfntCvarTable(
            majorVersion,
            minorVersion,
            controlValueCount,
            hasSharedNumbers,
            sharedNumbers,
            variations);
    }

    static SfntGvarTupleHeader ReadHeader(
        BigEndianReader reader,
        int axisCount,
        SfntVariationParsingBudget budget)
    {
        int variationDataSize = reader.ReadUInt16();
        int encodedTupleIndex = reader.ReadUInt16();

        if ((encodedTupleIndex & 0x1000) != 0)
        {
            throw FontFormatException.Create(
                "A cvar tuple header has a reserved flag set.");
        }

        if ((encodedTupleIndex & EmbeddedPeakTuple) == 0)
        {
            throw FontFormatException.Create(
                "A cvar tuple header is missing its embedded peak tuple.");
        }

        bool intermediate = (encodedTupleIndex & IntermediateRegion) != 0;
        bool privateNumbers =
            (encodedTupleIndex & PrivateControlValueNumbers) != 0;
        int tupleIndex = encodedTupleIndex & TupleIndexMask;
        SfntGvarTuple peak = SfntGvarGlyphDataParser.ReadTuple(
            reader,
            axisCount,
            budget);
        SfntGvarTuple? start = null;
        SfntGvarTuple? end = null;

        if (intermediate)
        {
            start = SfntGvarGlyphDataParser.ReadTuple(
                reader,
                axisCount,
                budget);
            end = SfntGvarGlyphDataParser.ReadTuple(
                reader,
                axisCount,
                budget);
        }

        return new SfntGvarTupleHeader(
            variationDataSize,
            tupleIndex,
            true,
            intermediate,
            privateNumbers,
            peak,
            start,
            end);
    }
}
