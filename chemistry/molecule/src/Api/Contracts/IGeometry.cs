using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Geometry"/>.
/// </summary>
public interface IGeometry
{
    /// <summary>
    /// Calculates the three-dimensional Euclidean distance between two coordinates.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Distance(Coordinate3D left, Coordinate3D right);

    /// <summary>
    /// Calculates the two-dimensional Euclidean distance between two coordinates.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Distance2D(Coordinate2D left, Coordinate2D right);

    /// <summary>
    /// Calculates the angle in degrees formed by three coordinates.
    /// </summary>
    /// <param name="first">The first.</param>
    /// <param name="center">The center.</param>
    /// <param name="third">The third.</param>
    /// <returns>The calculated numeric value.</returns>
    double Angle(Coordinate3D first, Coordinate3D center, Coordinate3D third);

    /// <summary>
    /// Calculates the signed dihedral angle in degrees formed by four coordinates.
    /// </summary>
    /// <param name="first">The first.</param>
    /// <param name="second">The second.</param>
    /// <param name="third">The third.</param>
    /// <param name="fourth">The fourth.</param>
    /// <returns>The calculated numeric value.</returns>
    double Torsion(Coordinate3D first, Coordinate3D second, Coordinate3D third, Coordinate3D fourth);

    /// <summary>
    /// Calculates the arithmetic center of the assigned coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates.</param>
    /// <returns>The resulting coordinate 3D.</returns>
    Coordinate3D Centroid(List<Coordinate3D>? coordinates);

    /// <summary>
    /// Calculates the arithmetic center of the assigned coordinates.
    /// </summary>
    /// <param name="conformer">The conformer.</param>
    /// <returns>The resulting coordinate 3D.</returns>
    Coordinate3D CentroidFromConformer(Conformer? conformer);

    /// <summary>
    /// Calculates the smallest axis-aligned box containing the supplied coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates.</param>
    /// <returns>The resulting bounding box 3D.</returns>
    BoundingBox3D BoundingBox(List<Coordinate3D>? coordinates);

    /// <summary>
    /// Calculates the smallest axis-aligned box containing the supplied coordinates.
    /// </summary>
    /// <param name="conformer">The conformer.</param>
    /// <returns>The resulting bounding box 3D.</returns>
    BoundingBox3D BoundingBoxFromConformer(Conformer? conformer);

    /// <summary>
    /// Calculates root-mean-square distance over coordinates assigned in both conformers.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns>The calculated numeric value.</returns>
    double Rmsd(Conformer? left, Conformer? right);
}

