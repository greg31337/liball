namespace ttf;

/// <summary>Identifies shaping state retained on one output glyph.</summary>
public enum ShapedGlyphFlags
{
    /// <summary>No special shaping state is present.</summary>
    None = 0,

    /// <summary>A line break before this glyph may change shaping.</summary>
    UnsafeToBreak = 1,

    /// <summary>The source glyph is default ignorable.</summary>
    DefaultIgnorable = 2,

    /// <summary>The glyph represents multiple ligature components.</summary>
    Ligature = 4,

    /// <summary>The glyph is positioned by mark attachment.</summary>
    MarkAttachment = 8,

    /// <summary>The glyph is positioned by cursive attachment.</summary>
    CursiveAttachment = 16
}
