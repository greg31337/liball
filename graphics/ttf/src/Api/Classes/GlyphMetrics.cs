namespace ttf;

/// <summary>Contains horizontal and vertical metrics for one glyph in design units.</summary>
public class GlyphMetrics : IGlyphMetrics
{
    int glyphId;
    int advanceWidth;
    int leftSideBearing;
    int advanceHeight;
    int topSideBearing;
    int verticalOriginY;
    bool synthesizedVerticalAdvance;
    bool usesVorgOrigin;

    internal GlyphMetrics(
        int glyphId,
        int advanceWidth,
        int leftSideBearing,
        int advanceHeight,
        int topSideBearing,
        int verticalOriginY,
        bool synthesizedVerticalAdvance,
        bool usesVorgOrigin)
    {
        this.glyphId = glyphId;
        this.advanceWidth = advanceWidth;
        this.leftSideBearing = leftSideBearing;
        this.advanceHeight = advanceHeight;
        this.topSideBearing = topSideBearing;
        this.verticalOriginY = verticalOriginY;
        this.synthesizedVerticalAdvance = synthesizedVerticalAdvance;
        this.usesVorgOrigin = usesVorgOrigin;
    }

    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the horizontal advance.</summary>
    /// <returns>The advance in design units.</returns>
    public int GetAdvanceWidth() { return advanceWidth; }

    /// <summary>Gets the left side bearing.</summary>
    /// <returns>The bearing in design units.</returns>
    public int GetLeftSideBearing() { return leftSideBearing; }

    /// <summary>Gets the vertical advance.</summary>
    /// <returns>The advance in design units.</returns>
    public int GetAdvanceHeight() { return advanceHeight; }

    /// <summary>Gets the top side bearing.</summary>
    /// <returns>The bearing in design units.</returns>
    public int GetTopSideBearing() { return topSideBearing; }

    /// <summary>Gets the resolved vertical-origin y coordinate.</summary>
    /// <returns>The y coordinate in design units.</returns>
    public int GetVerticalOriginY() { return verticalOriginY; }

    /// <summary>Reports whether the vertical advance was synthesized as one em.</summary>
    /// <returns>True when vhea and vmtx were absent.</returns>
    public bool UsesSynthesizedVerticalAdvance() { return synthesizedVerticalAdvance; }

    /// <summary>Reports whether VORG supplied the vertical-origin y coordinate.</summary>
    /// <returns>True when VORG was used.</returns>
    public bool UsesVorgOrigin() { return usesVorgOrigin; }

    /// <summary>Scales the glyph metrics to a point size.</summary>
    /// <param name="unitsPerEm">The positive design-unit scale.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A scaled immutable result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is negative or non-finite.</exception>
    /// <exception cref="FontFormatException"><c>unitsPerEm</c> is not positive.</exception>
    public ScaledGlyphMetrics Scale(int unitsPerEm, double pointSize)
    {
        return new ScaledGlyphMetrics(this, unitsPerEm, pointSize);
    }
}
