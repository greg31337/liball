namespace ttf;

internal class SfntLayoutGlyphClassParserContext
{
    FontDataSlice tableData;
    int structureOffset;
    int fontGlyphCount;
    FontLimits limits;

    internal SfntLayoutGlyphClassParserContext(
        FontDataSlice tableData,
        int structureOffset,
        int fontGlyphCount,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.structureOffset = structureOffset;
        this.fontGlyphCount = fontGlyphCount;
        this.limits = limits;
    }

    internal SfntCoverageTable ParseCoverage()
    {
        BigEndianReader reader = CreateReader(
            2,
            "A Coverage table header is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParseCoverageFormat1();
        }

        if (format == 2)
        {
            return ParseCoverageFormat2();
        }

        throw FontFormatException.Create(
            "The Coverage table format is not supported.");
    }

    internal SfntClassDefinitionTable ParseClassDefinition()
    {
        BigEndianReader reader = CreateReader(
            2,
            "A ClassDef table header is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParseClassDefinitionFormat1();
        }

        if (format == 2)
        {
            return ParseClassDefinitionFormat2();
        }

        throw FontFormatException.Create(
            "The ClassDef table format is not supported.");
    }

    SfntCoverageTable ParseCoverageFormat1()
    {
        BigEndianReader reader = CreateReader(
            4,
            "A format 1 Coverage table header is truncated.");
        reader.ReadUInt16();
        int count = reader.ReadUInt16();
        ValidateStoredCount(
            count,
            "A Coverage table exceeds the configured entry limit.");
        int length = CheckedFontMath.GetRangeEnd(4, count, 2);
        reader = CreateReader(
            length,
            "A format 1 Coverage glyph array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] glyphIds = new int[count];
        int previousGlyphId = -1;
        int index = 0;

        while (index < count)
        {
            int glyphId = reader.ReadUInt16();
            ValidateGlyphId(glyphId, "A Coverage table glyph ID is invalid.");

            if (glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "A format 1 Coverage glyph array is not strictly ordered.");
            }

            glyphIds[index] = glyphId;
            previousGlyphId = glyphId;
            index += 1;
        }

        return new SfntCoverageTable(
            1,
            glyphIds,
            new SfntCoverageRange[0],
            count);
    }

    SfntCoverageTable ParseCoverageFormat2()
    {
        BigEndianReader reader = CreateReader(
            4,
            "A format 2 Coverage table header is truncated.");
        reader.ReadUInt16();
        int rangeCount = reader.ReadUInt16();
        ValidateStoredCount(
            rangeCount,
            "A Coverage table exceeds the configured entry limit.");
        int length = CheckedFontMath.GetRangeEnd(4, rangeCount, 6);
        reader = CreateReader(
            length,
            "A format 2 Coverage range array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntCoverageRange[] ranges =
            new SfntCoverageRange[rangeCount];
        int previousEndGlyphId = -1;
        int expectedCoverageIndex = 0;
        int index = 0;

        while (index < rangeCount)
        {
            int startGlyphId = reader.ReadUInt16();
            int endGlyphId = reader.ReadUInt16();
            int startCoverageIndex = reader.ReadUInt16();
            ValidateRange(
                startGlyphId,
                endGlyphId,
                previousEndGlyphId,
                "A Coverage range is invalid.");

            if (startCoverageIndex != expectedCoverageIndex)
            {
                throw FontFormatException.Create(
                    "Coverage indexes are not sequential.");
            }

            ranges[index] = new SfntCoverageRange(
                startGlyphId,
                endGlyphId,
                startCoverageIndex);
            previousEndGlyphId = endGlyphId;
            expectedCoverageIndex = CheckedFontMath.AddCounts(
                expectedCoverageIndex,
                endGlyphId - startGlyphId + 1);
            index += 1;
        }

        return new SfntCoverageTable(
            2,
            new int[0],
            ranges,
            expectedCoverageIndex);
    }

    SfntClassDefinitionTable ParseClassDefinitionFormat1()
    {
        BigEndianReader reader = CreateReader(
            6,
            "A format 1 ClassDef table header is truncated.");
        reader.ReadUInt16();
        int startGlyphId = reader.ReadUInt16();
        int count = reader.ReadUInt16();
        ValidateStoredCount(
            count,
            "A ClassDef table exceeds the configured entry limit.");

        if (count > 0 &&
            (startGlyphId >= fontGlyphCount ||
            count > fontGlyphCount - startGlyphId))
        {
            throw FontFormatException.Create(
                "A format 1 ClassDef glyph range is invalid.");
        }

        int length = CheckedFontMath.GetRangeEnd(6, count, 2);
        reader = CreateReader(
            length,
            "A format 1 ClassDef value array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] classValues = new int[count];
        int maximumClassValue = 0;
        int index = 0;

        while (index < count)
        {
            int classValue = reader.ReadUInt16();
            classValues[index] = classValue;
            maximumClassValue = System.Math.Max(
                maximumClassValue,
                classValue);
            index += 1;
        }

        return new SfntClassDefinitionTable(
            1,
            startGlyphId,
            classValues,
            new SfntClassRange[0],
            maximumClassValue);
    }

    SfntClassDefinitionTable ParseClassDefinitionFormat2()
    {
        BigEndianReader reader = CreateReader(
            4,
            "A format 2 ClassDef table header is truncated.");
        reader.ReadUInt16();
        int rangeCount = reader.ReadUInt16();
        ValidateStoredCount(
            rangeCount,
            "A ClassDef table exceeds the configured entry limit.");
        int length = CheckedFontMath.GetRangeEnd(4, rangeCount, 6);
        reader = CreateReader(
            length,
            "A format 2 ClassDef range array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntClassRange[] ranges = new SfntClassRange[rangeCount];
        int previousEndGlyphId = -1;
        int maximumClassValue = 0;
        int index = 0;

        while (index < rangeCount)
        {
            int startGlyphId = reader.ReadUInt16();
            int endGlyphId = reader.ReadUInt16();
            int classValue = reader.ReadUInt16();
            ValidateRange(
                startGlyphId,
                endGlyphId,
                previousEndGlyphId,
                "A ClassDef range is invalid.");
            ranges[index] = new SfntClassRange(
                startGlyphId,
                endGlyphId,
                classValue);
            previousEndGlyphId = endGlyphId;
            maximumClassValue = System.Math.Max(
                maximumClassValue,
                classValue);
            index += 1;
        }

        return new SfntClassDefinitionTable(
            2,
            0,
            new int[0],
            ranges,
            maximumClassValue);
    }

    BigEndianReader CreateReader(int requiredLength, string message)
    {
        if (structureOffset < 0 ||
            requiredLength < 0 ||
            structureOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return new BigEndianReader(tableData.GetSlice(
            structureOffset,
            tableData.GetLength() - structureOffset));
    }

    void ValidateGlyphId(int glyphId, string message)
    {
        if (glyphId < 0 || glyphId >= fontGlyphCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    void ValidateRange(
        int startGlyphId,
        int endGlyphId,
        int previousEndGlyphId,
        string message)
    {
        if (startGlyphId > endGlyphId ||
            startGlyphId <= previousEndGlyphId ||
            endGlyphId >= fontGlyphCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    void ValidateStoredCount(int count, string message)
    {
        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(message);
        }
    }
}

internal static class SfntLayoutGlyphClassParser
{
    internal static SfntCoverageTable ParseCoverage(
        FontDataSlice tableData,
        int structureOffset,
        int fontGlyphCount,
        FontLimits limits)
    {
        ValidateArguments(tableData, fontGlyphCount, limits);
        return new SfntLayoutGlyphClassParserContext(
            tableData,
            structureOffset,
            fontGlyphCount,
            limits).ParseCoverage();
    }

    internal static SfntClassDefinitionTable ParseClassDefinition(
        FontDataSlice tableData,
        int structureOffset,
        int fontGlyphCount,
        FontLimits limits)
    {
        ValidateArguments(tableData, fontGlyphCount, limits);
        return new SfntLayoutGlyphClassParserContext(
            tableData,
            structureOffset,
            fontGlyphCount,
            limits).ParseClassDefinition();
    }

    static void ValidateArguments(
        FontDataSlice tableData,
        int fontGlyphCount,
        FontLimits limits)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (fontGlyphCount < 0 || fontGlyphCount > 65535)
        {
            throw new System.ArgumentOutOfRangeException("fontGlyphCount");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }
    }
}
