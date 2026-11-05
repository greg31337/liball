namespace geo;

/// <summary>Stores X, Y, optional Z, and optional M ordinates.</summary>
public class Coordinate
{
    /// <summary>The X ordinate.</summary>
    public double X;

    /// <summary>The Y ordinate.</summary>
    public double Y;

    /// <summary>The Z ordinate, or <see cref="GeometryConstants.NoDataOrdinate"/> when absent.</summary>
    public double Z = GeometryConstants.NoDataOrdinate;

    /// <summary>The M ordinate, or <see cref="GeometryConstants.NoDataOrdinate"/> when absent.</summary>
    public double M = GeometryConstants.NoDataOrdinate;
}
