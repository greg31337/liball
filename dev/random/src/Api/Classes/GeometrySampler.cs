using System;

namespace random;

/// <summary>
/// Provides uniform geometric point, rotation, and matrix sampling operations.
/// </summary>
/// <remarks>
/// Batch output is sample-major; each point, quaternion, or row-major matrix immediately
/// follows the previous sample. Batch results and generator state match repeated destination
/// calls. This class retains the supplied generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class GeometrySampler : IGeometrySampler
{
    RandomGenerator generator;

    /// <summary>Creates a geometry sampler backed by a deterministic generator.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public GeometrySampler(RandomGenerator generator)
    {
        Guard.NotNull(generator, "generator");
        this.generator = generator;
    }

    /// <summary>Returns one uniform point on a line segment.</summary>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <returns>A point with the endpoint dimension.</returns>
    public double[] Line(double[] start, double[] end)
    {
        return RandomGeometry.Line(generator, start, end);
    }

    /// <summary>Writes one uniform point on a line segment.</summary>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <param name="destination">The exact-length point destination.</param>
    public void LineInto(double[] start, double[] end, Span<double> destination)
    {
        RandomGeometry.Line(generator, start, end, destination);
    }

    /// <summary>Fills a point-major batch with line-segment points.</summary>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillLine(double[] start, double[] end, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillLine(generator, start, end, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside an axis-aligned box.</summary>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <returns>A point with the bounds dimension.</returns>
    public double[] Box(double[] minimum, double[] maximum)
    {
        return RandomGeometry.Box(generator, minimum, maximum);
    }

    /// <summary>Writes one uniform point inside an axis-aligned box.</summary>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <param name="destination">The exact-length point destination.</param>
    public void BoxInto(double[] minimum, double[] maximum, Span<double> destination)
    {
        RandomGeometry.Box(generator, minimum, maximum, destination);
    }

    /// <summary>Fills a point-major batch with axis-aligned-box points.</summary>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillBox(double[] minimum, double[] maximum, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillBox(generator, minimum, maximum, sampleCount, destination);
    }

    /// <summary>Returns one uniform point on the unit circle.</summary>
    /// <returns>Two coordinates.</returns>
    public double[] UnitCircle()
    {
        return RandomGeometry.UnitCircle(generator);
    }

    /// <summary>Writes one uniform point on the unit circle.</summary>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    public void UnitCircleInto(Span<double> destination)
    {
        RandomGeometry.UnitCircle(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-circle points.</summary>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the point count.</param>
    public void FillUnitCircle(int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillUnitCircle(generator, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside the unit disk.</summary>
    /// <returns>Two coordinates.</returns>
    public double[] UnitDisk()
    {
        return RandomGeometry.UnitDisk(generator);
    }

    /// <summary>Writes one uniform point inside the unit disk.</summary>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    public void UnitDiskInto(Span<double> destination)
    {
        RandomGeometry.UnitDisk(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-disk points.</summary>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the point count.</param>
    public void FillUnitDisk(int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillUnitDisk(generator, sampleCount, destination);
    }

    /// <summary>Returns one uniform point on the three-dimensional unit sphere.</summary>
    /// <returns>Three coordinates.</returns>
    public double[] UnitSphere()
    {
        return RandomGeometry.UnitSphere(generator);
    }

    /// <summary>Writes one uniform point on the three-dimensional unit sphere.</summary>
    /// <param name="destination">The destination of exactly three coordinates.</param>
    public void UnitSphereInto(Span<double> destination)
    {
        RandomGeometry.UnitSphere(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-sphere points.</summary>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly three times the point count.</param>
    public void FillUnitSphere(int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillUnitSphere(generator, sampleCount, destination);
    }

    /// <summary>Returns one uniform unit vector in an arbitrary dimension.</summary>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <returns>A vector whose Euclidean norm is one within rounding.</returns>
    public double[] UnitVector(int dimension)
    {
        return RandomGeometry.UnitVector(generator, dimension);
    }

    /// <summary>Writes one uniform unit vector.</summary>
    /// <param name="destination">The nonempty destination whose length defines the dimension.</param>
    public void UnitVectorInto(Span<double> destination)
    {
        RandomGeometry.UnitVectorInto(generator, destination);
    }

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit vectors.</summary>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <param name="sampleCount">The nonnegative vector count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillUnitVector(int dimension, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillUnitVector(generator, dimension, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside an arbitrary-dimensional unit ball.</summary>
    /// <param name="dimension">The positive point dimension.</param>
    /// <returns>A point with Euclidean norm at most one.</returns>
    public double[] UnitBall(int dimension)
    {
        return RandomGeometry.UnitBall(generator, dimension);
    }

    /// <summary>Writes one uniform point inside a unit ball.</summary>
    /// <param name="destination">The nonempty destination whose length defines the dimension.</param>
    public void UnitBallInto(Span<double> destination)
    {
        RandomGeometry.UnitBall(generator, destination);
    }

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit-ball points.</summary>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillUnitBall(int dimension, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillUnitBall(generator, dimension, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside a full-dimensional simplex.</summary>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <returns>A point with the requested dimension.</returns>
    public double[] Simplex(double[] vertices, int dimension)
    {
        return RandomGeometry.Simplex(generator, vertices, dimension);
    }

    /// <summary>Writes one uniform point inside a full-dimensional simplex.</summary>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="destination">The exact-length point destination.</param>
    public void SimplexInto(double[] vertices, int dimension, Span<double> destination)
    {
        RandomGeometry.Simplex(generator, vertices, dimension, destination);
    }

    /// <summary>Fills a point-major batch with uniform simplex points.</summary>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillSimplex(double[] vertices, int dimension, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillSimplex(generator, vertices, dimension, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside a triangle.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <returns>A point with the vertex dimension.</returns>
    public double[] Triangle(double[] first, double[] second, double[] third)
    {
        return RandomGeometry.Triangle(generator, first, second, third);
    }

    /// <summary>Writes one uniform point inside a triangle.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="destination">The exact-length point destination.</param>
    public void TriangleInto(double[] first, double[] second, double[] third, Span<double> destination)
    {
        RandomGeometry.Triangle(generator, first, second, third, destination);
    }

    /// <summary>Fills a point-major batch with uniform triangle points.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillTriangle(double[] first, double[] second, double[] third, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillTriangle(generator, first, second, third, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside a tetrahedron.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <returns>A point with the vertex dimension.</returns>
    public double[] Tetrahedron(double[] first, double[] second, double[] third, double[] fourth)
    {
        return RandomGeometry.Tetrahedron(generator, first, second, third, fourth);
    }

    /// <summary>Writes one uniform point inside a tetrahedron.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <param name="destination">The exact-length point destination.</param>
    public void TetrahedronInto(double[] first, double[] second, double[] third, double[] fourth, Span<double> destination)
    {
        RandomGeometry.Tetrahedron(generator, first, second, third, fourth, destination);
    }

    /// <summary>Fills a point-major batch with uniform tetrahedron points.</summary>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    public void FillTetrahedron(double[] first, double[] second, double[] third, double[] fourth, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillTetrahedron(generator, first, second, third, fourth, sampleCount, destination);
    }

    /// <summary>Returns one uniform unit quaternion in x, y, z, w order.</summary>
    /// <returns>Four quaternion components.</returns>
    public double[] Quaternion()
    {
        return RandomGeometry.Quaternion(generator);
    }

    /// <summary>Writes one uniform unit quaternion in x, y, z, w order.</summary>
    /// <param name="destination">The destination of exactly four components.</param>
    public void QuaternionInto(Span<double> destination)
    {
        RandomGeometry.Quaternion(generator, destination);
    }

    /// <summary>Fills a quaternion-major batch with unit quaternions.</summary>
    /// <param name="sampleCount">The nonnegative quaternion count.</param>
    /// <param name="destination">The destination of exactly four times the count.</param>
    public void FillQuaternions(int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillQuaternions(generator, sampleCount, destination);
    }

    /// <summary>Returns one uniform row-major three-dimensional rotation matrix.</summary>
    /// <returns>Nine matrix entries.</returns>
    public double[] Rotation3D()
    {
        return RandomGeometry.Rotation3D(generator);
    }

    /// <summary>Writes one uniform row-major three-dimensional rotation matrix.</summary>
    /// <param name="destination">The destination of exactly nine entries.</param>
    public void Rotation3DInto(Span<double> destination)
    {
        RandomGeometry.Rotation3D(generator, destination);
    }

    /// <summary>Fills a matrix-major batch with three-dimensional rotations.</summary>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The destination of exactly nine times the count.</param>
    public void FillRotation3D(int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillRotation3D(generator, sampleCount, destination);
    }

    /// <summary>Returns one Haar-distributed row-major orthogonal matrix.</summary>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <returns>Dimension squared entries.</returns>
    public double[] OrthogonalMatrix(int dimension)
    {
        return RandomGeometry.OrthogonalMatrix(generator, dimension);
    }

    /// <summary>Writes one Haar-distributed row-major orthogonal matrix.</summary>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <param name="destination">The destination of exactly dimension squared entries.</param>
    public void OrthogonalMatrixInto(int dimension, Span<double> destination)
    {
        RandomGeometry.OrthogonalMatrix(generator, dimension, destination);
    }

    /// <summary>Fills a matrix-major batch with Haar-distributed orthogonal matrices.</summary>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The exact matrix-major destination.</param>
    public void FillOrthogonalMatrices(int dimension, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillOrthogonalMatrices(generator, dimension, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside a finite simple polygon.</summary>
    /// <param name="vertices">The polygon x, y coordinate pairs.</param>
    /// <returns>Two point coordinates.</returns>
    public double[] Polygon(double[] vertices)
    {
        return RandomGeometry.Polygon(generator, vertices);
    }

    /// <summary>Writes one uniform point inside a finite simple polygon.</summary>
    /// <param name="vertices">The polygon x, y coordinate pairs.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    public void PolygonInto(double[] vertices, Span<double> destination)
    {
        RandomGeometry.Polygon(generator, vertices, destination);
    }

    /// <summary>Fills a point-major batch with uniform polygon points.</summary>
    /// <param name="vertices">The polygon x, y coordinate pairs.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the count.</param>
    public void FillPolygon(double[] vertices, int sampleCount, Span<double> destination)
    {
        RandomGeometry.FillPolygon(generator, vertices, sampleCount, destination);
    }

    /// <summary>Returns deterministic triangulation indexes for a finite simple polygon.</summary>
    /// <param name="vertices">The polygon x, y coordinate pairs.</param>
    /// <returns>Three original vertex indexes per triangle.</returns>
    public int[] PolygonTriangulation(double[] vertices)
    {
        return RandomGeometry.PolygonTriangulation(vertices);
    }
}
