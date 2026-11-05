namespace svg;

/// <summary>Defines whether stroke metrics scale with the geometry transform.</summary>
public enum SvgStrokeVectorEffect
{
    /// <summary>Constructs the stroke in path user coordinates before transformation.</summary>
    None = 0,
    /// <summary>Constructs the stroke after transforming the centerline, preserving output-space width.</summary>
    NonScalingStroke = 1
}
