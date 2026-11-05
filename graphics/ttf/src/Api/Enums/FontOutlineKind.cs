namespace ttf;

/// <summary>Identifies the technology that supplies a font's glyph outlines.</summary>
public enum FontOutlineKind
{
    /// <summary>The outline technology has not been identified.</summary>
    Unknown = 0,

    /// <summary>TrueType quadratic outlines stored in <c>glyf</c>.</summary>
    TrueType = 1,

    /// <summary>Compact Font Format 1 cubic outlines stored in <c>CFF </c>.</summary>
    Cff1 = 2,

    /// <summary>Compact Font Format 2 variable cubic outlines stored in <c>CFF2</c>.</summary>
    Cff2 = 3,

    /// <summary>The font supplies bitmap glyphs without supported vector outlines.</summary>
    BitmapOnly = 4
}
