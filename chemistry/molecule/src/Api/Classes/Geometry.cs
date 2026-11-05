using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides geometry data and operations for molecular graphs.
/// </summary>
public class Geometry : IGeometry
{
    /// <summary>
    /// Initializes a geometry calculator that operates directly on coordinate values.
    /// </summary>
    public Geometry()
    {
    }

    /// <summary>
    /// Calculates the three-dimensional Euclidean distance between two coordinates.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Distance(Coordinate3D left, Coordinate3D right)
    {
        double dx = right.X - left.X;
        double dy = right.Y - left.Y;
        double dz = right.Z - left.Z;
        return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the two-dimensional Euclidean distance between two coordinates.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Distance2D(Coordinate2D left, Coordinate2D right)
    {
        double dx = right.X - left.X;
        double dy = right.Y - left.Y;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the angle in degrees formed by three coordinates.
    /// </summary>
    /// <param name="first">The first.</param>
    /// <param name="center">The center.</param>
    /// <param name="third">The third.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Angle(Coordinate3D first, Coordinate3D center, Coordinate3D third)
    {
        Coordinate3D left = Subtract(first, center);
        Coordinate3D right = Subtract(third, center);
        double denominator = Length(left) * Length(right);
        if (denominator == 0.0)
        {
            return 0.0;
        }

        double cosine = Clamp(Dot(left, right) / denominator, -1.0, 1.0);
        return RadiansToDegrees(System.Math.Acos(cosine));
    }

    /// <summary>
    /// Calculates the signed dihedral angle in degrees formed by four coordinates.
    /// </summary>
    /// <param name="first">The first.</param>
    /// <param name="second">The second.</param>
    /// <param name="third">The third.</param>
    /// <param name="fourth">The fourth.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Torsion(Coordinate3D first, Coordinate3D second, Coordinate3D third, Coordinate3D fourth)
    {
        Coordinate3D b1 = Subtract(second, first);
        Coordinate3D b2 = Subtract(third, second);
        Coordinate3D b3 = Subtract(fourth, third);
        Coordinate3D n1 = Cross(b1, b2);
        Coordinate3D n2 = Cross(b2, b3);
        Coordinate3D unitB2 = Normalize(b2);
        Coordinate3D m1 = Cross(n1, unitB2);
        double x = Dot(n1, n2);
        double y = Dot(m1, n2);
        if (Length(n1) == 0.0 || Length(n2) == 0.0)
        {
            return 0.0;
        }

        return RadiansToDegrees(System.Math.Atan2(y, x));
    }

    /// <summary>
    /// Calculates the arithmetic center of the assigned coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates.</param>
    /// <returns>The resulting coordinate 3D.</returns>
    public Coordinate3D Centroid(List<Coordinate3D>? coordinates)
    {
        Coordinate3D centroid = Coordinate3D.CreateEmpty();
        if (coordinates == null)
        {
            return centroid;
        }

        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
        int count = 0;
        int index = 0;
        while (index < coordinates.Count)
        {
            if (coordinates[index].IsSet)
            {
                x = x + coordinates[index].X;
                y = y + coordinates[index].Y;
                z = z + coordinates[index].Z;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            return centroid;
        }

        centroid.Set(x / (double)count, y / (double)count, z / (double)count);
        return centroid;
    }

    /// <summary>
    /// Calculates the arithmetic center of the assigned coordinates.
    /// </summary>
    /// <param name="conformer">The conformer.</param>
    /// <returns>The resulting coordinate 3D.</returns>
    public Coordinate3D CentroidFromConformer(Conformer? conformer)
    {
        if (conformer == null)
        {
            return Coordinate3D.CreateEmpty();
        }

        return Centroid(conformer.Coordinates);
    }

    /// <summary>
    /// Calculates the smallest axis-aligned box containing the supplied coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates.</param>
    /// <returns>The resulting bounding box 3D.</returns>
    public BoundingBox3D BoundingBox(List<Coordinate3D>? coordinates)
    {
        BoundingBox3D box = new BoundingBox3D();
        if (coordinates == null)
        {
            return box;
        }

        int index = 0;
        while (index < coordinates.Count)
        {
            if (coordinates[index].IsSet)
            {
                box.Include(coordinates[index]);
            }

            index = index + 1;
        }

        return box;
    }

    /// <summary>
    /// Calculates the smallest axis-aligned box containing the supplied coordinates.
    /// </summary>
    /// <param name="conformer">The conformer.</param>
    /// <returns>The resulting bounding box 3D.</returns>
    public BoundingBox3D BoundingBoxFromConformer(Conformer? conformer)
    {
        if (conformer == null)
        {
            return new BoundingBox3D();
        }

        return BoundingBox(conformer.Coordinates);
    }

    /// <summary>
    /// Calculates root-mean-square distance over coordinates assigned in both conformers.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    public double Rmsd(Conformer? left, Conformer? right)
    {
        if (left == null || right == null)
        {
            return 0.0;
        }

        int maximum = left.Coordinates.Count;
        if (right.Coordinates.Count < maximum)
        {
            maximum = right.Coordinates.Count;
        }

        double sum = 0.0;
        int count = 0;
        int index = 0;
        while (index < maximum)
        {
            Coordinate3D leftCoordinate = left.Coordinates[index];
            Coordinate3D rightCoordinate = right.Coordinates[index];
            if (leftCoordinate.IsSet && rightCoordinate.IsSet)
            {
                double distance = Distance(leftCoordinate, rightCoordinate);
                sum = sum + distance * distance;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == 0)
        {
            return 0.0;
        }

        return System.Math.Sqrt(sum / (double)count);
    }

    internal Coordinate3D Subtract(Coordinate3D left, Coordinate3D right)
    {
        return new Coordinate3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    internal Coordinate3D Cross(Coordinate3D left, Coordinate3D right)
    {
        double x = left.Y * right.Z - left.Z * right.Y;
        double y = left.Z * right.X - left.X * right.Z;
        double z = left.X * right.Y - left.Y * right.X;
        return new Coordinate3D(x, y, z);
    }

    internal double Dot(Coordinate3D left, Coordinate3D right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    internal double Length(Coordinate3D coordinate)
    {
        return System.Math.Sqrt(Dot(coordinate, coordinate));
    }

    internal Coordinate3D Normalize(Coordinate3D coordinate)
    {
        double length = Length(coordinate);
        if (length == 0.0)
        {
            return Coordinate3D.CreateEmpty();
        }

        return new Coordinate3D(coordinate.X / length, coordinate.Y / length, coordinate.Z / length);
    }

    internal double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    internal double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / System.Math.PI;
    }
}

