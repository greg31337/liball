namespace svg;

/// <summary>Defines supported two-dimensional CSS transform functions.</summary>
public enum SvgCssTransformKind
{
    /// <summary>A six-number affine matrix.</summary>
    Matrix = 0,
    /// <summary>A one- or two-axis length-percentage translation.</summary>
    Translate = 1,
    /// <summary>A horizontal length-percentage translation.</summary>
    TranslateX = 2,
    /// <summary>A vertical length-percentage translation.</summary>
    TranslateY = 3,
    /// <summary>A one- or two-axis numeric scale.</summary>
    Scale = 4,
    /// <summary>A horizontal numeric scale.</summary>
    ScaleX = 5,
    /// <summary>A vertical numeric scale.</summary>
    ScaleY = 6,
    /// <summary>An angle rotation.</summary>
    Rotate = 7,
    /// <summary>A one- or two-angle skew.</summary>
    Skew = 8,
    /// <summary>A horizontal angle skew.</summary>
    SkewX = 9,
    /// <summary>A vertical angle skew.</summary>
    SkewY = 10
}

/// <summary>Defines the reference box selected by transform-box.</summary>
public enum SvgTransformBox
{
    /// <summary>The CSS content box.</summary>
    ContentBox = 0,
    /// <summary>The CSS border box.</summary>
    BorderBox = 1,
    /// <summary>The SVG object bounding box.</summary>
    FillBox = 2,
    /// <summary>The SVG stroke bounding box.</summary>
    StrokeBox = 3,
    /// <summary>The nearest SVG viewport or view box.</summary>
    ViewBox = 4
}

/// <summary>Defines stable CSS transform matrix-evaluation failures.</summary>
public enum SvgCssTransformError
{
    /// <summary>No evaluation failure occurred.</summary>
    None = 0,
    /// <summary>A value, collection, enum, or context state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The selected transform reference box was unavailable.</summary>
    MissingReferenceBox = 2,
    /// <summary>A relative length required unavailable viewport or font context.</summary>
    MissingLengthContext = 3,
    /// <summary>A length or angle unit was unsupported for the requested component.</summary>
    UnsupportedUnit = 4,
    /// <summary>A computed matrix component was not finite.</summary>
    NonFiniteResult = 5
}
