namespace svg;

/// <summary>Defines CSS animation planning and sampling failures.</summary>
public enum SvgCssAnimationError
{
    /// <summary>No CSS animation failure occurred.</summary>
    None = 0,
    /// <summary>An argument, option, or mutable plan was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>An animation longhand list was invalid.</summary>
    InvalidAnimation = 2,
    /// <summary>A named keyframes rule was unavailable or invalid.</summary>
    MissingKeyframes = 3,
    /// <summary>A keyframe property value could not be parsed.</summary>
    InvalidKeyframeValue = 4,
    /// <summary>An easing function was invalid or unsupported.</summary>
    InvalidEasing = 5,
    /// <summary>An underlying value required by an implicit endpoint was absent.</summary>
    MissingUnderlyingValue = 6,
    /// <summary>Typed keyframe values were incompatible.</summary>
    IncompatibleValues = 7,
    /// <summary>A configured animation, keyframe, property, or token bound was exceeded.</summary>
    LimitExceeded = 8
}

/// <summary>Defines CSS animation playback direction.</summary>
public enum SvgCssAnimationDirection
{
    /// <summary>Every iteration runs from zero to one.</summary>
    Normal = 0,
    /// <summary>Every iteration runs from one to zero.</summary>
    Reverse = 1,
    /// <summary>Odd iterations run in reverse.</summary>
    Alternate = 2,
    /// <summary>Even iterations run in reverse.</summary>
    AlternateReverse = 3
}

/// <summary>Defines CSS animation fill behavior outside its active interval.</summary>
public enum SvgCssAnimationFillMode
{
    /// <summary>No effect applies outside the active interval.</summary>
    None = 0,
    /// <summary>The final effect remains after the active interval.</summary>
    Forwards = 1,
    /// <summary>The initial effect applies during the delay.</summary>
    Backwards = 2,
    /// <summary>Both backwards and forwards filling apply.</summary>
    Both = 3
}

/// <summary>Defines CSS animation play state.</summary>
public enum SvgCssAnimationPlayState
{
    /// <summary>The caller clock advances the animation.</summary>
    Running = 0,
    /// <summary>The animation is held at its initial active time.</summary>
    Paused = 1
}

/// <summary>Identifies a sampled CSS animation interval phase.</summary>
public enum SvgCssAnimationPhase
{
    /// <summary>The animation contributes no effect.</summary>
    Idle = 0,
    /// <summary>The animation is before its active interval.</summary>
    Before = 1,
    /// <summary>The animation is in its active interval.</summary>
    Active = 2,
    /// <summary>The animation is after its active interval.</summary>
    After = 3
}

/// <summary>Identifies a parsed CSS easing algorithm.</summary>
public enum SvgCssEasingKind
{
    /// <summary>Identity easing.</summary>
    Linear = 0,
    /// <summary>A cubic Bézier timing curve.</summary>
    CubicBezier = 1,
    /// <summary>A stepped timing curve.</summary>
    Steps = 2
}
