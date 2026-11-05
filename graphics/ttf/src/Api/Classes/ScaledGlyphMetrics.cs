namespace ttf;

/// <summary>Contains horizontal and vertical metrics for one glyph at a point size.</summary>
public class ScaledGlyphMetrics : IScaledGlyphMetrics
{
    GlyphMetrics designMetrics;
    int unitsPerEm;
    double pointSize;

    internal ScaledGlyphMetrics(GlyphMetrics metrics, int unitsPerEm, double pointSize)
    {
        CheckedFontMath.ScaleDesignUnits(0, unitsPerEm, pointSize);
        this.designMetrics = metrics;
        this.unitsPerEm = unitsPerEm;
        this.pointSize = pointSize;
    }

    /// <summary>Gets the source design metrics.</summary>
    /// <returns>The design-unit result.</returns>
    public GlyphMetrics GetDesignMetrics() { return designMetrics; }

    /// <summary>Gets the target point size.</summary>
    /// <returns>The point size.</returns>
    public double GetPointSize() { return pointSize; }

    /// <summary>Gets the scaled horizontal advance.</summary>
    /// <returns>The advance.</returns>
    public double GetAdvanceWidth() { return Scale(designMetrics.GetAdvanceWidth()); }

    /// <summary>Gets the scaled left side bearing.</summary>
    /// <returns>The bearing.</returns>
    public double GetLeftSideBearing() { return Scale(designMetrics.GetLeftSideBearing()); }

    /// <summary>Gets the scaled vertical advance.</summary>
    /// <returns>The advance.</returns>
    public double GetAdvanceHeight() { return Scale(designMetrics.GetAdvanceHeight()); }

    /// <summary>Gets the scaled top side bearing.</summary>
    /// <returns>The bearing.</returns>
    public double GetTopSideBearing() { return Scale(designMetrics.GetTopSideBearing()); }

    /// <summary>Gets the scaled vertical-origin y coordinate.</summary>
    /// <returns>The y coordinate.</returns>
    public double GetVerticalOriginY() { return Scale(designMetrics.GetVerticalOriginY()); }

    double Scale(int value)
    {
        return CheckedFontMath.ScaleDesignUnits(value, unitsPerEm, pointSize);
    }
}
