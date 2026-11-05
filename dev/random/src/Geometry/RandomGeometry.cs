using System;

namespace random;

/// <summary>
/// Provides one-shot and row-major batch facades for uniform geometric sampling.
/// </summary>
/// <remarks>
/// Output layout and generator consumption match the corresponding reusable geometry sampler.
/// With a deterministic generator, Portable math mode provides bit-for-bit replay for the same
/// geometry and call sequence; System math mode promises statistical equivalence only for
/// operations that invoke platform mathematical functions.
/// </remarks>
internal static class RandomGeometry
{
    /// <summary>Returns one uniform point on a line segment.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <returns>A point with the endpoint coordinate dimension.</returns>
    /// <exception cref="RandomArgumentException">An argument or geometry is invalid.</exception>
    public static double[] Line(RandomGenerator generator, double[] start, double[] end)
    {
        return BallSampler.Line(generator, start, end);
    }

    /// <summary>Writes one uniform point on a line segment.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <param name="destination">The exact-length point destination.</param>
    /// <exception cref="RandomArgumentException">An argument, geometry, or destination length is invalid.</exception>
    public static void Line(RandomGenerator generator, double[] start, double[] end, Span<double> destination)
    {
        BallSampler.Line(generator, start, end, destination);
    }

    /// <summary>Fills a point-major batch with uniform line-segment points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>Points occupy consecutive endpoint-dimension blocks and match repeated calls to <see cref="Line(RandomGenerator, double[], double[], Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">An argument, geometry, sample count, or destination length is invalid.</exception>
    public static void FillLine(RandomGenerator generator, double[] start, double[] end, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(start, "start");
        Guard.NotNull(end, "end");
        double[] stableStart = (double[])start.Clone();
        double[] stableEnd = (double[])end.Clone();
        ValidateLine(generator, stableStart, stableEnd);
        int requiredLength = RequiredOutputLength(sampleCount, stableStart.Length);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            BallSampler.Line(generator, stableStart, stableEnd, destination.Slice(offset, stableStart.Length));
            offset += stableStart.Length;
            sample++;
        }
    }

    /// <summary>Returns one uniform point inside an axis-aligned box.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <returns>A point with the bounds coordinate dimension.</returns>
    /// <exception cref="RandomArgumentException">An argument or geometry is invalid.</exception>
    public static double[] Box(RandomGenerator generator, double[] minimum, double[] maximum)
    {
        return BallSampler.Box(generator, minimum, maximum);
    }

    /// <summary>Writes one uniform point inside an axis-aligned box.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <param name="destination">The exact-length point destination.</param>
    /// <exception cref="RandomArgumentException">An argument, geometry, or destination length is invalid.</exception>
    public static void Box(RandomGenerator generator, double[] minimum, double[] maximum, Span<double> destination)
    {
        BallSampler.Box(generator, minimum, maximum, destination);
    }

    /// <summary>Fills a point-major batch with uniform axis-aligned-box points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="minimum">The finite lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>Points match repeated calls to <see cref="Box(RandomGenerator, double[], double[], Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">An argument, geometry, sample count, or destination length is invalid.</exception>
    public static void FillBox(RandomGenerator generator, double[] minimum, double[] maximum, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(minimum, "minimum");
        Guard.NotNull(maximum, "maximum");
        double[] stableMinimum = (double[])minimum.Clone();
        double[] stableMaximum = (double[])maximum.Clone();
        ValidateBox(generator, stableMinimum, stableMaximum);
        int requiredLength = RequiredOutputLength(sampleCount, stableMinimum.Length);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            BallSampler.Box(generator, stableMinimum, stableMaximum, destination.Slice(offset, stableMinimum.Length));
            offset += stableMinimum.Length;
            sample++;
        }
    }

    /// <summary>Returns one uniform point on the unit circle.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>Two coordinates.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitCircle(RandomGenerator generator)
    {
        return BallSampler.UnitCircle(generator);
    }

    /// <summary>Writes one uniform point on the unit circle.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is invalid.</exception>
    public static void UnitCircle(RandomGenerator generator, Span<double> destination)
    {
        BallSampler.UnitCircle(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-circle points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the point count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="UnitCircle(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillUnitCircle(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        FillFixed(generator, sampleCount, destination, 2, 1);
    }

    /// <summary>Returns one uniform point inside the unit disk.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>Two coordinates.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitDisk(RandomGenerator generator)
    {
        return BallSampler.UnitDisk(generator);
    }

    /// <summary>Writes one uniform point inside the unit disk.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is invalid.</exception>
    public static void UnitDisk(RandomGenerator generator, Span<double> destination)
    {
        BallSampler.UnitDisk(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-disk points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the point count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="UnitDisk(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillUnitDisk(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        FillFixed(generator, sampleCount, destination, 2, 2);
    }

    /// <summary>Returns one uniform point on the unit sphere.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>Three coordinates.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitSphere(RandomGenerator generator)
    {
        return BallSampler.UnitSphere(generator);
    }

    /// <summary>Writes one uniform point on the unit sphere.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination of exactly three coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is invalid.</exception>
    public static void UnitSphere(RandomGenerator generator, Span<double> destination)
    {
        BallSampler.UnitSphere(generator, destination);
    }

    /// <summary>Fills a point-major batch with unit-sphere points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly three times the point count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="UnitSphere(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillUnitSphere(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        FillFixed(generator, sampleCount, destination, 3, 3);
    }

    /// <summary>Returns one uniform unit vector in an arbitrary positive dimension.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <returns>A vector with the requested dimension.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is invalid.</exception>
    public static double[] UnitVector(RandomGenerator generator, int dimension)
    {
        return BallSampler.UnitVector(generator, dimension);
    }

    /// <summary>Writes one uniform unit vector.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The nonempty destination whose length defines the dimension.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination is empty.</exception>
    public static void UnitVectorInto(RandomGenerator generator, Span<double> destination)
    {
        BallSampler.UnitVectorInto(generator, destination);
    }

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit vectors.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <param name="sampleCount">The nonnegative vector count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="UnitVectorInto(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, a count is invalid, or the destination length is invalid.</exception>
    public static void FillUnitVector(RandomGenerator generator, int dimension, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        int requiredLength = RequiredOutputLength(sampleCount, dimension);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            BallSampler.UnitVectorInto(generator, destination.Slice(offset, dimension));
            offset += dimension;
            sample++;
        }
    }

    /// <summary>Returns one uniform point inside an arbitrary-dimensional unit ball.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <returns>A point with exactly dimension coordinates.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is invalid.</exception>
    public static double[] UnitBall(RandomGenerator generator, int dimension)
    {
        return BallSampler.UnitBall(generator, dimension);
    }

    /// <summary>Writes one uniform point inside a unit ball.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The nonempty destination whose length defines the dimension.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination is empty.</exception>
    public static void UnitBall(RandomGenerator generator, Span<double> destination)
    {
        BallSampler.UnitBall(generator, destination);
    }

    /// <summary>Fills a point-major batch with arbitrary-dimensional unit-ball points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="UnitBall(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the dimension or sample count is invalid, or the destination length is invalid.</exception>
    public static void FillUnitBall(RandomGenerator generator, int dimension, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        int requiredLength = RequiredOutputLength(sampleCount, dimension);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            BallSampler.UnitBall(generator, destination.Slice(offset, dimension));
            offset += dimension;
            sample++;
        }
    }

    /// <summary>Returns one uniform point inside a full-dimensional simplex.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <returns>A point with exactly dimension coordinates.</returns>
    /// <exception cref="RandomArgumentException">An argument or geometry is invalid.</exception>
    public static double[] Simplex(RandomGenerator generator, double[] vertices, int dimension)
    {
        return SimplexSampler.Simplex(generator, vertices, dimension);
    }

    /// <summary>Writes one uniform point inside a full-dimensional simplex.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="destination">The exact-length point destination.</param>
    /// <exception cref="RandomArgumentException">An argument, geometry, or destination length is invalid.</exception>
    public static void Simplex(RandomGenerator generator, double[] vertices, int dimension, Span<double> destination)
    {
        SimplexSampler.Simplex(generator, vertices, dimension, destination);
    }

    /// <summary>Fills a point-major batch with uniform simplex points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The row-major simplex vertices.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Simplex(RandomGenerator, double[], int, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">An argument, geometry, sample count, or destination length is invalid.</exception>
    public static void FillSimplex(RandomGenerator generator, double[] vertices, int dimension, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(vertices, "vertices");
        double[] stableVertices = (double[])vertices.Clone();
        ValidateSimplex(generator, stableVertices, dimension);
        int requiredLength = RequiredOutputLength(sampleCount, dimension);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            SimplexSampler.Simplex(generator, stableVertices, dimension, destination.Slice(offset, dimension));
            offset += dimension;
            sample++;
        }
    }

    /// <summary>Returns one uniform point inside a triangle.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <returns>A point with the vertex coordinate dimension.</returns>
    /// <exception cref="RandomArgumentException">An argument or geometry is invalid.</exception>
    public static double[] Triangle(RandomGenerator generator, double[] first, double[] second, double[] third)
    {
        return SimplexSampler.Triangle(generator, first, second, third);
    }

    /// <summary>Writes one uniform point inside a triangle.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="destination">The exact-length point destination.</param>
    /// <exception cref="RandomArgumentException">An argument, geometry, or destination length is invalid.</exception>
    public static void Triangle(RandomGenerator generator, double[] first, double[] second, double[] third, Span<double> destination)
    {
        SimplexSampler.Triangle(generator, first, second, third, destination);
    }

    /// <summary>Fills a point-major batch with uniform triangle points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Triangle(RandomGenerator, double[], double[], double[], Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">An argument, geometry, sample count, or destination length is invalid.</exception>
    public static void FillTriangle(RandomGenerator generator, double[] first, double[] second, double[] third, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(first, "first");
        Guard.NotNull(second, "second");
        Guard.NotNull(third, "third");
        double[] stableFirst = (double[])first.Clone();
        double[] stableSecond = (double[])second.Clone();
        double[] stableThird = (double[])third.Clone();
        ValidateTriangle(generator, stableFirst, stableSecond, stableThird);
        int requiredLength = RequiredOutputLength(sampleCount, stableFirst.Length);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            SimplexSampler.Triangle(generator, stableFirst, stableSecond, stableThird, destination.Slice(offset, stableFirst.Length));
            offset += stableFirst.Length;
            sample++;
        }
    }

    /// <summary>Returns one uniform point inside a tetrahedron.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <returns>A point with the vertex coordinate dimension.</returns>
    /// <exception cref="RandomArgumentException">An argument or geometry is invalid.</exception>
    public static double[] Tetrahedron(RandomGenerator generator, double[] first, double[] second, double[] third, double[] fourth)
    {
        return SimplexSampler.Tetrahedron(generator, first, second, third, fourth);
    }

    /// <summary>Writes one uniform point inside a tetrahedron.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <param name="destination">The exact-length point destination.</param>
    /// <exception cref="RandomArgumentException">An argument, geometry, or destination length is invalid.</exception>
    public static void Tetrahedron(RandomGenerator generator, double[] first, double[] second, double[] third, double[] fourth, Span<double> destination)
    {
        SimplexSampler.Tetrahedron(generator, first, second, third, fourth, destination);
    }

    /// <summary>Fills a point-major batch with uniform tetrahedron points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="first">The first vertex.</param>
    /// <param name="second">The second vertex.</param>
    /// <param name="third">The third vertex.</param>
    /// <param name="fourth">The fourth vertex.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Tetrahedron(RandomGenerator, double[], double[], double[], double[], Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">An argument, geometry, sample count, or destination length is invalid.</exception>
    public static void FillTetrahedron(RandomGenerator generator, double[] first, double[] second, double[] third, double[] fourth, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(first, "first");
        Guard.NotNull(second, "second");
        Guard.NotNull(third, "third");
        Guard.NotNull(fourth, "fourth");
        double[] stableFirst = (double[])first.Clone();
        double[] stableSecond = (double[])second.Clone();
        double[] stableThird = (double[])third.Clone();
        double[] stableFourth = (double[])fourth.Clone();
        ValidateTetrahedron(generator, stableFirst, stableSecond, stableThird, stableFourth);
        int requiredLength = RequiredOutputLength(sampleCount, stableFirst.Length);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            SimplexSampler.Tetrahedron(generator, stableFirst, stableSecond, stableThird, stableFourth, destination.Slice(offset, stableFirst.Length));
            offset += stableFirst.Length;
            sample++;
        }
    }

    /// <summary>Returns one uniform unit quaternion in <c>x, y, z, w</c> order.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>Four quaternion components.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] Quaternion(RandomGenerator generator)
    {
        return RotationSampler.Quaternion(generator);
    }

    /// <summary>Writes one uniform unit quaternion in <c>x, y, z, w</c> order.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination of exactly four components.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is invalid.</exception>
    public static void Quaternion(RandomGenerator generator, Span<double> destination)
    {
        RotationSampler.Quaternion(generator, destination);
    }

    /// <summary>Fills a quaternion-major batch with uniform unit quaternions.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative quaternion count.</param>
    /// <param name="destination">The destination of exactly four times the quaternion count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Quaternion(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillQuaternions(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        FillFixed(generator, sampleCount, destination, 4, 4);
    }

    /// <summary>Fills a quaternion-major batch with uniform unit quaternions.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative quaternion count.</param>
    /// <param name="destination">The destination of exactly four times the quaternion count.</param>
    /// <remarks>This is a naming alias for <see cref="FillQuaternions(RandomGenerator, int, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillQuaternion(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        FillQuaternions(generator, sampleCount, destination);
    }

    /// <summary>Returns one uniform row-major three-dimensional rotation matrix.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>Nine matrix entries.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] Rotation3D(RandomGenerator generator)
    {
        return RotationSampler.Rotation3D(generator);
    }

    /// <summary>Writes one uniform row-major three-dimensional rotation matrix.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination of exactly nine entries.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is invalid.</exception>
    public static void Rotation3D(RandomGenerator generator, Span<double> destination)
    {
        RotationSampler.Rotation3D(generator, destination);
    }

    /// <summary>Fills a matrix-major batch with uniform row-major three-dimensional rotations.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The destination of exactly nine times the matrix count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Rotation3D(RandomGenerator, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the sample count is negative, or the destination length is invalid.</exception>
    public static void FillRotation3D(RandomGenerator generator, int sampleCount, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        FillFixed(generator, sampleCount, destination, 9, 5);
    }

    /// <summary>Returns one Haar-distributed row-major orthogonal matrix.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <returns>Dimension squared matrix entries.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is invalid.</exception>
    public static double[] OrthogonalMatrix(RandomGenerator generator, int dimension)
    {
        return new OrthogonalMatrixSampler(generator, dimension).Sample();
    }

    /// <summary>Writes one Haar-distributed row-major orthogonal matrix.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <param name="destination">The destination of exactly dimension squared entries.</param>
    /// <exception cref="RandomArgumentException">The generator, dimension, or destination length is invalid.</exception>
    public static void OrthogonalMatrix(RandomGenerator generator, int dimension, Span<double> destination)
    {
        new OrthogonalMatrixSampler(generator, dimension).SampleInto(destination);
    }

    /// <summary>Fills a matrix-major batch with Haar-distributed row-major orthogonal matrices.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The exact matrix-major destination.</param>
    /// <remarks>The batch matches repeated calls to <see cref="OrthogonalMatrix(RandomGenerator, int, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator, dimension, sample count, or destination length is invalid.</exception>
    public static void FillOrthogonalMatrices(RandomGenerator generator, int dimension, int sampleCount, Span<double> destination)
    {
        OrthogonalMatrixSampler sampler = new OrthogonalMatrixSampler(generator, dimension);
        sampler.Fill(sampleCount, destination);
    }

    /// <summary>Fills a matrix-major batch with Haar-distributed row-major orthogonal matrices.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The exact matrix-major destination.</param>
    /// <remarks>This is a naming alias for <see cref="FillOrthogonalMatrices(RandomGenerator, int, int, Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator, dimension, sample count, or destination length is invalid.</exception>
    public static void FillOrthogonalMatrix(RandomGenerator generator, int dimension, int sampleCount, Span<double> destination)
    {
        FillOrthogonalMatrices(generator, dimension, sampleCount, destination);
    }

    /// <summary>Returns one uniform point inside a finite simple polygon.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The polygon <c>x, y</c> coordinate pairs.</param>
    /// <returns>Two point coordinates.</returns>
    /// <exception cref="RandomArgumentException">The generator or polygon is invalid.</exception>
    public static double[] Polygon(RandomGenerator generator, double[] vertices)
    {
        return new PolytopeSampler(generator, vertices).Sample();
    }

    /// <summary>Writes one uniform point inside a finite simple polygon.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The polygon <c>x, y</c> coordinate pairs.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator, polygon, or destination length is invalid.</exception>
    public static void Polygon(RandomGenerator generator, double[] vertices, Span<double> destination)
    {
        new PolytopeSampler(generator, vertices).SampleInto(destination);
    }

    /// <summary>Fills a point-major batch with uniform polygon points.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="vertices">The polygon <c>x, y</c> coordinate pairs.</param>
    /// <param name="sampleCount">The nonnegative point count.</param>
    /// <param name="destination">The destination of exactly twice the point count.</param>
    /// <remarks>The batch matches repeated calls to <see cref="Polygon(RandomGenerator, double[], Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator, polygon, sample count, or destination length is invalid.</exception>
    public static void FillPolygon(RandomGenerator generator, double[] vertices, int sampleCount, Span<double> destination)
    {
        PolytopeSampler sampler = new PolytopeSampler(generator, vertices);
        sampler.Fill(sampleCount, destination);
    }

    /// <summary>Returns deterministic triangulation indexes for a finite simple polygon.</summary>
    /// <param name="vertices">The polygon <c>x, y</c> coordinate pairs.</param>
    /// <returns>Three original vertex indexes per triangle.</returns>
    /// <exception cref="RandomArgumentException">The polygon is invalid.</exception>
    public static int[] PolygonTriangulation(double[] vertices)
    {
        return PolytopeSampler.Triangulate(vertices);
    }

    static int RequiredOutputLength(int sampleCount, int componentCount)
    {
        Guard.Count(sampleCount, "sampleCount");
        Guard.PositiveCount(componentCount, "dimension");
        if (sampleCount > int.MaxValue / componentCount)
        {
            throw RandomArgumentException.Create("sampleCount", "The requested batch contains too many coordinates.");
        }

        return sampleCount * componentCount;
    }

    static void FillFixed(RandomGenerator generator, int sampleCount, Span<double> destination, int componentCount, int shape)
    {
        int requiredLength = RequiredOutputLength(sampleCount, componentCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            Span<double> point = destination.Slice(offset, componentCount);
            switch (shape)
            {
                case 1:
                    BallSampler.UnitCircle(generator, point);
                    break;
                case 2:
                    BallSampler.UnitDisk(generator, point);
                    break;
                case 3:
                    BallSampler.UnitSphere(generator, point);
                    break;
                case 4:
                    RotationSampler.Quaternion(generator, point);
                    break;
                case 5:
                    RotationSampler.Rotation3D(generator, point);
                    break;
                default:
                    throw RandomStateException.Create("The fixed geometry shape is invalid.");
            }

            offset += componentCount;
            sample++;
        }
    }

    static void ValidateLine(RandomGenerator generator, double[] start, double[] end)
    {
        RandomGenerator validationGenerator = generator.Clone();
        BallSampler.Line(validationGenerator, start, end);
    }

    static void ValidateBox(RandomGenerator generator, double[] minimum, double[] maximum)
    {
        RandomGenerator validationGenerator = generator.Clone();
        BallSampler.Box(validationGenerator, minimum, maximum);
    }

    static void ValidateSimplex(RandomGenerator generator, double[] vertices, int dimension)
    {
        RandomGenerator validationGenerator = generator.Clone();
        SimplexSampler.Simplex(validationGenerator, vertices, dimension);
    }

    static void ValidateTriangle(RandomGenerator generator, double[] first, double[] second, double[] third)
    {
        RandomGenerator validationGenerator = generator.Clone();
        SimplexSampler.Triangle(validationGenerator, first, second, third);
    }

    static void ValidateTetrahedron(RandomGenerator generator, double[] first, double[] second, double[] third, double[] fourth)
    {
        RandomGenerator validationGenerator = generator.Clone();
        SimplexSampler.Tetrahedron(validationGenerator, first, second, third, fourth);
    }
}
