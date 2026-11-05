namespace svg;

/// <summary>Defines every command in SVG path data.</summary>
public enum SvgPathCommandKind
{
    /// <summary>An unknown command kind.</summary>
    Unknown = 0,
    /// <summary>A moveto command.</summary>
    MoveTo = 1,
    /// <summary>A closepath command.</summary>
    ClosePath = 2,
    /// <summary>A lineto command.</summary>
    LineTo = 3,
    /// <summary>A horizontal lineto command.</summary>
    HorizontalLineTo = 4,
    /// <summary>A vertical lineto command.</summary>
    VerticalLineTo = 5,
    /// <summary>A cubic Bézier command.</summary>
    CubicCurveTo = 6,
    /// <summary>A smooth cubic Bézier command.</summary>
    SmoothCubicCurveTo = 7,
    /// <summary>A quadratic Bézier command.</summary>
    QuadraticCurveTo = 8,
    /// <summary>A smooth quadratic Bézier command.</summary>
    SmoothQuadraticCurveTo = 9,
    /// <summary>An elliptical arc command.</summary>
    ArcTo = 10
}

/// <summary>Defines stable SVG path parsing failures.</summary>
public enum SvgPathParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The first valid command was required to be a moveto.</summary>
    ExpectedMoveTo = 2,
    /// <summary>An unrecognized command letter was encountered.</summary>
    UnknownCommand = 3,
    /// <summary>A command or parameter was required.</summary>
    ExpectedParameter = 4,
    /// <summary>A numeric parameter was malformed or non-finite.</summary>
    InvalidNumber = 5,
    /// <summary>A numeric token exceeded its configured bound.</summary>
    TokenTooLong = 6,
    /// <summary>An arc flag was not exactly zero or one.</summary>
    InvalidArcFlag = 7,
    /// <summary>An elliptical arc radius was negative.</summary>
    NegativeArcRadius = 8,
    /// <summary>Unexpected data occurred between commands or parameter groups.</summary>
    UnexpectedData = 9,
    /// <summary>The configured path segment count was exceeded.</summary>
    SegmentLimitExceeded = 10
}
