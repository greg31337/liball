namespace svg;

/// <summary>Defines the value validation used by a supports-query property entry.</summary>
public enum SvgPropertyValueSupportKind
{
    /// <summary>Any syntactically valid nonempty CSS component value is accepted.</summary>
    Any = 0,
    /// <summary>Only one of the entry's keywords is accepted.</summary>
    Keyword = 1,
    /// <summary>A supported CSS color is accepted.</summary>
    Color = 2,
    /// <summary>A supported SVG paint value is accepted.</summary>
    Paint = 3,
    /// <summary>A supported CSS length or percentage is accepted.</summary>
    Length = 4,
    /// <summary>A finite CSS number is accepted.</summary>
    Number = 5,
    /// <summary>A supported transform list or the none keyword is accepted.</summary>
    Transform = 6
}

/// <summary>Defines the external quantity used to resolve a property's percentages.</summary>
public enum SvgPropertyPercentageBasis
{
    /// <summary>The property grammar does not accept a percentage.</summary>
    None = 0,
    /// <summary>Percentages normalize to the unit numeric interval.</summary>
    Number = 1,
    /// <summary>Percentages resolve against the inherited font size.</summary>
    FontSize = 2,
    /// <summary>Percentages resolve against the applicable line height.</summary>
    LineHeight = 3,
    /// <summary>Percentages resolve against the current viewport width.</summary>
    ViewportWidth = 4,
    /// <summary>Percentages resolve against the current viewport height.</summary>
    ViewportHeight = 5,
    /// <summary>Percentages resolve against the normalized viewport diagonal.</summary>
    ViewportSize = 6,
    /// <summary>Percentages resolve against the applicable object bounding box.</summary>
    ObjectBoundingBox = 7
}

/// <summary>Defines the interpolation category recorded for a property.</summary>
public enum SvgPropertyAnimationType
{
    /// <summary>The property is not animatable.</summary>
    NotAnimatable = 0,
    /// <summary>Values switch discretely.</summary>
    Discrete = 1,
    /// <summary>Compatible computed values use their value-specific interpolation.</summary>
    ComputedValue = 2,
    /// <summary>Numeric values interpolate as finite numbers.</summary>
    Number = 3,
    /// <summary>Length and percentage values interpolate after unit resolution.</summary>
    Length = 4,
    /// <summary>Colors interpolate in the selected color space.</summary>
    Color = 5,
    /// <summary>Paint values interpolate when their paint kinds are compatible.</summary>
    Paint = 6,
    /// <summary>Transform lists use transform interpolation.</summary>
    Transform = 7,
    /// <summary>Compatible list items interpolate pairwise.</summary>
    List = 8
}
