namespace geo;

/// <summary>Stores the homogeneous point components of a multi-point payload.</summary>
public class MultiPoint
{
    /// <summary>The point components, or an empty array when the multi-point is empty.</summary>
    public Point[] Points = [];
}
