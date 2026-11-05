namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontLimits"/>.</summary>
public interface IFontLimits
{
    /// <summary>Gets the maximum input byte count.</summary>
    /// <returns>The maximum input byte count.</returns>
    int GetMaxInputBytes();

    /// <summary>Gets the maximum output byte count.</summary>
    /// <returns>The maximum output byte count.</returns>
    int GetMaxOutputBytes();

    /// <summary>Gets the maximum face count.</summary>
    /// <returns>The maximum face count.</returns>
    int GetMaxFaces();

    /// <summary>Gets the maximum table count per face.</summary>
    /// <returns>The maximum table count per face.</returns>
    int GetMaxTablesPerFace();

    /// <summary>Gets the maximum glyph count per face.</summary>
    /// <returns>The maximum glyph count per face.</returns>
    int GetMaxGlyphsPerFace();

    /// <summary>Gets the maximum resolved point count per glyph.</summary>
    /// <returns>The maximum resolved point count per glyph.</returns>
    int GetMaxPointsPerGlyph();

    /// <summary>Gets the maximum contour count per glyph.</summary>
    /// <returns>The maximum contour count per glyph.</returns>
    int GetMaxContoursPerGlyph();

    /// <summary>Gets the maximum composite glyph depth.</summary>
    /// <returns>The maximum composite glyph depth.</returns>
    int GetMaxComponentDepth();

    /// <summary>Gets the maximum CFF subroutine call depth.</summary>
    /// <returns>The maximum CFF subroutine call depth.</returns>
    int GetMaxCffSubroutineDepth();

    /// <summary>Gets the maximum contextual layout nesting depth.</summary>
    /// <returns>The maximum contextual layout nesting depth.</returns>
    int GetMaxLayoutNesting();

    /// <summary>Gets the maximum lookup count per layout table.</summary>
    /// <returns>The maximum lookup count per layout table.</returns>
    int GetMaxLookupsPerTable();

    /// <summary>Gets the maximum layout operation count.</summary>
    /// <returns>The maximum layout operation count.</returns>
    int GetMaxLayoutOperations();

    /// <summary>Gets the maximum coverage entry count.</summary>
    /// <returns>The maximum coverage entry count.</returns>
    int GetMaxCoverageEntries();

    /// <summary>Gets the maximum encoded string byte count.</summary>
    /// <returns>The maximum encoded string byte count.</returns>
    int GetMaxStringBytes();

    /// <summary>Gets the maximum decompressed byte count.</summary>
    /// <returns>The maximum decompressed byte count.</returns>
    int GetMaxDecompressedBytes();

    /// <summary>Gets the maximum integral decompression ratio.</summary>
    /// <returns>The maximum integral decompression ratio.</returns>
    int GetMaxDecompressionRatio();

    /// <summary>Gets the maximum variation region count.</summary>
    /// <returns>The maximum variation region count.</returns>
    int GetMaxVariationRegions();

    /// <summary>Gets the maximum color paint graph depth.</summary>
    /// <returns>The maximum color paint graph depth.</returns>
    int GetMaxPaintDepth();

    /// <summary>Gets the maximum retained diagnostic count.</summary>
    /// <returns>The maximum retained diagnostic count.</returns>
    int GetMaxDiagnostics();
}
