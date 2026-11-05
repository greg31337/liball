namespace molecule;


/// <summary>
/// Provides coordinate 3d data and operations for molecular graphs.
/// </summary>
public class Coordinate3D
{
    /// <summary>
    /// The x.
    /// </summary>
    public double X;
    /// <summary>
    /// The y.
    /// </summary>
    public double Y;
    /// <summary>
    /// The z.
    /// </summary>
    public double Z;
    /// <summary>
    /// Whether the value is set.
    /// </summary>
    public bool IsSet;

    /// <summary>
    /// Creates an unset coordinate.
    /// </summary>
    /// <returns>An unset coordinate at the origin.</returns>
    internal static Coordinate3D CreateEmpty()
    {
        Coordinate3D coordinate = new Coordinate3D(0.0, 0.0, 0.0);
        coordinate.IsSet = false;
        return coordinate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinate3D"/> class.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="z">The z.</param>
    public Coordinate3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        IsSet = true;
    }

    /// <summary>
    /// Set.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="z">The z.</param>
    internal void Set(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        IsSet = true;
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
        IsSet = false;
    }

    /// <summary>
    /// Converts this value to coordinate 2D.
    /// </summary>
    /// <returns>The resulting coordinate 2D.</returns>
    internal Coordinate2D ToCoordinate2D()
    {
        Coordinate2D coordinate = Coordinate2D.CreateEmpty();
        coordinate.X = X;
        coordinate.Y = Y;
        coordinate.IsSet = IsSet;
        return coordinate;
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting coordinate 3D.</returns>
    internal Coordinate3D Clone()
    {
        Coordinate3D coordinate = Coordinate3D.CreateEmpty();
        coordinate.X = X;
        coordinate.Y = Y;
        coordinate.Z = Z;
        coordinate.IsSet = IsSet;
        return coordinate;
    }
}

