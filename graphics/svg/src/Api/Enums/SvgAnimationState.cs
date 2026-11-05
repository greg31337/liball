namespace svg;

/// <summary>Defines reduced-motion behavior for caller-controlled animation sampling.</summary>
public enum SvgAnimationReducedMotionBehavior
{
    /// <summary>Animation sampling is unchanged.</summary>
    Preserve = 0,
    /// <summary>Animation effects are disabled and base values are returned.</summary>
    Disable = 1
}

/// <summary>Defines stable animation-state failures.</summary>
public enum SvgAnimationStateError
{
    /// <summary>No animation-state failure occurred.</summary>
    None = 0,
    /// <summary>An argument, option, or retained state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>An active animation did not have a compatible registered base value.</summary>
    MissingBaseValue = 2,
    /// <summary>An SMIL value or motion plan could not be resolved.</summary>
    InvalidSmilAnimation = 3,
    /// <summary>A registered CSS animation plan could not be sampled.</summary>
    InvalidCssAnimation = 4,
    /// <summary>The animation timeline failed or exceeded a bound.</summary>
    TimelineFailure = 5,
    /// <summary>The retained source or scene snapshot became stale.</summary>
    StaleSnapshot = 6
}
