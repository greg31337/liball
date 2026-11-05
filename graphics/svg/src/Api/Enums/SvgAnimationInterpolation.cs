namespace svg;

/// <summary>Identifies a typed SVG animation value family.</summary>
public enum SvgAnimationValueKind
{
    /// <summary>A finite scalar number.</summary>
    Number = 0,
    /// <summary>A typed SVG length or percentage.</summary>
    Length = 1,
    /// <summary>A CSS color.</summary>
    Color = 2,
    /// <summary>Normalized SVG path data.</summary>
    Path = 3,
    /// <summary>An ordered SVG point list.</summary>
    Points = 4,
    /// <summary>An SVG transform list.</summary>
    Transform = 5,
    /// <summary>A four-component SVG view box.</summary>
    ViewBox = 6,
    /// <summary>An SVG paint value.</summary>
    Paint = 7,
    /// <summary>An arbitrary discrete string.</summary>
    String = 8
}

/// <summary>Defines stable typed animation parsing and interpolation failures.</summary>
public enum SvgAnimationInterpolationError
{
    /// <summary>No interpolation failure occurred.</summary>
    None = 0,
    /// <summary>An argument, option, enum, or mutable value was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Authored text could not be parsed as the requested value family.</summary>
    InvalidValue = 2,
    /// <summary>The two values have incompatible types, units, commands, or list shapes.</summary>
    IncompatibleValues = 3,
    /// <summary>The requested mode cannot interpolate the supplied values.</summary>
    UnsupportedInterpolation = 4,
    /// <summary>A configured item, command, or token bound was exceeded.</summary>
    LimitExceeded = 5,
    /// <summary>Interpolation or distance arithmetic produced a non-finite value.</summary>
    NonFiniteResult = 6,
    /// <summary>A required spline count did not match the selected value sequence.</summary>
    InvalidSplines = 7
}
