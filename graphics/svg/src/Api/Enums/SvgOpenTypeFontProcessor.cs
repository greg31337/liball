namespace svg;

/// <summary>Defines stable variable and color-font processing outcomes.</summary>
public enum SvgOpenTypeInstanceStatus
{
    /// <summary>Processing completed without delegation.</summary>
    Success = 0,
    /// <summary>The arguments or requested coordinates were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The retained font table data was malformed.</summary>
    InvalidFont = 2,
    /// <summary>A configured axis, glyph, point, palette, or layer limit was exceeded.</summary>
    LimitExceeded = 3,
    /// <summary>Processing was cancelled.</summary>
    Cancelled = 4,
    /// <summary>A valid table, structure, or operator requires a caller implementation.</summary>
    CallbackRequired = 5
}

/// <summary>Identifies the supported normalized color-glyph paint operation.</summary>
public enum SvgOpenTypeColorPaintKind
{
    /// <summary>Paints one glyph outline with one CPAL color.</summary>
    SolidGlyph = 0
}
