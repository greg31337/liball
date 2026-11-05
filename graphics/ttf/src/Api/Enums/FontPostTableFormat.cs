namespace ttf;

/// <summary>Identifies a supported OpenType <c>post</c> glyph-name format.</summary>
public enum FontPostTableFormat
{
    /// <summary>No supported <c>post</c> table is available.</summary>
    Unavailable = 0,

    /// <summary>Version 1 supplies the standard 258-name order.</summary>
    Version1 = 1,

    /// <summary>Version 2 supplies standard and custom name indexes.</summary>
    Version2 = 2,

    /// <summary>Deprecated version 2.5 supplies standard-name offsets.</summary>
    Version25 = 3,

    /// <summary>Version 3 intentionally supplies no glyph names.</summary>
    Version3 = 4,

    /// <summary>Apple version 4 supplies character-code-derived names.</summary>
    Version4 = 5
}
