namespace svg;

/// <summary>Defines restart behavior for SVG animations.</summary>
public enum SvgAnimationRestart
{
    /// <summary>An unknown restart value.</summary>
    Unknown = 0,
    /// <summary>The animation can always restart.</summary>
    Always = 1,
    /// <summary>The animation restarts only while inactive.</summary>
    WhenNotActive = 2,
    /// <summary>The animation never restarts.</summary>
    Never = 3
}

/// <summary>Defines post-active fill behavior for SVG animations.</summary>
public enum SvgAnimationFill
{
    /// <summary>An unknown fill value.</summary>
    Unknown = 0,
    /// <summary>The animation effect is removed.</summary>
    Remove = 1,
    /// <summary>The final animation effect is frozen.</summary>
    Freeze = 2
}

/// <summary>Defines additive SVG animation behavior.</summary>
public enum SvgAnimationAdditive
{
    /// <summary>An unknown additive value.</summary>
    Unknown = 0,
    /// <summary>The animation replaces the underlying value.</summary>
    Replace = 1,
    /// <summary>The animation is added to the underlying value.</summary>
    Sum = 2
}

/// <summary>Defines accumulated SVG animation behavior.</summary>
public enum SvgAnimationAccumulate
{
    /// <summary>An unknown accumulate value.</summary>
    Unknown = 0,
    /// <summary>Repeated values do not accumulate.</summary>
    None = 1,
    /// <summary>Repeated values accumulate by summation.</summary>
    Sum = 2
}

/// <summary>Defines SVG animation interpolation modes.</summary>
public enum SvgAnimationCalcMode
{
    /// <summary>An unknown interpolation mode.</summary>
    Unknown = 0,
    /// <summary>Discrete interpolation.</summary>
    Discrete = 1,
    /// <summary>Linear interpolation.</summary>
    Linear = 2,
    /// <summary>Paced interpolation.</summary>
    Paced = 3,
    /// <summary>Cubic spline interpolation.</summary>
    Spline = 4
}

/// <summary>Defines the targeted attribute namespace for SVG animation.</summary>
public enum SvgAnimationAttributeType
{
    /// <summary>An unknown attribute type.</summary>
    Unknown = 0,
    /// <summary>Automatic CSS or XML selection.</summary>
    Auto = 1,
    /// <summary>A CSS property.</summary>
    Css = 2,
    /// <summary>An XML attribute.</summary>
    Xml = 3
}
