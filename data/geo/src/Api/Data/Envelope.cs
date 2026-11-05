namespace geo;

/// <summary>Stores an axis-aligned planar bounding box.</summary>
public class Envelope
{
    /// <summary>The minimum X ordinate, or no-data for an empty envelope.</summary>
    public double MinX = GeometryConstants.NoDataOrdinate;

    /// <summary>The minimum Y ordinate, or no-data for an empty envelope.</summary>
    public double MinY = GeometryConstants.NoDataOrdinate;

    /// <summary>The maximum X ordinate, or no-data for an empty envelope.</summary>
    public double MaxX = GeometryConstants.NoDataOrdinate;

    /// <summary>The maximum Y ordinate, or no-data for an empty envelope.</summary>
    public double MaxY = GeometryConstants.NoDataOrdinate;
}
