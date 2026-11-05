namespace ttf;

internal enum SfntMaxpKind
{
    Cff = 0,
    TrueType = 1
}

internal class SfntMaxpTable
{
    uint version;
    SfntMaxpKind kind;
    int glyphCount;
    int maxPoints;
    int maxContours;
    int maxCompositePoints;
    int maxCompositeContours;
    int maxZones;
    int maxTwilightPoints;
    int maxStorage;
    int maxFunctionDefinitions;
    int maxInstructionDefinitions;
    int maxStackElements;
    int maxInstructionSize;
    int maxComponentElements;
    int maxComponentDepth;

    internal SfntMaxpTable(
        uint version,
        SfntMaxpKind kind,
        int glyphCount,
        int maxPoints,
        int maxContours,
        int maxCompositePoints,
        int maxCompositeContours,
        int maxZones,
        int maxTwilightPoints,
        int maxStorage,
        int maxFunctionDefinitions,
        int maxInstructionDefinitions,
        int maxStackElements,
        int maxInstructionSize,
        int maxComponentElements,
        int maxComponentDepth)
    {
        this.version = version;
        this.kind = kind;
        this.glyphCount = glyphCount;
        this.maxPoints = maxPoints;
        this.maxContours = maxContours;
        this.maxCompositePoints = maxCompositePoints;
        this.maxCompositeContours = maxCompositeContours;
        this.maxZones = maxZones;
        this.maxTwilightPoints = maxTwilightPoints;
        this.maxStorage = maxStorage;
        this.maxFunctionDefinitions = maxFunctionDefinitions;
        this.maxInstructionDefinitions = maxInstructionDefinitions;
        this.maxStackElements = maxStackElements;
        this.maxInstructionSize = maxInstructionSize;
        this.maxComponentElements = maxComponentElements;
        this.maxComponentDepth = maxComponentDepth;
    }

    internal uint GetVersionRaw()
    {
        return version;
    }

    internal double GetVersion()
    {
        return kind == SfntMaxpKind.Cff ? 0.5 : 1.0;
    }

    internal SfntMaxpKind GetKind()
    {
        return kind;
    }

    internal int GetGlyphCount()
    {
        return glyphCount;
    }

    internal int GetMaxPoints()
    {
        EnsureTrueType();
        return maxPoints;
    }

    internal int GetMaxContours()
    {
        EnsureTrueType();
        return maxContours;
    }

    internal int GetMaxCompositePoints()
    {
        EnsureTrueType();
        return maxCompositePoints;
    }

    internal int GetMaxCompositeContours()
    {
        EnsureTrueType();
        return maxCompositeContours;
    }

    internal int GetMaxZones()
    {
        EnsureTrueType();
        return maxZones;
    }

    internal int GetMaxTwilightPoints()
    {
        EnsureTrueType();
        return maxTwilightPoints;
    }

    internal int GetMaxStorage()
    {
        EnsureTrueType();
        return maxStorage;
    }

    internal int GetMaxFunctionDefinitions()
    {
        EnsureTrueType();
        return maxFunctionDefinitions;
    }

    internal int GetMaxInstructionDefinitions()
    {
        EnsureTrueType();
        return maxInstructionDefinitions;
    }

    internal int GetMaxStackElements()
    {
        EnsureTrueType();
        return maxStackElements;
    }

    internal int GetMaxInstructionSize()
    {
        EnsureTrueType();
        return maxInstructionSize;
    }

    internal int GetMaxComponentElements()
    {
        EnsureTrueType();
        return maxComponentElements;
    }

    internal int GetMaxComponentDepth()
    {
        EnsureTrueType();
        return maxComponentDepth;
    }

    internal void VerifyGlyphCount(int observedGlyphCount)
    {
        ValidateNonNegative(observedGlyphCount, "observedGlyphCount");

        if (observedGlyphCount != glyphCount)
        {
            throw FontFormatException.Create(
                "The observed glyph count does not match the maxp declaration.");
        }
    }

    internal void VerifySimpleGlyph(
        int pointCount,
        int contourCount,
        int instructionSize,
        FontLimits limits)
    {
        EnsureTrueType();
        ValidateGlyphArguments(
            pointCount,
            contourCount,
            instructionSize,
            limits);

        if (pointCount > maxPoints ||
            contourCount > maxContours ||
            instructionSize > maxInstructionSize)
        {
            throw FontFormatException.Create(
                "A simple glyph exceeds a maximum declared by maxp.");
        }
    }

    internal void VerifyCompositeGlyph(
        int pointCount,
        int contourCount,
        int instructionSize,
        int componentElements,
        int componentDepth,
        FontLimits limits)
    {
        EnsureTrueType();
        ValidateGlyphArguments(
            pointCount,
            contourCount,
            instructionSize,
            limits);
        ValidateNonNegative(componentElements, "componentElements");
        ValidateNonNegative(componentDepth, "componentDepth");

        if (componentDepth > limits.GetMaxComponentDepth())
        {
            throw FontLimitException.Create(
                "A composite glyph exceeds the configured component-depth limit.");
        }

        if (pointCount > maxCompositePoints ||
            contourCount > maxCompositeContours ||
            instructionSize > maxInstructionSize ||
            componentElements > maxComponentElements ||
            componentDepth > maxComponentDepth)
        {
            throw FontFormatException.Create(
                "A composite glyph exceeds a maximum declared by maxp.");
        }
    }

    internal void VerifyHintingResourceUsage(
        int zones,
        int twilightPoints,
        int storageLocations,
        int functionDefinitions,
        int instructionDefinitions,
        int stackElements)
    {
        EnsureTrueType();
        ValidateNonNegative(zones, "zones");
        ValidateNonNegative(twilightPoints, "twilightPoints");
        ValidateNonNegative(storageLocations, "storageLocations");
        ValidateNonNegative(functionDefinitions, "functionDefinitions");
        ValidateNonNegative(instructionDefinitions, "instructionDefinitions");
        ValidateNonNegative(stackElements, "stackElements");

        if (zones > maxZones ||
            twilightPoints > maxTwilightPoints ||
            storageLocations > maxStorage ||
            functionDefinitions > maxFunctionDefinitions ||
            instructionDefinitions > maxInstructionDefinitions ||
            stackElements > maxStackElements)
        {
            throw FontFormatException.Create(
                "Hinting resource usage exceeds a maximum declared by maxp.");
        }
    }

    void ValidateGlyphArguments(
        int pointCount,
        int contourCount,
        int instructionSize,
        FontLimits limits)
    {
        ValidateNonNegative(pointCount, "pointCount");
        ValidateNonNegative(contourCount, "contourCount");
        ValidateNonNegative(instructionSize, "instructionSize");

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (pointCount > limits.GetMaxPointsPerGlyph())
        {
            throw FontLimitException.Create(
                "A glyph exceeds the configured point-count limit.");
        }

        if (contourCount > limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create(
                "A glyph exceeds the configured contour-count limit.");
        }
    }

    void EnsureTrueType()
    {
        if (kind != SfntMaxpKind.TrueType)
        {
            throw new System.InvalidOperationException(
                "The CFF maxp form does not contain TrueType maximum fields.");
        }
    }

    static void ValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal static class SfntMaxpTableParser
{
    const uint CffVersion = 0x00005000U;
    const uint TrueTypeVersion = 0x00010000U;
    const int CffLength = 6;
    const int TrueTypeLength = 32;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Maxp,
        0,
        1,
        5,
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

    internal static SfntMaxpTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntMaxpTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        uint version = reader.ReadUInt32();

        if (version == CffVersion)
        {
            return new SfntTableVersion(0, 5);
        }

        return new SfntTableVersion(
            (int)(version >> 16),
            (int)(version & 0xFFFFU));
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        uint encodedVersion = reader.ReadUInt32();

        if (encodedVersion == CffVersion)
        {
            return ParseCff(table, reader, limits);
        }

        if (encodedVersion == TrueTypeVersion)
        {
            return ParseTrueType(table, reader, limits);
        }

        throw FontFormatException.Create("The maxp table version is not supported.");
    }

    static SfntMaxpTable ParseCff(
        SfntTableData table,
        BigEndianReader reader,
        FontLimits limits)
    {
        if (table.GetLength() != CffLength)
        {
            throw FontFormatException.Create("The CFF maxp table length is invalid.");
        }

        int glyphCount = ReadGlyphCount(reader, limits);
        return new SfntMaxpTable(
            CffVersion,
            SfntMaxpKind.Cff,
            glyphCount,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
    }

    static SfntMaxpTable ParseTrueType(
        SfntTableData table,
        BigEndianReader reader,
        FontLimits limits)
    {
        if (table.GetLength() != TrueTypeLength)
        {
            throw FontFormatException.Create("The TrueType maxp table length is invalid.");
        }

        int glyphCount = ReadGlyphCount(reader, limits);
        int maxPoints = reader.ReadUInt16();
        int maxContours = reader.ReadUInt16();
        int maxCompositePoints = reader.ReadUInt16();
        int maxCompositeContours = reader.ReadUInt16();
        int maxZones = reader.ReadUInt16();
        int maxTwilightPoints = reader.ReadUInt16();
        int maxStorage = reader.ReadUInt16();
        int maxFunctionDefinitions = reader.ReadUInt16();
        int maxInstructionDefinitions = reader.ReadUInt16();
        int maxStackElements = reader.ReadUInt16();
        int maxInstructionSize = reader.ReadUInt16();
        int maxComponentElements = reader.ReadUInt16();
        int maxComponentDepth = reader.ReadUInt16();

        if (maxZones != 1 && maxZones != 2)
        {
            throw FontFormatException.Create("The maxp maxZones value is invalid.");
        }

        return new SfntMaxpTable(
            TrueTypeVersion,
            SfntMaxpKind.TrueType,
            glyphCount,
            maxPoints,
            maxContours,
            maxCompositePoints,
            maxCompositeContours,
            maxZones,
            maxTwilightPoints,
            maxStorage,
            maxFunctionDefinitions,
            maxInstructionDefinitions,
            maxStackElements,
            maxInstructionSize,
            maxComponentElements,
            maxComponentDepth);
    }

    static int ReadGlyphCount(BigEndianReader reader, FontLimits limits)
    {
        int glyphCount = reader.ReadUInt16();

        if (glyphCount == 0)
        {
            throw FontFormatException.Create("The maxp table declares no glyphs.");
        }

        if (glyphCount > limits.GetMaxGlyphsPerFace())
        {
            throw FontLimitException.Create(
                "The maxp glyph count exceeds the configured per-face limit.");
        }

        return glyphCount;
    }
}
