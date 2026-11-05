namespace ttf;

internal class SfntMathParserContext
{
    const int ConstantsLength = 214;
    const int ConstantValueCount = 51;
    FontDataSlice tableData;
    FontLimits limits;
    int glyphCount;
    int parsedEntries;

    internal SfntMathParserContext(
        FontDataSlice tableData,
        FontLimits limits,
        int glyphCount)
    {
        this.tableData = tableData;
        this.limits = limits;
        this.glyphCount = glyphCount;
        parsedEntries = 0;
    }

    internal SfntMathTable Parse()
    {
        const int HeaderLength = 10;
        BigEndianReader reader = CreateReader(
            0,
            HeaderLength,
            "The MATH table header is truncated.");
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int constantsOffset = reader.ReadUInt16();
        int glyphInfoOffset = reader.ReadUInt16();
        int variantsOffset = reader.ReadUInt16();

        if (majorVersion != 1)
        {
            throw FontFormatException.Create(
                "The MATH table version is not supported.");
        }

        SfntMathConstants constants = ParseConstants(
            ResolveRequiredOffset(
                0,
                constantsOffset,
                HeaderLength,
                ConstantsLength,
                "The MATH constants offset is invalid."));
        SfntMathGlyphInfo glyphInfo = ParseGlyphInfo(
            ResolveRequiredOffset(
                0,
                glyphInfoOffset,
                HeaderLength,
                8,
                "The MATH glyph-info offset is invalid."));
        SfntMathVariants variants = ParseVariants(
            ResolveRequiredOffset(
                0,
                variantsOffset,
                HeaderLength,
                10,
                "The MATH variants offset is invalid."));
        return new SfntMathTable(
            majorVersion,
            minorVersion,
            constants,
            glyphInfo,
            variants);
    }

    SfntMathConstants ParseConstants(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            ConstantsLength,
            "The MATH constants table is truncated.");
        int scriptPercent = reader.ReadInt16();
        int scriptScriptPercent = reader.ReadInt16();
        int delimitedHeight = reader.ReadUInt16();
        int displayHeight = reader.ReadUInt16();
        SfntMathValue[] values = ParseValues(
            offset,
            CheckedFontMath.AddOffset(offset, 8),
            ConstantValueCount,
            ConstantsLength);
        reader = CreateReader(
            CheckedFontMath.AddOffset(offset, ConstantsLength - 2),
            2,
            "The MATH constants table is truncated.");
        int radicalDegreePercent = reader.ReadInt16();
        return new SfntMathConstants(
            scriptPercent,
            scriptScriptPercent,
            delimitedHeight,
            displayHeight,
            values,
            radicalDegreePercent);
    }

    SfntMathGlyphInfo ParseGlyphInfo(int offset)
    {
        const int HeaderLength = 8;
        BigEndianReader reader = CreateReader(
            offset,
            HeaderLength,
            "The MATH glyph-info table is truncated.");
        int italicsOffset = reader.ReadUInt16();
        int accentOffset = reader.ReadUInt16();
        int extendedCoverageOffset = reader.ReadUInt16();
        int kernOffset = reader.ReadUInt16();
        SfntMathGlyphValueTable? italics = italicsOffset == 0 ? null :
            ParseGlyphValueTable(ResolveOffset(
                offset,
                italicsOffset,
                HeaderLength,
                4,
                "The MATH italics-correction offset is invalid."));
        SfntMathGlyphValueTable? accents = accentOffset == 0 ? null :
            ParseGlyphValueTable(ResolveOffset(
                offset,
                accentOffset,
                HeaderLength,
                4,
                "The MATH top-accent offset is invalid."));
        SfntCoverageTable? extendedCoverage =
            extendedCoverageOffset == 0 ? null :
            ParseCoverage(ResolveOffset(
                offset,
                extendedCoverageOffset,
                HeaderLength,
                4,
                "The MATH extended-shape Coverage offset is invalid."));
        SfntMathKernInfo? kernInfo = kernOffset == 0 ? null :
            ParseKernInfo(ResolveOffset(
                offset,
                kernOffset,
                HeaderLength,
                4,
                "The MATH kern-info offset is invalid."));
        return new SfntMathGlyphInfo(
            italics,
            accents,
            extendedCoverage,
            kernInfo);
    }

    SfntMathGlyphValueTable ParseGlyphValueTable(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A MATH glyph-value table is truncated.");
        int coverageOffset = reader.ReadUInt16();
        int valueCount = reader.ReadUInt16();
        AddEntries(valueCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            valueCount,
            4);
        CreateReader(
            offset,
            headerLength,
            "A MATH glyph-value array is truncated.");
        SfntCoverageTable coverage = ParseCoverage(
            ResolveRequiredOffset(
                offset,
                coverageOffset,
                headerLength,
                4,
                "A MATH glyph-value Coverage offset is invalid."));
        ValidateCoverageCount(coverage, valueCount);
        SfntMathValue[] values = ParseValues(
            offset,
            CheckedFontMath.AddOffset(offset, 4),
            valueCount,
            headerLength);
        return new SfntMathGlyphValueTable(coverage, values);
    }

    SfntMathKernInfo ParseKernInfo(int offset)
    {
        const int RecordLength = 8;
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "The MATH kern-info table is truncated.");
        int coverageOffset = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();
        AddEntries(recordCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            recordCount,
            RecordLength);
        reader = CreateReader(
            offset,
            headerLength,
            "The MATH kern-info record array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] offsets = new int[CheckedFontMath.MultiplyCount(
            recordCount,
            4)];
        int index = 0;

        while (index < offsets.Length)
        {
            offsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntCoverageTable coverage = ParseCoverage(
            ResolveRequiredOffset(
                offset,
                coverageOffset,
                headerLength,
                4,
                "The MATH kern-info Coverage offset is invalid."));
        ValidateCoverageCount(coverage, recordCount);
        SfntMathKernRecord[] records =
            new SfntMathKernRecord[recordCount];
        index = 0;

        while (index < recordCount)
        {
            int offsetIndex = index * 4;
            records[index] = new SfntMathKernRecord(
                ParseOptionalKern(
                    offset,
                    offsets[offsetIndex],
                    headerLength),
                ParseOptionalKern(
                    offset,
                    offsets[offsetIndex + 1],
                    headerLength),
                ParseOptionalKern(
                    offset,
                    offsets[offsetIndex + 2],
                    headerLength),
                ParseOptionalKern(
                    offset,
                    offsets[offsetIndex + 3],
                    headerLength));
            index += 1;
        }

        return new SfntMathKernInfo(coverage, records);
    }

    SfntMathKern? ParseOptionalKern(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset)
    {
        if (relativeOffset == 0)
        {
            return null;
        }

        int offset = ResolveOffset(
            parentOffset,
            relativeOffset,
            minimumRelativeOffset,
            2,
            "A MATH kern offset is invalid.");
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A MATH kern table is truncated.");
        int heightCount = reader.ReadUInt16();
        int kernValueCount = CheckedFontMath.AddCounts(heightCount, 1);
        AddEntries(CheckedFontMath.AddCounts(
            heightCount,
            kernValueCount));
        int heightsLength = CheckedFontMath.MultiplyCount(heightCount, 4);
        int headerLength = CheckedFontMath.GetRangeEnd(
            CheckedFontMath.AddOffset(2, heightsLength),
            kernValueCount,
            4);
        CreateReader(
            offset,
            headerLength,
            "A MATH kern value array is truncated.");
        SfntMathValue[] heights = ParseValues(
            offset,
            CheckedFontMath.AddOffset(offset, 2),
            heightCount,
            headerLength);
        int index = 1;

        while (index < heights.Length)
        {
            if (heights[index].GetValue() <=
                heights[index - 1].GetValue())
            {
                throw FontFormatException.Create(
                    "MATH kern correction heights are not strictly ordered.");
            }

            index += 1;
        }

        SfntMathValue[] kernValues = ParseValues(
            offset,
            CheckedFontMath.AddOffset(
                CheckedFontMath.AddOffset(offset, 2),
                heightsLength),
            kernValueCount,
            headerLength);
        return new SfntMathKern(heights, kernValues);
    }

    SfntMathVariants ParseVariants(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            10,
            "The MATH variants table is truncated.");
        int minimumOverlap = reader.ReadUInt16();
        int verticalCoverageOffset = reader.ReadUInt16();
        int horizontalCoverageOffset = reader.ReadUInt16();
        int verticalCount = reader.ReadUInt16();
        int horizontalCount = reader.ReadUInt16();
        AddEntries(CheckedFontMath.AddCounts(
            verticalCount,
            horizontalCount));
        int constructionCount = CheckedFontMath.AddCounts(
            verticalCount,
            horizontalCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            10,
            constructionCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "The MATH construction-offset arrays are truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] verticalOffsets = new int[verticalCount];
        int[] horizontalOffsets = new int[horizontalCount];
        int index = 0;

        while (index < verticalCount)
        {
            verticalOffsets[index] = reader.ReadUInt16();
            index += 1;
        }

        index = 0;

        while (index < horizontalCount)
        {
            horizontalOffsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntCoverageTable verticalCoverage = ParseCoverage(
            ResolveRequiredOffset(
                offset,
                verticalCoverageOffset,
                headerLength,
                4,
                "The MATH vertical Coverage offset is invalid."));
        SfntCoverageTable horizontalCoverage = ParseCoverage(
            ResolveRequiredOffset(
                offset,
                horizontalCoverageOffset,
                headerLength,
                4,
                "The MATH horizontal Coverage offset is invalid."));
        ValidateCoverageCount(verticalCoverage, verticalCount);
        ValidateCoverageCount(horizontalCoverage, horizontalCount);
        SfntMathGlyphConstruction[] vertical =
            ParseConstructions(
                offset,
                verticalOffsets,
                headerLength);
        SfntMathGlyphConstruction[] horizontal =
            ParseConstructions(
                offset,
                horizontalOffsets,
                headerLength);
        return new SfntMathVariants(
            minimumOverlap,
            verticalCoverage,
            horizontalCoverage,
            vertical,
            horizontal);
    }

    SfntMathGlyphConstruction[] ParseConstructions(
        int parentOffset,
        int[] relativeOffsets,
        int minimumRelativeOffset)
    {
        SfntMathGlyphConstruction[] constructions =
            new SfntMathGlyphConstruction[relativeOffsets.Length];
        int index = 0;

        while (index < relativeOffsets.Length)
        {
            int offset = ResolveRequiredOffset(
                parentOffset,
                relativeOffsets[index],
                minimumRelativeOffset,
                4,
                "A MATH glyph-construction offset is invalid.");
            constructions[index] = ParseConstruction(offset);
            index += 1;
        }

        return constructions;
    }

    SfntMathGlyphConstruction ParseConstruction(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A MATH glyph-construction table is truncated.");
        int assemblyOffset = reader.ReadUInt16();
        int variantCount = reader.ReadUInt16();
        AddEntries(variantCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            variantCount,
            4);
        reader = CreateReader(
            offset,
            headerLength,
            "A MATH glyph-variant array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntMathGlyphVariant[] variants =
            new SfntMathGlyphVariant[variantCount];
        int index = 0;

        while (index < variantCount)
        {
            int glyphId = reader.ReadUInt16();
            int advance = reader.ReadUInt16();
            ValidateGlyphId(
                glyphId,
                "A MATH variant glyph ID is invalid.");
            variants[index] = new SfntMathGlyphVariant(
                glyphId,
                advance);
            index += 1;
        }

        SfntMathGlyphAssembly? assembly = assemblyOffset == 0 ? null :
            ParseAssembly(ResolveOffset(
                offset,
                assemblyOffset,
                headerLength,
                6,
                "A MATH glyph-assembly offset is invalid."));
        return new SfntMathGlyphConstruction(assembly, variants);
    }

    SfntMathGlyphAssembly ParseAssembly(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A MATH glyph-assembly table is truncated.");
        int partCount = ReadUInt16At(
            CheckedFontMath.AddOffset(offset, 4),
            "A MATH glyph-assembly table is truncated.");

        if (partCount == 0)
        {
            throw FontFormatException.Create(
                "A MATH glyph assembly has no parts.");
        }

        AddEntries(partCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            partCount,
            10);
        reader = CreateReader(
            offset,
            headerLength,
            "A MATH glyph-part array is truncated.");
        SfntMathValue italics = ParseValue(
            offset,
            offset,
            headerLength);
        reader.ReadInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntMathGlyphPart[] parts = new SfntMathGlyphPart[partCount];
        int index = 0;

        while (index < partCount)
        {
            int glyphId = reader.ReadUInt16();
            int startConnector = reader.ReadUInt16();
            int endConnector = reader.ReadUInt16();
            int fullAdvance = reader.ReadUInt16();
            int flags = reader.ReadUInt16();
            ValidateGlyphId(
                glyphId,
                "A MATH assembly part glyph ID is invalid.");

            if ((flags & 0xFFFE) != 0)
            {
                throw FontFormatException.Create(
                    "A MATH assembly part uses reserved flags.");
            }

            parts[index] = new SfntMathGlyphPart(
                glyphId,
                startConnector,
                endConnector,
                fullAdvance,
                flags);
            index += 1;
        }

        return new SfntMathGlyphAssembly(italics, parts);
    }

    SfntMathValue[] ParseValues(
        int parentOffset,
        int valuesOffset,
        int count,
        int minimumDeviceOffset)
    {
        SfntMathValue[] values = new SfntMathValue[count];
        int index = 0;
        int offset = valuesOffset;

        while (index < count)
        {
            values[index] = ParseValue(
                parentOffset,
                offset,
                minimumDeviceOffset);
            offset = CheckedFontMath.AddOffset(offset, 4);
            index += 1;
        }

        return values;
    }

    SfntMathValue ParseValue(
        int parentOffset,
        int recordOffset,
        int minimumDeviceOffset)
    {
        BigEndianReader reader = CreateReader(
            recordOffset,
            4,
            "A MATH value record is truncated.");
        int value = reader.ReadInt16();
        int deviceOffset = reader.ReadUInt16();
        int deviceTableOffset = 0;
        SfntDeviceOrVariationIndex? adjustment = null;

        if (deviceOffset != 0)
        {
            deviceTableOffset = ResolveOffset(
                parentOffset,
                deviceOffset,
                minimumDeviceOffset,
                6,
                "A MATH Device or VariationIndex offset is invalid.");
            adjustment =
                SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
                    tableData,
                    deviceTableOffset,
                    limits);
        }

        return new SfntMathValue(
            value,
            deviceOffset,
            deviceTableOffset,
            adjustment);
    }

    SfntCoverageTable ParseCoverage(int offset)
    {
        SfntCoverageTable coverage =
            SfntLayoutGlyphClassParser.ParseCoverage(
                tableData,
                offset,
                glyphCount,
                limits);
        AddEntries(coverage.GetCoveredGlyphCount());
        return coverage;
    }

    void ValidateCoverageCount(
        SfntCoverageTable coverage,
        int expectedCount)
    {
        if (coverage.GetCoveredGlyphCount() != expectedCount)
        {
            throw FontFormatException.Create(
                "A MATH count does not match its Coverage table.");
        }
    }

    void ValidateGlyphId(int glyphId, string message)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    int ReadUInt16At(int offset, string message)
    {
        return CreateReader(offset, 2, message).ReadUInt16();
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

        int offset = CheckedFontMath.AddOffset(
            baseOffset,
            relativeOffset);

        if (offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return offset;
    }

    void AddEntries(int count)
    {
        if (count > limits.GetMaxCoverageEntries() - parsedEntries)
        {
            throw FontLimitException.Create(
                "The MATH table exceeds the configured coverage-entry limit.");
        }

        parsedEntries += count;
    }
}

internal static class SfntMathTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Math,
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

    internal static SfntMathTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntMathTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The MATH table requires a supported maxp table.");
        }

        return new SfntMathParserContext(
            table.GetDataSlice(),
            limits,
            maxp.GetGlyphCount()).Parse();
    }
}
