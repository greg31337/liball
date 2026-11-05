namespace ttf;

/// <summary>Identifies one bit in a PDF FontDescriptor <c>Flags</c> value.</summary>
public enum PdfFontDescriptorFlag
{
    /// <summary>All glyphs have the same horizontal advance.</summary>
    FixedPitch = 1,

    /// <summary>The glyphs use serif forms.</summary>
    Serif = 2,

    /// <summary>Character codes use a symbolic encoding.</summary>
    Symbolic = 4,

    /// <summary>The glyphs resemble cursive handwriting.</summary>
    Script = 8,

    /// <summary>Character codes use a nonsymbolic encoding.</summary>
    Nonsymbolic = 32,

    /// <summary>The dominant vertical strokes are slanted.</summary>
    Italic = 64,

    /// <summary>The font contains no lowercase letters.</summary>
    AllCap = 65536,

    /// <summary>The font contains lowercase small-cap forms.</summary>
    SmallCap = 131072,

    /// <summary>Bold glyphs should remain emphasized at small sizes.</summary>
    ForceBold = 262144
}
