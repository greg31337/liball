namespace svg;

/// <summary>Defines supported SVG and CSS length units.</summary>
public enum SvgLengthUnit
{
    /// <summary>A unitless SVG user-unit value.</summary>
    Number = 0,

    /// <summary>A percentage whose basis is supplied during evaluation.</summary>
    Percentage = 1,

    /// <summary>CSS pixels and SVG user units.</summary>
    Px = 2,

    /// <summary>The current element font size.</summary>
    Em = 3,

    /// <summary>The current font x-height.</summary>
    Ex = 4,

    /// <summary>The current font zero-glyph advance.</summary>
    Ch = 5,

    /// <summary>The root element font size.</summary>
    Rem = 6,

    /// <summary>One percent of the viewport width.</summary>
    Vw = 7,

    /// <summary>One percent of the viewport height.</summary>
    Vh = 8,

    /// <summary>One percent of the smaller viewport dimension.</summary>
    Vmin = 9,

    /// <summary>One percent of the larger viewport dimension.</summary>
    Vmax = 10,

    /// <summary>Centimeters at 96 CSS pixels per inch.</summary>
    Cm = 11,

    /// <summary>Millimeters at 96 CSS pixels per inch.</summary>
    Mm = 12,

    /// <summary>Quarter-millimeters at 96 CSS pixels per inch.</summary>
    Q = 13,

    /// <summary>Inches at 96 CSS pixels per inch.</summary>
    In = 14,

    /// <summary>Points at 72 points per inch.</summary>
    Pt = 15,

    /// <summary>Picas at 6 picas per inch.</summary>
    Pc = 16
}

/// <summary>Defines the reference dimension used to evaluate a percentage.</summary>
public enum SvgLengthPercentageBasis
{
    /// <summary>No percentage basis is available.</summary>
    None = 0,

    /// <summary>The viewport width.</summary>
    ViewportWidth = 1,

    /// <summary>The viewport height.</summary>
    ViewportHeight = 2,

    /// <summary>The normalized viewport diagonal.</summary>
    ViewportDiagonal = 3,

    /// <summary>The current font size.</summary>
    FontSize = 4,

    /// <summary>The root font size.</summary>
    RootFontSize = 5,

    /// <summary>The object bounding-box width.</summary>
    ObjectBoundingBoxWidth = 6,

    /// <summary>The object bounding-box height.</summary>
    ObjectBoundingBoxHeight = 7,

    /// <summary>The normalized object bounding-box diagonal.</summary>
    ObjectBoundingBoxDiagonal = 8
}

/// <summary>Defines stable failures produced while evaluating a typed length.</summary>
public enum SvgLengthEvaluationError
{
    /// <summary>No evaluation failure occurred.</summary>
    None = 0,

    /// <summary>The length or context argument was missing or invalid.</summary>
    InvalidArgument = 1,

    /// <summary>A viewport-relative unit or basis had no viewport context.</summary>
    MissingViewport = 2,

    /// <summary>An em unit or font-size basis had no current font size.</summary>
    MissingFontSize = 3,

    /// <summary>An ex or ch unit had no corresponding font metric.</summary>
    MissingFontMetric = 4,

    /// <summary>A rem unit or root-font basis had no root font size.</summary>
    MissingRootFontSize = 5,

    /// <summary>A percentage was evaluated without a basis.</summary>
    MissingPercentageBasis = 6,

    /// <summary>An object-bounding-box value had no bounding-box context.</summary>
    MissingObjectBoundingBox = 7,

    /// <summary>A required context value was negative or non-finite.</summary>
    InvalidContext = 8,

    /// <summary>The converted user-unit result was not finite.</summary>
    NonFiniteResult = 9,

    /// <summary>The length unit or percentage basis was not recognized.</summary>
    UnsupportedUnitOrBasis = 10
}
