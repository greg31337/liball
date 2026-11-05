namespace ttf;

/// <summary>Defines one MATH glyph variant.</summary>
public interface IFontMathVariant
{
    /// <summary>Gets the variant glyph ID.</summary><returns>The glyph ID.</returns>
    int GetGlyphId();
    /// <summary>Gets the advance measurement.</summary><returns>The measurement.</returns>
    int GetAdvanceMeasurement();
}
