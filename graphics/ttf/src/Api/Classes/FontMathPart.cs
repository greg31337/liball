namespace ttf;

/// <summary>Represents one MATH glyph assembly part.</summary>
public class FontMathPart : IFontMathPart
{
    SfntMathGlyphPart part;
    internal FontMathPart(SfntMathGlyphPart part) { this.part = part; }
    /// <summary>Gets the part glyph ID.</summary><returns>The glyph ID.</returns>
    public int GetGlyphId() { return part.GetGlyphId(); }
    /// <summary>Gets the start connector length.</summary><returns>The length.</returns>
    public int GetStartConnectorLength() { return part.GetStartConnectorLength(); }
    /// <summary>Gets the end connector length.</summary><returns>The length.</returns>
    public int GetEndConnectorLength() { return part.GetEndConnectorLength(); }
    /// <summary>Gets the full advance.</summary><returns>The advance.</returns>
    public int GetFullAdvance() { return part.GetFullAdvance(); }
    /// <summary>Gets the raw part flags.</summary><returns>The flags.</returns>
    public int GetFlags() { return part.GetFlags(); }
    /// <summary>Reports whether the part is an extender.</summary><returns>True for an extender.</returns>
    public bool IsExtender() { return part.IsExtender(); }
}
