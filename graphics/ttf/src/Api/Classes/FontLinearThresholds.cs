namespace ttf;

/// <summary>Contains legacy LTSH linear-scaling thresholds.</summary>
public class FontLinearThresholds : IFontLinearThresholds
{
    SfntLtshTable table;

    internal FontLinearThresholds(SfntLtshTable table)
    {
        this.table = table;
    }

    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    public int GetVersion() { return table.GetVersion(); }

    /// <summary>Gets the number of glyph thresholds.</summary>
    /// <returns>The glyph count.</returns>
    public int GetGlyphCount() { return table.GetGlyphCount(); }

    /// <summary>Gets one glyph's linear-scaling threshold.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The pixels-per-em threshold.</returns>
    public int GetLinearThreshold(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return table.GetLinearThreshold(glyphId);
    }

    /// <summary>Reports whether linear scaling applies at a pixels-per-em size.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="pixelsPerEm">The nonnegative pixels-per-em size.</param>
    /// <returns>True when the size is at or above the threshold.</returns>
    public bool IsLinearAtPixelsPerEm(int glyphId, int pixelsPerEm)
    {
        ValidateGlyphId(glyphId);

        if (pixelsPerEm < 0)
        {
            throw new System.ArgumentOutOfRangeException("pixelsPerEm");
        }

        return table.IsLinearAtPpem(glyphId, pixelsPerEm);
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= table.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}
