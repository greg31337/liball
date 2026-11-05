namespace svg;

/// <summary>Defines alignment of each independently positioned SVG text chunk.</summary>
public enum SvgTextAnchor
{
    /// <summary>Place the chunk start at its current text position.</summary>
    Start = 0,
    /// <summary>Center the chunk on its current text position.</summary>
    Middle = 1,
    /// <summary>Place the chunk end at its current text position.</summary>
    End = 2
}

/// <summary>Defines the dominant baseline used by an SVG text run.</summary>
public enum SvgDominantBaseline
{
    /// <summary>Use the alphabetic baseline.</summary>
    Auto = 0,
    /// <summary>Use the alphabetic baseline.</summary>
    Alphabetic = 1,
    /// <summary>Use the ideographic baseline.</summary>
    Ideographic = 2,
    /// <summary>Use the hanging baseline.</summary>
    Hanging = 3,
    /// <summary>Use the mathematical baseline.</summary>
    Mathematical = 4,
    /// <summary>Use the center of the em box.</summary>
    Central = 5,
    /// <summary>Use the center of the x-height.</summary>
    Middle = 6,
    /// <summary>Use the text-over edge.</summary>
    TextTop = 7,
    /// <summary>Use the text-under edge.</summary>
    TextBottom = 8
}

/// <summary>Defines alignment of a run baseline with its dominant baseline.</summary>
public enum SvgAlignmentBaseline
{
    /// <summary>Use the dominant baseline.</summary>
    Auto = 0,
    /// <summary>Use the dominant baseline.</summary>
    Baseline = 1,
    /// <summary>Use the before edge of the em box.</summary>
    BeforeEdge = 2,
    /// <summary>Use the text-before edge.</summary>
    TextBeforeEdge = 3,
    /// <summary>Use the center of the x-height.</summary>
    Middle = 4,
    /// <summary>Use the center of the em box.</summary>
    Central = 5,
    /// <summary>Use the after edge of the em box.</summary>
    AfterEdge = 6,
    /// <summary>Use the text-after edge.</summary>
    TextAfterEdge = 7,
    /// <summary>Use the ideographic baseline.</summary>
    Ideographic = 8,
    /// <summary>Use the alphabetic baseline.</summary>
    Alphabetic = 9,
    /// <summary>Use the hanging baseline.</summary>
    Hanging = 10,
    /// <summary>Use the mathematical baseline.</summary>
    Mathematical = 11,
    /// <summary>Use the top edge.</summary>
    Top = 12,
    /// <summary>Use the center of the em box.</summary>
    Center = 13,
    /// <summary>Use the bottom edge.</summary>
    Bottom = 14
}

/// <summary>Defines how an authored baseline shift is resolved.</summary>
public enum SvgBaselineShiftKind
{
    /// <summary>Do not shift the baseline.</summary>
    Baseline = 0,
    /// <summary>Use the conventional subscript shift.</summary>
    Sub = 1,
    /// <summary>Use the conventional superscript shift.</summary>
    Super = 2,
    /// <summary>Use the explicit user-unit shift.</summary>
    Length = 3
}

/// <summary>Defines how an SVG textLength value changes laid-out text.</summary>
public enum SvgTextLengthAdjust
{
    /// <summary>Adjust only spacing between addressable characters.</summary>
    Spacing = 0,
    /// <summary>Scale glyph advances, glyphs, positioning deltas, and spacing.</summary>
    SpacingAndGlyphs = 1
}

/// <summary>Defines stable SVG text-positioning failures.</summary>
public enum SvgTextLayoutError
{
    /// <summary>No layout failure occurred.</summary>
    None = 0,
    /// <summary>The shaping result or positioning options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured glyph, range, or character bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Text positioning was cancelled.</summary>
    Cancelled = 3
}
