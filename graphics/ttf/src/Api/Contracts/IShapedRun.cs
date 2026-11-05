namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ShapedRun"/>.</summary>
public interface IShapedRun
{
    /// <summary>Gets the visual glyph count.</summary><returns>The glyph count.</returns>
    int GetGlyphCount();
    /// <summary>Gets one visual-order glyph.</summary><param name="visualIndex">The visual index.</param><returns>The glyph.</returns>
    ShapedGlyph GetGlyph(int visualIndex);
    /// <summary>Gets one logical-order glyph.</summary><param name="logicalIndex">The logical index.</param><returns>The glyph.</returns>
    ShapedGlyph GetLogicalGlyph(int logicalIndex);
    /// <summary>Maps a logical glyph index to visual order.</summary><param name="logicalIndex">The logical index.</param><returns>The visual index.</returns>
    int GetVisualGlyphIndex(int logicalIndex);
    /// <summary>Maps a visual glyph index to logical order.</summary><param name="visualIndex">The visual index.</param><returns>The logical index.</returns>
    int GetLogicalGlyphIndex(int visualIndex);
    /// <summary>Gets the logical cluster count.</summary><returns>The cluster count.</returns>
    int GetClusterCount();
    /// <summary>Gets one logical-order cluster.</summary><param name="logicalIndex">The logical index.</param><returns>The cluster.</returns>
    ShapedCluster GetCluster(int logicalIndex);
    /// <summary>Gets one visual-order cluster.</summary><param name="visualIndex">The visual index.</param><returns>The cluster.</returns>
    ShapedCluster GetVisualCluster(int visualIndex);
    /// <summary>Maps a logical cluster index to visual order.</summary><param name="logicalIndex">The logical index.</param><returns>The visual index.</returns>
    int GetVisualClusterIndex(int logicalIndex);
    /// <summary>Maps a visual cluster index to logical order.</summary><param name="visualIndex">The visual index.</param><returns>The logical index.</returns>
    int GetLogicalClusterIndex(int visualIndex);
    /// <summary>Gets the complete source UTF-16 length.</summary><returns>The code-unit length.</returns>
    int GetSourceLength();
    /// <summary>Gets the resolved run direction.</summary><returns>The direction.</returns>
    ShapingDirection GetDirection();
    /// <summary>Gets total horizontal advance.</summary><returns>The design-unit advance.</returns>
    double GetTotalXAdvance();
    /// <summary>Gets total vertical advance.</summary><returns>The design-unit advance.</returns>
    double GetTotalYAdvance();
    /// <summary>Reports whether breaking at a UTF-16 position may change shaping.</summary><param name="utf16Index">The absolute boundary position.</param><returns>True when unsafe.</returns>
    bool IsUnsafeToBreakAtUtf16Index(int utf16Index);
}
