namespace svg;

/// <summary>Defines Unicode bidirectional character classes used during resolution.</summary>
public enum SvgBidiClass
{
    /// <summary>Left-to-right strong text.</summary>
    LeftToRight = 0,
    /// <summary>Right-to-left strong text.</summary>
    RightToLeft = 1,
    /// <summary>Arabic right-to-left strong text.</summary>
    ArabicLetter = 2,
    /// <summary>European number.</summary>
    EuropeanNumber = 3,
    /// <summary>European number separator.</summary>
    EuropeanSeparator = 4,
    /// <summary>European number terminator.</summary>
    EuropeanTerminator = 5,
    /// <summary>Arabic number.</summary>
    ArabicNumber = 6,
    /// <summary>Common number separator.</summary>
    CommonSeparator = 7,
    /// <summary>Nonspacing mark.</summary>
    NonspacingMark = 8,
    /// <summary>Boundary neutral.</summary>
    BoundaryNeutral = 9,
    /// <summary>Paragraph separator.</summary>
    ParagraphSeparator = 10,
    /// <summary>Segment separator.</summary>
    SegmentSeparator = 11,
    /// <summary>Whitespace.</summary>
    Whitespace = 12,
    /// <summary>Other neutral.</summary>
    OtherNeutral = 13,
    /// <summary>Left-to-right embedding control.</summary>
    LeftToRightEmbedding = 14,
    /// <summary>Right-to-left embedding control.</summary>
    RightToLeftEmbedding = 15,
    /// <summary>Left-to-right override control.</summary>
    LeftToRightOverride = 16,
    /// <summary>Right-to-left override control.</summary>
    RightToLeftOverride = 17,
    /// <summary>Pop directional formatting control.</summary>
    PopDirectionalFormat = 18,
    /// <summary>Left-to-right isolate control.</summary>
    LeftToRightIsolate = 19,
    /// <summary>Right-to-left isolate control.</summary>
    RightToLeftIsolate = 20,
    /// <summary>First-strong isolate control.</summary>
    FirstStrongIsolate = 21,
    /// <summary>Pop directional isolate control.</summary>
    PopDirectionalIsolate = 22
}

/// <summary>Defines how a paragraph embedding level is selected.</summary>
public enum SvgBidiDirection
{
    /// <summary>Use the first strong character outside isolate contents.</summary>
    Auto = 0,
    /// <summary>Force a left-to-right paragraph level.</summary>
    LeftToRight = 1,
    /// <summary>Force a right-to-left paragraph level.</summary>
    RightToLeft = 2
}

/// <summary>Defines stable bidirectional analysis failures.</summary>
public enum SvgBidiError
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>The input or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured input bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Bidirectional analysis was cancelled.</summary>
    Cancelled = 3
}
