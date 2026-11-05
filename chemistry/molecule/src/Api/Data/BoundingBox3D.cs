using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides bounding box 3d data and operations for molecular graphs.
/// </summary>
public class BoundingBox3D
{
    /// <summary>
    /// The minimum.
    /// </summary>
    public Coordinate3D Minimum;
    /// <summary>
    /// The maximum.
    /// </summary>
    public Coordinate3D Maximum;
    /// <summary>
    /// Whether the value is set.
    /// </summary>
    public bool IsSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundingBox3D"/> class.
    /// </summary>
    public BoundingBox3D()
    {
        Minimum = Coordinate3D.CreateEmpty();
        Maximum = Coordinate3D.CreateEmpty();
        IsSet = false;
    }

    /// <summary>
    /// Expands the bounding box to include the supplied coordinate.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    internal void Include(Coordinate3D? coordinate)
    {
        if (coordinate == null || !coordinate.IsSet)
        {
            return;
        }

        if (!IsSet)
        {
            Minimum = coordinate.Clone();
            Maximum = coordinate.Clone();
            IsSet = true;
            return;
        }

        if (coordinate.X < Minimum.X)
        {
            Minimum.X = coordinate.X;
        }

        if (coordinate.Y < Minimum.Y)
        {
            Minimum.Y = coordinate.Y;
        }

        if (coordinate.Z < Minimum.Z)
        {
            Minimum.Z = coordinate.Z;
        }

        if (coordinate.X > Maximum.X)
        {
            Maximum.X = coordinate.X;
        }

        if (coordinate.Y > Maximum.Y)
        {
            Maximum.Y = coordinate.Y;
        }

        if (coordinate.Z > Maximum.Z)
        {
            Maximum.Z = coordinate.Z;
        }
    }

    /// <summary>
    /// Returns the bounding box width along the x axis.
    /// </summary>
    /// <returns>The calculated numeric value.</returns>
    internal double Width()
    {
        if (!IsSet)
        {
            return 0.0;
        }

        return Maximum.X - Minimum.X;
    }

    /// <summary>
    /// Returns the bounding box height along the y axis.
    /// </summary>
    /// <returns>The calculated numeric value.</returns>
    internal double Height()
    {
        if (!IsSet)
        {
            return 0.0;
        }

        return Maximum.Y - Minimum.Y;
    }

    /// <summary>
    /// Returns the bounding box depth along the z axis.
    /// </summary>
    /// <returns>The calculated numeric value.</returns>
    internal double Depth()
    {
        if (!IsSet)
        {
            return 0.0;
        }

        return Maximum.Z - Minimum.Z;
    }
}

