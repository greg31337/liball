namespace molecule;


/// <summary>
/// Provides coordinate 2d data and operations for molecular graphs.
/// </summary>
public class Coordinate2D
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
    /// Whether the value is set.
    /// </summary>
    public bool IsSet;

    /// <summary>
    /// Creates an unset coordinate.
    /// </summary>
    /// <returns>An unset coordinate at the origin.</returns>
    internal static Coordinate2D CreateEmpty()
    {
        Coordinate2D coordinate = new Coordinate2D(0.0, 0.0);
        coordinate.IsSet = false;
        return coordinate;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinate2D"/> class.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public Coordinate2D(double x, double y)
    {
        X = x;
        Y = y;
        IsSet = true;
    }

    /// <summary>
    /// Set.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    internal void Set(double x, double y)
    {
        X = x;
        Y = y;
        IsSet = true;
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        X = 0.0;
        Y = 0.0;
        IsSet = false;
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting coordinate 2D.</returns>
    internal Coordinate2D Clone()
    {
        Coordinate2D coordinate = Coordinate2D.CreateEmpty();
        coordinate.X = X;
        coordinate.Y = Y;
        coordinate.IsSet = IsSet;
        return coordinate;
    }
}

