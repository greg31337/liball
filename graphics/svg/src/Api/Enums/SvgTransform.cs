namespace svg;

/// <summary>Defines the supported SVG transform functions.</summary>
public enum SvgTransformKind
{
    /// <summary>A six-component affine matrix.</summary>
    Matrix = 0,

    /// <summary>A one- or two-axis translation.</summary>
    Translate = 1,

    /// <summary>A one- or two-axis scale.</summary>
    Scale = 2,

    /// <summary>A rotation about the origin or an explicit center.</summary>
    Rotate = 3,

    /// <summary>A horizontal skew.</summary>
    SkewX = 4,

    /// <summary>A vertical skew.</summary>
    SkewY = 5
}

/// <summary>Defines stable transform-to-matrix failures.</summary>
public enum SvgTransformMatrixError
{
    /// <summary>No matrix construction failure occurred.</summary>
    None = 0,

    /// <summary>A transform, list, kind, parameter, or collection state was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>A computed matrix component was not finite.</summary>
    NonFiniteResult = 2
}
