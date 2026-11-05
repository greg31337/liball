namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorPaintGraph"/>.</summary>
public interface IColorPaintGraph
{
    /// <summary>Gets the base glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the source COLR table version.</summary>
    /// <returns>Zero or one.</returns>
    int GetColrVersion();

    /// <summary>Gets the normalized root paint.</summary>
    /// <returns>The root paint.</returns>
    ColorPaint GetRoot();

    /// <summary>Gets the bounded source-node count.</summary>
    /// <returns>The node count.</returns>
    int GetNodeCount();

    /// <summary>Gets the maximum normalized paint depth.</summary>
    /// <returns>The maximum depth including the root.</returns>
    int GetMaximumDepth();

    /// <summary>Gets the clip box resolved for this base glyph.</summary>
    /// <returns>The resolved clip box, or null when no clip applies.</returns>
    ColorGlyphClipBox? GetClipBox();

    /// <summary>Gets the normalized coordinates used to resolve variable paint values.</summary>
    /// <returns>The immutable variation coordinates.</returns>
    NormalizedFontCoordinates GetVariationCoordinates();

    /// <summary>Gets the number of referenced glyph identifiers.</summary>
    /// <returns>The referenced-glyph count.</returns>
    int GetReferencedGlyphCount();

    /// <summary>Gets one referenced glyph identifier.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The referenced glyph identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the graph.</exception>
    int GetReferencedGlyphId(int index);
}
