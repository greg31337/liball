namespace svg;

/// <summary>Defines an optional media query modifier.</summary>
public enum SvgMediaQueryModifier
{
    /// <summary>No modifier is present.</summary>
    None = 0,
    /// <summary>The complete query result is negated.</summary>
    Not = 1,
    /// <summary>The legacy compatibility modifier is present without changing evaluation.</summary>
    Only = 2
}

/// <summary>Defines recognized media features.</summary>
public enum SvgMediaFeatureKind
{
    /// <summary>An unknown feature that safely evaluates false.</summary>
    Unknown = 0,
    /// <summary>The viewport width.</summary>
    Width = 1,
    /// <summary>The viewport height.</summary>
    Height = 2,
    /// <summary>The viewport orientation.</summary>
    Orientation = 3,
    /// <summary>The output resolution.</summary>
    Resolution = 4,
    /// <summary>The number of bits per color component.</summary>
    Color = 5,
    /// <summary>The number of monochrome bits per pixel.</summary>
    Monochrome = 6,
    /// <summary>The user's reduced-motion preference.</summary>
    ReducedMotion = 7
}

/// <summary>Defines how a media feature value is compared with the environment.</summary>
public enum SvgMediaComparison
{
    /// <summary>The feature is evaluated in Boolean context.</summary>
    Boolean = 0,
    /// <summary>The environment must equal the authored value.</summary>
    Equal = 1,
    /// <summary>The environment must be greater than or equal to the authored value.</summary>
    MinimumInclusive = 2,
    /// <summary>The environment must be strictly greater than the authored value.</summary>
    MinimumExclusive = 3,
    /// <summary>The environment must be less than or equal to the authored value.</summary>
    MaximumInclusive = 4,
    /// <summary>The environment must be strictly less than the authored value.</summary>
    MaximumExclusive = 5
}
