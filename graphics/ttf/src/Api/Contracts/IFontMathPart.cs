namespace ttf;

/// <summary>Defines one MATH glyph assembly part.</summary>
public interface IFontMathPart
{
    /// <summary>Gets the part glyph ID.</summary><returns>The glyph ID.</returns>
    int GetGlyphId();
    /// <summary>Gets the start connector length.</summary><returns>The length.</returns>
    int GetStartConnectorLength();
    /// <summary>Gets the end connector length.</summary><returns>The length.</returns>
    int GetEndConnectorLength();
    /// <summary>Gets the full advance.</summary><returns>The advance.</returns>
    int GetFullAdvance();
    /// <summary>Gets the raw part flags.</summary><returns>The flags.</returns>
    int GetFlags();
    /// <summary>Reports whether the part is an extender.</summary><returns>True for an extender.</returns>
    bool IsExtender();
}
