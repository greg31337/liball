using System;

namespace random;

/// <summary>
/// Generates the points of a finite Hammersley point set in index order.
/// </summary>
/// <remarks>
/// For a set of <c>N</c> points, point index <c>i</c> has coordinate zero
/// <c>i / N</c>. Coordinate <c>d</c> for <c>d</c> greater than zero is the
/// radical inverse of <c>i</c> in the prime base assigned to coordinate
/// <c>d - 1</c>. Points and point-major blocks are emitted for indexes zero
/// through <c>N - 1</c>. The set is mutable and is not safe for concurrent use
/// without external synchronization.
/// </remarks>
public class HammersleySet : IHammersleySet
{
    int pointCount;
    int dimension;
    ulong index;
    HaltonSequence? radicalSequence;

    /// <summary>
    /// Creates a finite Hammersley point set positioned at index zero.
    /// </summary>
    /// <param name="pointCount">The positive total number of points.</param>
    /// <param name="dimension">The positive coordinate dimension.</param>
    /// <exception cref="RandomArgumentException">The point count or dimension is not positive.</exception>
    public HammersleySet(int pointCount, int dimension)
    {
        Guard.PositiveCount(pointCount, "pointCount");
        Guard.PositiveCount(dimension, "dimension");
        this.pointCount = pointCount;
        this.dimension = dimension;
        index = 0UL;
        if (dimension > 1)
        {
            radicalSequence = HaltonSequence.CreateUnscrambled(dimension - 1);
        }
    }

    /// <summary>
    /// Returns the fixed number of points in this set.
    /// </summary>
    /// <returns>The positive total point count.</returns>
    public int PointCount()
    {
        return pointCount;
    }

    /// <summary>
    /// Returns the number of coordinates in each point.
    /// </summary>
    /// <returns>The positive point dimension.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>
    /// Returns an independent copy of the prime bases used after coordinate zero.
    /// </summary>
    /// <returns>Exactly dimension minus one consecutive primes beginning with two.</returns>
    public int[] Bases()
    {
        if (radicalSequence == null)
        {
            return new int[0];
        }

        return RadicalSequence().Bases();
    }

    /// <summary>
    /// Returns the exact index of the next point.
    /// </summary>
    /// <returns>A value from zero through <see cref="PointCount()"/> inclusive.</returns>
    public ulong Index()
    {
        return index;
    }

    /// <summary>
    /// Returns the number of points that have not yet been emitted.
    /// </summary>
    /// <returns>The total point count minus the current index.</returns>
    public int Remaining()
    {
        return pointCount - (int)index;
    }

    /// <summary>
    /// Returns the next point and advances its index by one.
    /// </summary>
    /// <returns>A new array containing one point.</returns>
    /// <exception cref="RandomExhaustedException">Every point in the finite set has already been emitted.</exception>
    public double[] Next()
    {
        EnsureAvailable(1UL);
        double[] result = new double[dimension];
        NextInto(result);
        return result;
    }

    /// <summary>
    /// Writes the next point and advances its index by one.
    /// </summary>
    /// <param name="destination">The destination with exactly <see cref="Dimension()"/> coordinates.</param>
    /// <exception cref="RandomArgumentException">The destination length does not equal the dimension.</exception>
    /// <exception cref="RandomExhaustedException">Every point in the finite set has already been emitted.</exception>
    public void NextInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        EnsureAvailable(1UL);
        WritePoint(destination, 0);
    }

    /// <summary>
    /// Returns a point-major block of consecutive remaining points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points to emit.</param>
    /// <returns>A new row-major array containing point count times dimension coordinates.</returns>
    /// <exception cref="RandomArgumentException">The point count is negative or the required storage is too large.</exception>
    /// <exception cref="RandomExhaustedException">The requested block exceeds the remaining finite set.</exception>
    public double[] Fill(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        EnsureAvailable((ulong)pointCount);
        double[] result = new double[requiredLength];
        FillInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Fills a point-major block with consecutive remaining points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points to emit.</param>
    /// <param name="destination">The destination with exactly point count times dimension coordinates.</param>
    /// <remarks>
    /// Point row <c>p</c>, coordinate <c>d</c> is stored at
    /// <c>destination[p * Dimension() + d]</c>. Results match repeated calls to
    /// <see cref="NextInto(Span{double})"/> in increasing set-index order.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The point count or destination length is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The requested block exceeds the remaining finite set.</exception>
    public void FillInto(int pointCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        EnsureAvailable((ulong)pointCount);
        int point = 0;
        int offset = 0;
        while (point < pointCount)
        {
            WritePoint(destination, offset);
            offset += dimension;
            point++;
        }
    }

    /// <summary>
    /// Returns the required row-major storage length for a point block.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points to emit.</param>
    /// <returns>The requested point count multiplied by the dimension.</returns>
    /// <exception cref="RandomArgumentException">The point count is negative or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int pointCount)
    {
        Guard.Count(pointCount, "pointCount");
        if (pointCount > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create("pointCount", "The requested block contains too many coordinates.");
        }

        return pointCount * dimension;
    }

    /// <summary>
    /// Advances past a number of remaining points without evaluating them.
    /// </summary>
    /// <param name="count">The number of points to skip.</param>
    /// <exception cref="RandomExhaustedException">The skip exceeds the remaining finite set.</exception>
    public void Skip(ulong count)
    {
        EnsureAvailable(count);
        if (radicalSequence != null)
        {
            RadicalSequence().AdvanceIndex(count);
        }

        index += count;
    }

    /// <summary>
    /// Restores the set to point index zero.
    /// </summary>
    public void Reset()
    {
        index = 0UL;
        if (radicalSequence != null)
        {
            RadicalSequence().Reset();
        }
    }

    void WritePoint(Span<double> destination, int offset)
    {
        destination[offset] = (double)index / (double)pointCount;
        if (radicalSequence != null)
        {
            RadicalSequence().NextInto(destination.Slice(offset + 1, dimension - 1));
        }

        index++;
    }

    void EnsureAvailable(ulong count)
    {
        ulong total = (ulong)pointCount;
        if (count > total - index)
        {
            throw RandomExhaustedException.Create("The requested operation exceeds the remaining Hammersley point set.");
        }
    }

    HaltonSequence RadicalSequence()
    {
        if (radicalSequence == null)
        {
            throw RandomStateException.Create("The Hammersley radical sequence is unavailable.");
        }

        return radicalSequence;
    }
}
