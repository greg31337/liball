namespace odf;

/// <summary>
/// Identifies a two-dimensional drawing transform operation.
/// </summary>
public enum OdfTransformOperationKind
{
    /// <summary>The transform operation is unrecognized.</summary>
    Unknown = 0,
    /// <summary>A matrix transform.</summary>
    Matrix = 1,
    /// <summary>A translation.</summary>
    Translate = 2,
    /// <summary>A scale transform.</summary>
    Scale = 3,
    /// <summary>A rotation.</summary>
    Rotate = 4,
    /// <summary>A horizontal skew.</summary>
    SkewX = 5,
    /// <summary>A vertical skew.</summary>
    SkewY = 6
}
