namespace ttf;

/// <summary>Represents one MATH glyph variant.</summary>
public class FontMathVariant : IFontMathVariant
{
    SfntMathGlyphVariant variant;
    internal FontMathVariant(SfntMathGlyphVariant variant)
    {
        this.variant = variant;
    }
    /// <summary>Gets the variant glyph ID.</summary><returns>The glyph ID.</returns>
    public int GetGlyphId() { return variant.GetGlyphId(); }
    /// <summary>Gets the advance measurement.</summary><returns>The measurement.</returns>
    public int GetAdvanceMeasurement() { return variant.GetAdvanceMeasurement(); }
}
