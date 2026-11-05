using System;

namespace random;

/// <summary>
/// Defines uniform geometric point, rotation, and matrix sampling operations.
/// </summary>
public interface IGeometrySampler
{
    /// <summary>Returns one point on a line segment.</summary>
    double[] Line(double[] start, double[] end);

    /// <summary>Writes one point on a line segment.</summary>
    void LineInto(double[] start, double[] end, Span<double> destination);

    /// <summary>Fills a point-major batch with line-segment points.</summary>
    void FillLine(double[] start, double[] end, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside an axis-aligned box.</summary>
    double[] Box(double[] minimum, double[] maximum);

    /// <summary>Writes one point inside an axis-aligned box.</summary>
    void BoxInto(double[] minimum, double[] maximum, Span<double> destination);

    /// <summary>Fills a point-major batch with box points.</summary>
    void FillBox(double[] minimum, double[] maximum, int sampleCount, Span<double> destination);

    /// <summary>Returns one point on the unit circle.</summary>
    double[] UnitCircle();

    /// <summary>Writes one point on the unit circle.</summary>
    void UnitCircleInto(Span<double> destination);

    /// <summary>Fills a point-major batch with unit-circle points.</summary>
    void FillUnitCircle(int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside the unit disk.</summary>
    double[] UnitDisk();

    /// <summary>Writes one point inside the unit disk.</summary>
    void UnitDiskInto(Span<double> destination);

    /// <summary>Fills a point-major batch with unit-disk points.</summary>
    void FillUnitDisk(int sampleCount, Span<double> destination);

    /// <summary>Returns one point on the three-dimensional unit sphere.</summary>
    double[] UnitSphere();

    /// <summary>Writes one point on the three-dimensional unit sphere.</summary>
    void UnitSphereInto(Span<double> destination);

    /// <summary>Fills a point-major batch with unit-sphere points.</summary>
    void FillUnitSphere(int sampleCount, Span<double> destination);

    /// <summary>Returns one arbitrary-dimensional unit vector.</summary>
    double[] UnitVector(int dimension);

    /// <summary>Writes one arbitrary-dimensional unit vector.</summary>
    void UnitVectorInto(Span<double> destination);

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit vectors.</summary>
    void FillUnitVector(int dimension, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside an arbitrary-dimensional unit ball.</summary>
    double[] UnitBall(int dimension);

    /// <summary>Writes one point inside an arbitrary-dimensional unit ball.</summary>
    void UnitBallInto(Span<double> destination);

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit-ball points.</summary>
    void FillUnitBall(int dimension, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside a full-dimensional simplex.</summary>
    double[] Simplex(double[] vertices, int dimension);

    /// <summary>Writes one point inside a full-dimensional simplex.</summary>
    void SimplexInto(double[] vertices, int dimension, Span<double> destination);

    /// <summary>Fills a point-major batch with simplex points.</summary>
    void FillSimplex(double[] vertices, int dimension, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside a triangle.</summary>
    double[] Triangle(double[] first, double[] second, double[] third);

    /// <summary>Writes one point inside a triangle.</summary>
    void TriangleInto(double[] first, double[] second, double[] third, Span<double> destination);

    /// <summary>Fills a point-major batch with triangle points.</summary>
    void FillTriangle(double[] first, double[] second, double[] third, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside a tetrahedron.</summary>
    double[] Tetrahedron(double[] first, double[] second, double[] third, double[] fourth);

    /// <summary>Writes one point inside a tetrahedron.</summary>
    void TetrahedronInto(double[] first, double[] second, double[] third, double[] fourth, Span<double> destination);

    /// <summary>Fills a point-major batch with tetrahedron points.</summary>
    void FillTetrahedron(double[] first, double[] second, double[] third, double[] fourth, int sampleCount, Span<double> destination);

    /// <summary>Returns one unit quaternion in x, y, z, w order.</summary>
    double[] Quaternion();

    /// <summary>Writes one unit quaternion in x, y, z, w order.</summary>
    void QuaternionInto(Span<double> destination);

    /// <summary>Fills a quaternion-major batch.</summary>
    void FillQuaternions(int sampleCount, Span<double> destination);

    /// <summary>Returns one row-major three-dimensional rotation matrix.</summary>
    double[] Rotation3D();

    /// <summary>Writes one row-major three-dimensional rotation matrix.</summary>
    void Rotation3DInto(Span<double> destination);

    /// <summary>Fills a matrix-major batch with three-dimensional rotations.</summary>
    void FillRotation3D(int sampleCount, Span<double> destination);

    /// <summary>Returns one row-major Haar orthogonal matrix.</summary>
    double[] OrthogonalMatrix(int dimension);

    /// <summary>Writes one row-major Haar orthogonal matrix.</summary>
    void OrthogonalMatrixInto(int dimension, Span<double> destination);

    /// <summary>Fills a matrix-major batch with Haar orthogonal matrices.</summary>
    void FillOrthogonalMatrices(int dimension, int sampleCount, Span<double> destination);

    /// <summary>Returns one point inside a simple polygon.</summary>
    double[] Polygon(double[] vertices);

    /// <summary>Writes one point inside a simple polygon.</summary>
    void PolygonInto(double[] vertices, Span<double> destination);

    /// <summary>Fills a point-major batch with polygon points.</summary>
    void FillPolygon(double[] vertices, int sampleCount, Span<double> destination);

    /// <summary>Returns deterministic polygon triangulation indexes.</summary>
    int[] PolygonTriangulation(double[] vertices);
}
