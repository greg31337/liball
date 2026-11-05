namespace ttf;

/// <summary>Defines inspection of the legacy LTSH table.</summary>
public interface IFontLinearThresholds
{
    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    int GetVersion();

    /// <summary>Gets the number of glyph thresholds.</summary>
    /// <returns>The glyph count.</returns>
    int GetGlyphCount();

    /// <summary>Gets one glyph's linear-scaling threshold.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The pixels-per-em threshold.</returns>
    int GetLinearThreshold(int glyphId);

    /// <summary>Reports whether linear scaling applies at a pixels-per-em size.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="pixelsPerEm">The nonnegative pixels-per-em size.</param>
    /// <returns>True when the size is at or above the threshold.</returns>
    bool IsLinearAtPixelsPerEm(int glyphId, int pixelsPerEm);
}
