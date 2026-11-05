namespace svg;

/// <summary>Identifies a requested or declared font style.</summary>
public enum SvgFontStyle
{
    /// <summary>Derives a face style from OpenType metadata.</summary>
    Auto = 0,
    /// <summary>Selects an upright face.</summary>
    Normal = 1,
    /// <summary>Selects an italic face.</summary>
    Italic = 2,
    /// <summary>Selects an oblique face.</summary>
    Oblique = 3
}

/// <summary>Identifies a CSS generic font family.</summary>
public enum SvgGenericFontFamily
{
    /// <summary>No generic family.</summary>
    None = 0,
    /// <summary>The CSS <c>serif</c> family.</summary>
    Serif = 1,
    /// <summary>The CSS <c>sans-serif</c> family.</summary>
    SansSerif = 2,
    /// <summary>The CSS <c>monospace</c> family.</summary>
    Monospace = 3,
    /// <summary>The CSS <c>cursive</c> family.</summary>
    Cursive = 4,
    /// <summary>The CSS <c>fantasy</c> family.</summary>
    Fantasy = 5,
    /// <summary>The CSS <c>system-ui</c> family.</summary>
    SystemUi = 6,
    /// <summary>The CSS <c>emoji</c> family.</summary>
    Emoji = 7,
    /// <summary>The CSS <c>math</c> family.</summary>
    Math = 8,
    /// <summary>The CSS <c>fangsong</c> family.</summary>
    FangSong = 9,
    /// <summary>The CSS <c>ui-serif</c> family.</summary>
    UiSerif = 10,
    /// <summary>The CSS <c>ui-sans-serif</c> family.</summary>
    UiSansSerif = 11,
    /// <summary>The CSS <c>ui-monospace</c> family.</summary>
    UiMonospace = 12,
    /// <summary>The CSS <c>ui-rounded</c> family.</summary>
    UiRounded = 13
}

/// <summary>Identifies how a font face entered a database.</summary>
public enum SvgFontFaceOrigin
{
    /// <summary>The face was parsed from caller-provided bytes.</summary>
    CallerBytes = 0,
    /// <summary>The face was parsed from a caller-resolved font-face resource.</summary>
    ResolvedFontFace = 1
}

/// <summary>Identifies one declared font source kind.</summary>
public enum SvgFontSourceKind
{
    /// <summary>The source is an in-memory caller byte snapshot.</summary>
    Bytes = 0,
    /// <summary>The source is a caller-resolved URL.</summary>
    Url = 1,
    /// <summary>The source names a caller-known local font.</summary>
    Local = 2
}

/// <summary>Defines stable font-database operation failures.</summary>
public enum SvgFontDatabaseError
{
    /// <summary>No database failure occurred.</summary>
    None = 0,
    /// <summary>An argument or descriptor was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The font database count or byte budget was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>The supplied font could not be parsed.</summary>
    FontReadFailed = 3,
    /// <summary>The resolved resource was unavailable or unsuccessful.</summary>
    ResourceFailed = 4,
    /// <summary>No matching face was available.</summary>
    NoMatch = 5
}
