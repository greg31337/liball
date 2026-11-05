namespace ttf;

/// <summary>Controls assignment of glyph identifiers in a subset.</summary>
public enum FontSubsetGlyphRetentionMode
{
    /// <summary>Assign dense new glyph identifiers in deterministic order.</summary>
    Compact = 0,

    /// <summary>Keep original glyph identifiers, including gaps.</summary>
    Preserve = 1
}
