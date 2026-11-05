namespace ttf;

/// <summary>Represents one GDEF ligature caret.</summary>
public class FontGlyphCaret : IFontGlyphCaret
{
    SfntGdefCaretValue caret;

    internal FontGlyphCaret(SfntGdefCaretValue caret) { this.caret = caret; }

    /// <summary>Gets the caret format.</summary><returns>The format.</returns>
    public int GetFormat() { return caret.GetFormat(); }
    /// <summary>Gets the design coordinate.</summary><returns>The coordinate.</returns>
    public int GetCoordinate() { return caret.GetCoordinate(); }
    /// <summary>Reports whether a contour point is used.</summary><returns>True for format 2.</returns>
    public bool HasContourPointIndex() { return caret.HasContourPointIndex(); }
    /// <summary>Gets the contour point.</summary><returns>The point index.</returns>
    /// <exception cref="System.InvalidOperationException">The caret is not format 2.</exception>
    public int GetContourPointIndex()
    {
        if (!caret.HasContourPointIndex())
        {
            throw new System.InvalidOperationException(
                "The caret does not reference a contour point.");
        }
        return caret.GetContourPointIndex();
    }
    /// <summary>Reports whether an adjustment exists.</summary><returns>True when present.</returns>
    public bool HasAdjustment() { return caret.GetAdjustment() != null; }
    /// <summary>Reports whether the adjustment is device data.</summary><returns>True for device data.</returns>
    public bool IsDeviceAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = caret.GetAdjustment();
        return adjustment != null && adjustment.IsDevice();
    }
    /// <summary>Reports whether the adjustment is a variation index.</summary><returns>True for a variation index.</returns>
    public bool IsVariationAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = caret.GetAdjustment();
        return adjustment != null && adjustment.IsVariationIndex();
    }
}
