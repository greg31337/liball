namespace ttf;

/// <summary>Defines embedded-bitmap metrics for one glyph.</summary>
public interface IColorBitmapGlyphMetrics
{
    /// <summary>Reports whether both horizontal and vertical metrics are stored.</summary>
    /// <returns>True for big metrics; otherwise false.</returns>
    bool HasBothDirections();
    /// <summary>Reports whether small metrics describe horizontal layout.</summary>
    /// <returns>True for horizontal or big metrics; otherwise false.</returns>
    bool IsHorizontal();
    /// <summary>Gets the bitmap height in pixels.</summary>
    /// <returns>The height.</returns>
    int GetHeight();
    /// <summary>Gets the bitmap width in pixels.</summary>
    /// <returns>The width.</returns>
    int GetWidth();
    /// <summary>Gets the primary x bearing in pixels.</summary>
    /// <returns>The x bearing.</returns>
    int GetBearingX();
    /// <summary>Gets the primary y bearing in pixels.</summary>
    /// <returns>The y bearing.</returns>
    int GetBearingY();
    /// <summary>Gets the primary advance in pixels.</summary>
    /// <returns>The advance.</returns>
    int GetAdvance();
    /// <summary>Gets the vertical x bearing.</summary>
    /// <returns>The vertical x bearing.</returns>
    int GetVerticalBearingX();
    /// <summary>Gets the vertical y bearing.</summary>
    /// <returns>The vertical y bearing.</returns>
    int GetVerticalBearingY();
    /// <summary>Gets the vertical advance.</summary>
    /// <returns>The vertical advance.</returns>
    int GetVerticalAdvance();
}
