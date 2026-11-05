namespace geo;

/// <summary>Identifies the ordinates carried by a coordinate sequence.</summary>
public enum CoordinateLayout
{
    /// <summary>Two-dimensional X and Y coordinates.</summary>
    XY = 0,

    /// <summary>X, Y, and elevation coordinates.</summary>
    XYZ = 1,

    /// <summary>X, Y, and measure coordinates.</summary>
    XYM = 2,

    /// <summary>X, Y, elevation, and measure coordinates.</summary>
    XYZM = 3
}
