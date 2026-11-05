namespace geo;

/// <summary>Identifies the topological dimension of a geometry or result.</summary>
public enum TopologicalDimension
{
    /// <summary>No dimension because the value is empty.</summary>
    Empty = -1,

    /// <summary>Zero-dimensional point topology.</summary>
    Point = 0,

    /// <summary>One-dimensional curve topology.</summary>
    Curve = 1,

    /// <summary>Two-dimensional surface topology.</summary>
    Surface = 2
}
