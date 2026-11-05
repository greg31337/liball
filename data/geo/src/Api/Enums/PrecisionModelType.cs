namespace geo;

/// <summary>Identifies how coordinate precision is represented.</summary>
public enum PrecisionModelType
{
    /// <summary>IEEE 754 double-precision floating point.</summary>
    Floating = 0,

    /// <summary>Coordinates rounded to single-precision floating point.</summary>
    FloatingSingle = 1,

    /// <summary>Coordinates rounded to a fixed grid.</summary>
    Fixed = 2
}
