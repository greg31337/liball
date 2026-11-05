namespace svg;

/// <summary>Defines stroke endpoint cap geometry.</summary>
public enum SvgStrokeLineCap
{
    /// <summary>Stops the stroke at the endpoint.</summary>
    Butt = 0,
    /// <summary>Adds a semicircular endpoint cap.</summary>
    Round = 1,
    /// <summary>Adds a half-width square endpoint extension.</summary>
    Square = 2
}

/// <summary>Defines stroke corner join geometry.</summary>
public enum SvgStrokeLineJoin
{
    /// <summary>Uses a miter up to the miter limit and bevels longer miters.</summary>
    Miter = 0,
    /// <summary>Clips a miter at the configured miter limit.</summary>
    MiterClip = 1,
    /// <summary>Uses a circular outer corner.</summary>
    Round = 2,
    /// <summary>Connects outer corners directly.</summary>
    Bevel = 3,
    /// <summary>Uses a bounded curved outer corner.</summary>
    Arcs = 4
}

/// <summary>Defines stable stroke-outline failures.</summary>
public enum SvgStrokeError
{
    /// <summary>No stroke failure occurred.</summary>
    None = 0,
    /// <summary>The path, style, enum, dash pattern, or limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Adaptive flattening exceeded its segment limit.</summary>
    SegmentLimitExceeded = 4,
    /// <summary>Adaptive flattening exceeded its subdivision limit.</summary>
    SubdivisionLimitExceeded = 5,
    /// <summary>Dash placement exceeded its configured fragment limit.</summary>
    DashLimitExceeded = 6,
    /// <summary>Outline construction exceeded its output-command limit.</summary>
    OutputLimitExceeded = 7
}
