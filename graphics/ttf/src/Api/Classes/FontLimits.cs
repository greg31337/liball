namespace ttf;

/// <summary>
/// Defines hard resource limits applied while loading, inspecting, shaping, and
/// writing fonts.
/// </summary>
/// <remarks>
/// Limit instances are immutable. Every limit is positive and no loading mode
/// may bypass the selected limits. The parameterless constructor uses the
/// compatible profile.
/// </remarks>
public class FontLimits : IFontLimits
{
    int maxInputBytes;
    int maxOutputBytes;
    int maxFaces;
    int maxTablesPerFace;
    int maxGlyphsPerFace;
    int maxPointsPerGlyph;
    int maxContoursPerGlyph;
    int maxComponentDepth;
    int maxCffSubroutineDepth;
    int maxLayoutNesting;
    int maxLookupsPerTable;
    int maxLayoutOperations;
    int maxCoverageEntries;
    int maxStringBytes;
    int maxDecompressedBytes;
    int maxDecompressionRatio;
    int maxVariationRegions;
    int maxPaintDepth;
    int maxDiagnostics;

    /// <summary>Creates limits using the compatible profile.</summary>
    public FontLimits()
    {
        maxInputBytes = 268435456;
        maxOutputBytes = 536870912;
        maxFaces = 256;
        maxTablesPerFace = 1024;
        maxGlyphsPerFace = 65535;
        maxPointsPerGlyph = 1048576;
        maxContoursPerGlyph = 32767;
        maxComponentDepth = 64;
        maxCffSubroutineDepth = 10;
        maxLayoutNesting = 32;
        maxLookupsPerTable = 16384;
        maxLayoutOperations = 5000000;
        maxCoverageEntries = 2000000;
        maxStringBytes = 4194304;
        maxDecompressedBytes = 536870912;
        maxDecompressionRatio = 250;
        maxVariationRegions = 65536;
        maxPaintDepth = 128;
        maxDiagnostics = 4096;
    }

    /// <summary>Creates explicitly configured resource limits.</summary>
    /// <param name="maxInputBytes">Maximum bytes accepted from one input.</param>
    /// <param name="maxOutputBytes">Maximum bytes produced by one operation.</param>
    /// <param name="maxFaces">Maximum faces in one package.</param>
    /// <param name="maxTablesPerFace">Maximum table records in one face.</param>
    /// <param name="maxGlyphsPerFace">Maximum glyphs in one face.</param>
    /// <param name="maxPointsPerGlyph">Maximum resolved points in one glyph.</param>
    /// <param name="maxContoursPerGlyph">Maximum contours in one glyph.</param>
    /// <param name="maxComponentDepth">Maximum composite glyph nesting depth.</param>
    /// <param name="maxCffSubroutineDepth">Maximum CFF subroutine call depth.</param>
    /// <param name="maxLayoutNesting">Maximum contextual layout nesting depth.</param>
    /// <param name="maxLookupsPerTable">Maximum lookups in one layout table.</param>
    /// <param name="maxLayoutOperations">Maximum lookup applications per operation.</param>
    /// <param name="maxCoverageEntries">Maximum entries visited in one coverage structure.</param>
    /// <param name="maxStringBytes">Maximum bytes in one decoded font string.</param>
    /// <param name="maxDecompressedBytes">Maximum bytes produced by decompression.</param>
    /// <param name="maxDecompressionRatio">Maximum integral decompressed-to-compressed ratio.</param>
    /// <param name="maxVariationRegions">Maximum variation regions in one store.</param>
    /// <param name="maxPaintDepth">Maximum color paint graph nesting depth.</param>
    /// <param name="maxDiagnostics">Maximum diagnostics retained by one operation.</param>
    /// <returns>A new configured limit set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A supplied limit is not positive.
    /// </exception>
    internal static FontLimits Create(
        int maxInputBytes,
        int maxOutputBytes,
        int maxFaces,
        int maxTablesPerFace,
        int maxGlyphsPerFace,
        int maxPointsPerGlyph,
        int maxContoursPerGlyph,
        int maxComponentDepth,
        int maxCffSubroutineDepth,
        int maxLayoutNesting,
        int maxLookupsPerTable,
        int maxLayoutOperations,
        int maxCoverageEntries,
        int maxStringBytes,
        int maxDecompressedBytes,
        int maxDecompressionRatio,
        int maxVariationRegions,
        int maxPaintDepth,
        int maxDiagnostics)
    {
        ValidatePositive(maxInputBytes, "maxInputBytes");
        ValidatePositive(maxOutputBytes, "maxOutputBytes");
        ValidatePositive(maxFaces, "maxFaces");
        ValidatePositive(maxTablesPerFace, "maxTablesPerFace");
        ValidatePositive(maxGlyphsPerFace, "maxGlyphsPerFace");
        ValidatePositive(maxPointsPerGlyph, "maxPointsPerGlyph");
        ValidatePositive(maxContoursPerGlyph, "maxContoursPerGlyph");
        ValidatePositive(maxComponentDepth, "maxComponentDepth");
        ValidatePositive(maxCffSubroutineDepth, "maxCffSubroutineDepth");
        ValidatePositive(maxLayoutNesting, "maxLayoutNesting");
        ValidatePositive(maxLookupsPerTable, "maxLookupsPerTable");
        ValidatePositive(maxLayoutOperations, "maxLayoutOperations");
        ValidatePositive(maxCoverageEntries, "maxCoverageEntries");
        ValidatePositive(maxStringBytes, "maxStringBytes");
        ValidatePositive(maxDecompressedBytes, "maxDecompressedBytes");
        ValidatePositive(maxDecompressionRatio, "maxDecompressionRatio");
        ValidatePositive(maxVariationRegions, "maxVariationRegions");
        ValidatePositive(maxPaintDepth, "maxPaintDepth");
        ValidatePositive(maxDiagnostics, "maxDiagnostics");

        FontLimits limits = new FontLimits();
        limits.maxInputBytes = maxInputBytes;
        limits.maxOutputBytes = maxOutputBytes;
        limits.maxFaces = maxFaces;
        limits.maxTablesPerFace = maxTablesPerFace;
        limits.maxGlyphsPerFace = maxGlyphsPerFace;
        limits.maxPointsPerGlyph = maxPointsPerGlyph;
        limits.maxContoursPerGlyph = maxContoursPerGlyph;
        limits.maxComponentDepth = maxComponentDepth;
        limits.maxCffSubroutineDepth = maxCffSubroutineDepth;
        limits.maxLayoutNesting = maxLayoutNesting;
        limits.maxLookupsPerTable = maxLookupsPerTable;
        limits.maxLayoutOperations = maxLayoutOperations;
        limits.maxCoverageEntries = maxCoverageEntries;
        limits.maxStringBytes = maxStringBytes;
        limits.maxDecompressedBytes = maxDecompressedBytes;
        limits.maxDecompressionRatio = maxDecompressionRatio;
        limits.maxVariationRegions = maxVariationRegions;
        limits.maxPaintDepth = maxPaintDepth;
        limits.maxDiagnostics = maxDiagnostics;
        return limits;
    }

    /// <summary>Creates limits using the compatible profile.</summary>
    /// <returns>A new compatible-profile limit set.</returns>
    internal static FontLimits Create()
    {
        return new FontLimits();
    }

    /// <summary>Creates conservative limits for strict validation.</summary>
    /// <returns>A new strict-profile limit set.</returns>
    internal static FontLimits CreateStrict()
    {
        return FontLimits.Create(
            67108864,
            134217728,
            32,
            256,
            65535,
            131072,
            32767,
            32,
            10,
            16,
            4096,
            1000000,
            1000000,
            1048576,
            134217728,
            100,
            32768,
            64,
            1024);
    }

    /// <summary>Creates limits for normal production compatibility.</summary>
    /// <returns>A new compatible-profile limit set.</returns>
    internal static FontLimits CreateCompatible()
    {
        return FontLimits.Create();
    }

    /// <summary>Creates limits for bounded forensic and metadata inspection.</summary>
    /// <returns>A new inspect-only-profile limit set.</returns>
    internal static FontLimits CreateInspectOnly()
    {
        return FontLimits.Create(
            268435456,
            67108864,
            1024,
            4096,
            65535,
            131072,
            32767,
            16,
            10,
            16,
            4096,
            250000,
            1000000,
            4194304,
            536870912,
            250,
            32768,
            32,
            16384);
    }

    /// <summary>Gets the maximum input byte count.</summary>
    /// <returns>The maximum input byte count.</returns>
    public int GetMaxInputBytes()
    {
        return maxInputBytes;
    }

    /// <summary>Gets the maximum output byte count.</summary>
    /// <returns>The maximum output byte count.</returns>
    public int GetMaxOutputBytes()
    {
        return maxOutputBytes;
    }

    /// <summary>Gets the maximum face count.</summary>
    /// <returns>The maximum face count.</returns>
    public int GetMaxFaces()
    {
        return maxFaces;
    }

    /// <summary>Gets the maximum table count per face.</summary>
    /// <returns>The maximum table count per face.</returns>
    public int GetMaxTablesPerFace()
    {
        return maxTablesPerFace;
    }

    /// <summary>Gets the maximum glyph count per face.</summary>
    /// <returns>The maximum glyph count per face.</returns>
    public int GetMaxGlyphsPerFace()
    {
        return maxGlyphsPerFace;
    }

    /// <summary>Gets the maximum resolved point count per glyph.</summary>
    /// <returns>The maximum resolved point count per glyph.</returns>
    public int GetMaxPointsPerGlyph()
    {
        return maxPointsPerGlyph;
    }

    /// <summary>Gets the maximum contour count per glyph.</summary>
    /// <returns>The maximum contour count per glyph.</returns>
    public int GetMaxContoursPerGlyph()
    {
        return maxContoursPerGlyph;
    }

    /// <summary>Gets the maximum composite glyph depth.</summary>
    /// <returns>The maximum composite glyph depth.</returns>
    public int GetMaxComponentDepth()
    {
        return maxComponentDepth;
    }

    /// <summary>Gets the maximum CFF subroutine call depth.</summary>
    /// <returns>The maximum CFF subroutine call depth.</returns>
    public int GetMaxCffSubroutineDepth()
    {
        return maxCffSubroutineDepth;
    }

    /// <summary>Gets the maximum contextual layout nesting depth.</summary>
    /// <returns>The maximum contextual layout nesting depth.</returns>
    public int GetMaxLayoutNesting()
    {
        return maxLayoutNesting;
    }

    /// <summary>Gets the maximum lookup count per layout table.</summary>
    /// <returns>The maximum lookup count per layout table.</returns>
    public int GetMaxLookupsPerTable()
    {
        return maxLookupsPerTable;
    }

    /// <summary>Gets the maximum layout operation count.</summary>
    /// <returns>The maximum layout operation count.</returns>
    public int GetMaxLayoutOperations()
    {
        return maxLayoutOperations;
    }

    /// <summary>Gets the maximum coverage entry count.</summary>
    /// <returns>The maximum coverage entry count.</returns>
    public int GetMaxCoverageEntries()
    {
        return maxCoverageEntries;
    }

    /// <summary>Gets the maximum encoded string byte count.</summary>
    /// <returns>The maximum encoded string byte count.</returns>
    public int GetMaxStringBytes()
    {
        return maxStringBytes;
    }

    /// <summary>Gets the maximum decompressed byte count.</summary>
    /// <returns>The maximum decompressed byte count.</returns>
    public int GetMaxDecompressedBytes()
    {
        return maxDecompressedBytes;
    }

    /// <summary>Gets the maximum integral decompression ratio.</summary>
    /// <returns>The maximum integral decompression ratio.</returns>
    public int GetMaxDecompressionRatio()
    {
        return maxDecompressionRatio;
    }

    /// <summary>Gets the maximum variation region count.</summary>
    /// <returns>The maximum variation region count.</returns>
    public int GetMaxVariationRegions()
    {
        return maxVariationRegions;
    }

    /// <summary>Gets the maximum color paint graph depth.</summary>
    /// <returns>The maximum color paint graph depth.</returns>
    public int GetMaxPaintDepth()
    {
        return maxPaintDepth;
    }

    /// <summary>Gets the maximum retained diagnostic count.</summary>
    /// <returns>The maximum retained diagnostic count.</returns>
    public int GetMaxDiagnostics()
    {
        return maxDiagnostics;
    }

    static void ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
