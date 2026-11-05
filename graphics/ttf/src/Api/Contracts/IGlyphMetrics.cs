namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="GlyphMetrics"/>.</summary>
public interface IGlyphMetrics
{
    /// <summary>Gets the glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the horizontal advance.</summary>
    /// <returns>The advance in design units.</returns>
    int GetAdvanceWidth();

    /// <summary>Gets the left side bearing.</summary>
    /// <returns>The bearing in design units.</returns>
    int GetLeftSideBearing();

    /// <summary>Gets the vertical advance.</summary>
    /// <returns>The advance in design units.</returns>
    int GetAdvanceHeight();

    /// <summary>Gets the top side bearing.</summary>
    /// <returns>The bearing in design units.</returns>
    int GetTopSideBearing();

    /// <summary>Gets the resolved vertical-origin y coordinate.</summary>
    /// <returns>The y coordinate in design units.</returns>
    int GetVerticalOriginY();

    /// <summary>Reports whether the vertical advance was synthesized as one em.</summary>
    /// <returns>True when vhea and vmtx were absent.</returns>
    bool UsesSynthesizedVerticalAdvance();

    /// <summary>Reports whether VORG supplied the vertical-origin y coordinate.</summary>
    /// <returns>True when VORG was used.</returns>
    bool UsesVorgOrigin();

    /// <summary>Scales the glyph metrics to a point size.</summary>
    /// <param name="unitsPerEm">The positive design-unit scale.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>A scaled immutable result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is negative or non-finite.</exception>
    /// <exception cref="FontFormatException"><c>unitsPerEm</c> is not positive.</exception>
    ScaledGlyphMetrics Scale(int unitsPerEm, double pointSize);
}
