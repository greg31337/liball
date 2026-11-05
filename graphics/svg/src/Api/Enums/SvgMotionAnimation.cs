namespace svg;

/// <summary>Identifies the geometry source selected for a motion animation.</summary>
public enum SvgMotionPathSourceKind
{
    /// <summary>No motion geometry was selected.</summary>
    None = 0,
    /// <summary>A child <c>mpath</c> reference supplied the geometry.</summary>
    MPath = 1,
    /// <summary>The animateMotion <c>path</c> attribute supplied the geometry.</summary>
    Path = 2,
    /// <summary>Point values supplied the motion positions.</summary>
    Values = 3
}

/// <summary>Identifies how a motion sample is rotated.</summary>
public enum SvgMotionRotateKind
{
    /// <summary>A fixed authored angle is used.</summary>
    Angle = 0,
    /// <summary>The forward path tangent is used.</summary>
    Auto = 1,
    /// <summary>The reverse path tangent is used.</summary>
    AutoReverse = 2
}

/// <summary>Defines stable motion animation failures.</summary>
public enum SvgMotionAnimationError
{
    /// <summary>No motion animation failure occurred.</summary>
    None = 0,
    /// <summary>An argument, option, or animation snapshot was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>No path or point-value motion source was present.</summary>
    MissingPath = 2,
    /// <summary>The inline path data was invalid or exceeded a bound.</summary>
    InvalidPath = 3,
    /// <summary>The child mpath reference was invalid or did not target a path element.</summary>
    InvalidMPath = 4,
    /// <summary>The referenced path geometry or pathLength calibration was invalid.</summary>
    InvalidReferencedPath = 5,
    /// <summary>The rotate value was invalid.</summary>
    InvalidRotate = 6,
    /// <summary>The keyTimes, keySplines, or keyPoints metadata was invalid.</summary>
    InvalidKeys = 7,
    /// <summary>The point-value motion plan was invalid.</summary>
    InvalidValues = 8,
    /// <summary>Path measurement or finite transform composition failed.</summary>
    NonFiniteResult = 9,
    /// <summary>A configured parsing, geometry, or repeat bound was exceeded.</summary>
    LimitExceeded = 10
}
