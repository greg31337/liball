namespace ttf;

/// <summary>Represents embedded-bitmap metrics for one glyph.</summary>
public class ColorBitmapGlyphMetrics : IColorBitmapGlyphMetrics
{
    bool bothDirections;
    bool horizontal;
    int height;
    int width;
    int bearingX;
    int bearingY;
    int advance;
    int verticalBearingX;
    int verticalBearingY;
    int verticalAdvance;

    internal ColorBitmapGlyphMetrics(
        bool bothDirections,
        bool horizontal,
        int height,
        int width,
        int bearingX,
        int bearingY,
        int advance,
        int verticalBearingX,
        int verticalBearingY,
        int verticalAdvance)
    {
        this.bothDirections = bothDirections;
        this.horizontal = horizontal;
        this.height = height;
        this.width = width;
        this.bearingX = bearingX;
        this.bearingY = bearingY;
        this.advance = advance;
        this.verticalBearingX = verticalBearingX;
        this.verticalBearingY = verticalBearingY;
        this.verticalAdvance = verticalAdvance;
    }

    /// <summary>Reports whether both horizontal and vertical metrics are stored.</summary>
    /// <returns>True for big metrics; otherwise false.</returns>
    public bool HasBothDirections() { return bothDirections; }
    /// <summary>Reports whether small metrics describe horizontal layout.</summary>
    /// <returns>True for horizontal or big metrics; otherwise false.</returns>
    public bool IsHorizontal() { return horizontal; }
    /// <summary>Gets the bitmap height in pixels.</summary>
    /// <returns>The height.</returns>
    public int GetHeight() { return height; }
    /// <summary>Gets the bitmap width in pixels.</summary>
    /// <returns>The width.</returns>
    public int GetWidth() { return width; }
    /// <summary>Gets the primary x bearing in pixels.</summary>
    /// <returns>The x bearing.</returns>
    public int GetBearingX() { return bearingX; }
    /// <summary>Gets the primary y bearing in pixels.</summary>
    /// <returns>The y bearing.</returns>
    public int GetBearingY() { return bearingY; }
    /// <summary>Gets the primary advance in pixels.</summary>
    /// <returns>The advance.</returns>
    public int GetAdvance() { return advance; }
    /// <summary>Gets the vertical x bearing.</summary>
    /// <returns>The vertical x bearing.</returns>
    public int GetVerticalBearingX()
    {
        ValidateBothDirections();
        return verticalBearingX;
    }
    /// <summary>Gets the vertical y bearing.</summary>
    /// <returns>The vertical y bearing.</returns>
    public int GetVerticalBearingY()
    {
        ValidateBothDirections();
        return verticalBearingY;
    }
    /// <summary>Gets the vertical advance.</summary>
    /// <returns>The vertical advance.</returns>
    public int GetVerticalAdvance()
    {
        ValidateBothDirections();
        return verticalAdvance;
    }

    void ValidateBothDirections()
    {
        if (!bothDirections)
        {
            throw new System.InvalidOperationException(
                "These bitmap glyph metrics store only one layout direction.");
        }
    }
}
