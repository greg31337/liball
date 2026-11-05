namespace ttf;

/// <summary>Represents one bounded, normalized COLR glyph paint graph.</summary>
public class ColorPaintGraph : IColorPaintGraph
{
    int glyphId;
    int colrVersion;
    ColorPaint root;
    int nodeCount;
    int maximumDepth;
    int[] referencedGlyphIds;
    ColorGlyphClipBox? clipBox;
    NormalizedFontCoordinates variationCoordinates;

    internal ColorPaintGraph(
        int glyphId,
        int colrVersion,
        ColorPaint root,
        int nodeCount,
        int maximumDepth,
        int[] referencedGlyphIds,
        ColorGlyphClipBox? clipBox,
        NormalizedFontCoordinates variationCoordinates)
    {
        this.glyphId = glyphId;
        this.colrVersion = colrVersion;
        this.root = root;
        this.nodeCount = nodeCount;
        this.maximumDepth = maximumDepth;
        this.referencedGlyphIds = referencedGlyphIds;
        this.clipBox = clipBox;
        this.variationCoordinates = variationCoordinates;
    }

    /// <summary>Gets the base glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the source COLR table version.</summary>
    /// <returns>Zero or one.</returns>
    public int GetColrVersion() { return colrVersion; }

    /// <summary>Gets the normalized root paint.</summary>
    /// <returns>The root paint.</returns>
    public ColorPaint GetRoot() { return root; }

    /// <summary>Gets the bounded source-node count.</summary>
    /// <returns>The node count.</returns>
    public int GetNodeCount() { return nodeCount; }

    /// <summary>Gets the maximum normalized paint depth.</summary>
    /// <returns>The maximum depth including the root.</returns>
    public int GetMaximumDepth() { return maximumDepth; }

    /// <summary>Gets the clip box resolved for this base glyph.</summary>
    /// <returns>The resolved clip box, or null when no clip applies.</returns>
    public ColorGlyphClipBox? GetClipBox() { return clipBox; }

    /// <summary>Gets the normalized coordinates used to resolve variable paint values.</summary>
    /// <returns>The immutable variation coordinates.</returns>
    public NormalizedFontCoordinates GetVariationCoordinates()
    {
        return variationCoordinates;
    }

    /// <summary>Gets the number of referenced glyph identifiers.</summary>
    /// <returns>The referenced-glyph count.</returns>
    public int GetReferencedGlyphCount() { return referencedGlyphIds.Length; }

    /// <summary>Gets one referenced glyph identifier.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The referenced glyph identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the graph.</exception>
    public int GetReferencedGlyphId(int index)
    {
        if (index < 0 || index >= referencedGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return referencedGlyphIds[index];
    }
}
