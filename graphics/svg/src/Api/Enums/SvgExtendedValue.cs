namespace svg;

/// <summary>Identifies standalone typed value grammars exposed by <see cref="SvgValueService"/>.</summary>
public enum SvgExtendedValueKind
{
    /// <summary>An SVG length or percentage.</summary>
    Length = 0,
    /// <summary>An angle.</summary>
    Angle = 1,
    /// <summary>A time.</summary>
    Time = 2,
    /// <summary>A frequency.</summary>
    Frequency = 3,
    /// <summary>A resolution.</summary>
    Resolution = 4,
    /// <summary>A positive ratio.</summary>
    Ratio = 5,
    /// <summary>An SVG pathLength value.</summary>
    PathLength = 6,
    /// <summary>An SVG viewBox.</summary>
    ViewBox = 7,
    /// <summary>An SVG preserveAspectRatio value.</summary>
    PreserveAspectRatio = 8,
    /// <summary>An SVG transform list.</summary>
    SvgTransform = 9,
    /// <summary>A CSS transform list.</summary>
    CssTransform = 10,
    /// <summary>A CSS transform-origin value.</summary>
    TransformOrigin = 11,
    /// <summary>A CSS transform-box keyword.</summary>
    TransformBox = 12,
    /// <summary>A CSS color.</summary>
    Color = 13,
    /// <summary>An SVG paint value.</summary>
    Paint = 14,
    /// <summary>A typed CSS calc, min, max, or clamp expression.</summary>
    Calc = 15,
    /// <summary>An SVG stroke dash array.</summary>
    DashArray = 16,
    /// <summary>An SVG text-positioning length list.</summary>
    TextPositionList = 17,
    /// <summary>An SVG text rotate list.</summary>
    RotateList = 18,
    /// <summary>A CSS font-family list.</summary>
    FontFamilyList = 19,
    /// <summary>An SVG paint-order component list.</summary>
    PaintOrder = 20
}

/// <summary>Defines stable failures shared by extended standalone value parsing.</summary>
public enum SvgExtendedValueError
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>An input, option, bound, enum, or mutable value was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A required value or component was absent.</summary>
    ExpectedValue = 2,
    /// <summary>A numeric component was malformed or non-finite.</summary>
    InvalidNumber = 3,
    /// <summary>A unit or keyword was unknown or disallowed.</summary>
    InvalidUnitOrKeyword = 4,
    /// <summary>The parsed value was outside the grammar's permitted range.</summary>
    OutOfRange = 5,
    /// <summary>A token exceeded its configured character bound.</summary>
    TokenTooLong = 6,
    /// <summary>Trailing characters followed a complete value.</summary>
    TrailingCharacters = 7,
    /// <summary>The grammar was malformed.</summary>
    InvalidSyntax = 8,
    /// <summary>An item, node, transform, or fallback limit was exceeded.</summary>
    LimitExceeded = 9
}

/// <summary>Defines stable failures produced while evaluating CSS math.</summary>
public enum SvgCalcEvaluationError
{
    /// <summary>No evaluation failure occurred.</summary>
    None = 0,
    /// <summary>The expression, context, basis, or mutable node state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A required viewport, font, or percentage context was unavailable.</summary>
    MissingContext = 2,
    /// <summary>A division used a zero divisor.</summary>
    DivisionByZero = 3,
    /// <summary>An operation combined incompatible evaluated dimensions.</summary>
    IncompatibleDimensions = 4,
    /// <summary>An evaluated result was not finite.</summary>
    NonFiniteResult = 5
}
