using System;

namespace random;

/// <summary>
/// Generates an unscrambled multidimensional Sobol low-discrepancy sequence.
/// </summary>
/// <remarks>
/// The sequence uses 64-bit direction numbers from the Joe-Kuo D(6) data and
/// binary-reflected Gray-code ordering. Point index zero contains only zeros.
/// Use <see cref="FillPowerOfTwoInto(int, Span{double})"/> from a block-aligned index
/// when the Sobol digital-net balance guarantee is required. Arbitrary counts,
/// skipping, and thinning remain deterministic but do not carry that guarantee.
/// The sequence is mutable and is not safe for concurrent use without external
/// synchronization.
/// </remarks>
public class SobolSequence : ISobolSequence
{
    int dimension;
    ulong index;
    ulong[] directions = new ulong[0];
    ulong[] initialState = new ulong[0];
    ulong[] state = new ulong[0];

    /// <summary>
    /// Creates a Sobol sequence using a precomputed digital scrambler.
    /// </summary>
    /// <param name="dimension">The point dimension from one through 21201 inclusive.</param>
    /// <param name="scrambler">The reusable scrambler with the same dimension.</param>
    /// <remarks>The scrambler is fully precomputed and no random generator is retained or consumed by sequence operations.</remarks>
    /// <exception cref="RandomArgumentException">The dimension is unsupported, the scrambler is null, or its dimension differs.</exception>
    public SobolSequence(int dimension, DigitalScrambler scrambler)
    {
        ValidateDimension(dimension);
        Guard.NotNull(scrambler, "scrambler");
        if (scrambler.Dimension() != dimension)
        {
            throw RandomArgumentException.Create("scrambler", "The scrambler dimension must match the Sobol sequence dimension.");
        }

        Initialize(dimension, scrambler);
    }

    /// <summary>
    /// Creates an unscrambled Sobol sequence positioned at index zero.
    /// </summary>
    /// <param name="dimension">The point dimension from one through 21201 inclusive.</param>
    /// <returns>An unscrambled Sobol sequence.</returns>
    /// <exception cref="RandomArgumentException">The dimension is outside the supported Joe-Kuo data range.</exception>
    internal static SobolSequence CreateUnscrambled(int dimension)
    {
        return new SobolSequence(dimension, DigitalScrambler.CreateIdentity(dimension));
    }

    /// <summary>
    /// Returns the number of coordinates in each point.
    /// </summary>
    /// <returns>The supported positive point dimension.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>
    /// Returns the exact index of the next point.
    /// </summary>
    /// <returns>The next point index, or <c>ulong.MaxValue</c> at the terminal position.</returns>
    public ulong Index()
    {
        return index;
    }

    /// <summary>
    /// Returns one point and advances the sequence by one recurrence step.
    /// </summary>
    /// <returns>A new array containing one point in the closed-open unit cube.</returns>
    /// <exception cref="RandomExhaustedException">The sequence is at its terminal index.</exception>
    public double[] Next()
    {
        EnsureAvailable(1UL);
        double[] result = new double[dimension];
        NextInto(result);
        return result;
    }

    /// <summary>
    /// Writes one point and advances the sequence by one recurrence step.
    /// </summary>
    /// <param name="destination">The destination with exactly <see cref="Dimension()"/> coordinates.</param>
    /// <exception cref="RandomArgumentException">The destination length does not equal the dimension.</exception>
    /// <exception cref="RandomExhaustedException">The sequence is at its terminal index.</exception>
    public void NextInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        EnsureAvailable(1UL);
        WriteCurrent(destination, 0);
        AdvanceOne();
    }

    /// <summary>
    /// Returns a point-major block of consecutive Sobol points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points.</param>
    /// <returns>A new row-major array containing point count times dimension coordinates.</returns>
    /// <remarks>Arbitrary point counts do not carry the balance guarantee of <see cref="FillPowerOfTwo(int)"/>.</remarks>
    /// <exception cref="RandomArgumentException">The point count is negative or the required storage is too large.</exception>
    /// <exception cref="RandomExhaustedException">The block would advance beyond the terminal index.</exception>
    public double[] Fill(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        EnsureAvailable((ulong)pointCount);
        double[] result = new double[requiredLength];
        FillInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Fills a point-major block with consecutive Sobol points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points.</param>
    /// <param name="destination">The destination with exactly point count times dimension coordinates.</param>
    /// <remarks>
    /// Point <c>p</c>, coordinate <c>d</c> is written at
    /// <c>destination[p * Dimension() + d]</c>. Results match repeated calls to
    /// <see cref="NextInto(Span{double})"/> in increasing point order. Arbitrary point
    /// counts do not carry the balance guarantee of <see cref="FillPowerOfTwoInto(int, Span{double})"/>.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The point count or destination length is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The block would advance beyond the terminal index.</exception>
    public void FillInto(int pointCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        EnsureAvailable((ulong)pointCount);
        int point = 0;
        int offset = 0;
        while (point < pointCount)
        {
            WriteCurrent(destination, offset);
            AdvanceOne();
            offset += dimension;
            point++;
        }
    }

    /// <summary>
    /// Returns a balanced, aligned block containing a power-of-two number of points.
    /// </summary>
    /// <param name="exponent">The exponent defining a block of <c>2^exponent</c> points.</param>
    /// <returns>A new point-major array containing the requested aligned block.</returns>
    /// <remarks>The current index must be divisible by the requested point count.</remarks>
    /// <exception cref="RandomArgumentException">The exponent or required storage is invalid.</exception>
    /// <exception cref="RandomStateException">The current index is not aligned to the requested block size.</exception>
    /// <exception cref="RandomExhaustedException">The block would advance beyond the terminal index.</exception>
    public double[] FillPowerOfTwo(int exponent)
    {
        int requiredLength = RequiredPowerOfTwoOutputLength(exponent);
        int pointCount = 1 << exponent;
        ValidatePowerOfTwoBlock((ulong)pointCount);
        double[] result = new double[requiredLength];
        FillInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Fills a balanced, aligned block containing a power-of-two number of points.
    /// </summary>
    /// <param name="exponent">The exponent defining a block of <c>2^exponent</c> points.</param>
    /// <param name="destination">The exact point-major block destination.</param>
    /// <remarks>The current index must be divisible by the requested point count.</remarks>
    /// <exception cref="RandomArgumentException">The exponent or destination length is invalid.</exception>
    /// <exception cref="RandomStateException">The current index is not aligned to the requested block size.</exception>
    /// <exception cref="RandomExhaustedException">The block would advance beyond the terminal index.</exception>
    public void FillPowerOfTwoInto(int exponent, Span<double> destination)
    {
        int requiredLength = RequiredPowerOfTwoOutputLength(exponent);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int pointCount = 1 << exponent;
        ValidatePowerOfTwoBlock((ulong)pointCount);
        FillInto(pointCount, destination);
    }

    /// <summary>
    /// Returns the storage required for a power-of-two point block.
    /// </summary>
    /// <param name="exponent">The exponent defining a block of <c>2^exponent</c> points.</param>
    /// <returns>The power-of-two point count multiplied by the dimension.</returns>
    /// <exception cref="RandomArgumentException">The exponent is outside the supported array range or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredPowerOfTwoOutputLength(int exponent)
    {
        if (exponent < 0 || exponent > 30)
        {
            throw RandomArgumentException.Create("exponent", "The exponent must be between zero and 30 inclusive.");
        }

        int pointCount = 1 << exponent;
        if (pointCount > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create("exponent", "The requested power-of-two block contains too many coordinates.");
        }

        return pointCount * dimension;
    }

    /// <summary>
    /// Returns the required row-major storage length for a point block.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points.</param>
    /// <returns>The point count multiplied by the dimension.</returns>
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
    /// Fast-forwards past a number of points using direct Gray-code positioning.
    /// </summary>
    /// <param name="count">The number of points to skip.</param>
    /// <exception cref="RandomExhaustedException">The skip would advance beyond the terminal index.</exception>
    public void Skip(ulong count)
    {
        FastForward(count);
    }

    /// <summary>
    /// Advances by an arbitrary distance using direct Gray-code positioning.
    /// </summary>
    /// <param name="count">The number of points to advance.</param>
    /// <remarks>The work depends on the dimension and index bit width, not on the distance advanced.</remarks>
    /// <exception cref="RandomExhaustedException">The advance would move beyond the terminal index.</exception>
    public void FastForward(ulong count)
    {
        EnsureAvailable(count);
        if (count == 0UL)
        {
            return;
        }

        ulong targetIndex = index + count;
        SetIndex(targetIndex);
    }

    /// <summary>
    /// Restores the sequence to point index zero.
    /// </summary>
    public void Reset()
    {
        index = 0UL;
        int coordinate = 0;
        while (coordinate < state.Length)
        {
            state[coordinate] = initialState[coordinate];
            coordinate++;
        }
    }

    void WriteCurrent(Span<double> destination, int offset)
    {
        int coordinate = 0;
        while (coordinate < dimension)
        {
            destination[offset + coordinate] = ToUnitDouble(state[coordinate]);
            coordinate++;
        }
    }

    void AdvanceOne()
    {
        ulong nextIndex = index + 1UL;
        int directionIndex = BitMath.TrailingZeroCount(nextIndex);
        int coordinate = 0;
        int directionOffset = directionIndex;
        while (coordinate < dimension)
        {
            state[coordinate] ^= directions[directionOffset];
            directionOffset += 64;
            coordinate++;
        }

        index = nextIndex;
    }

    void SetIndex(ulong targetIndex)
    {
        ulong grayCode = targetIndex ^ (targetIndex >> 1);
        int coordinate = 0;
        int coordinateOffset = 0;
        while (coordinate < dimension)
        {
            ulong value = initialState[coordinate];
            ulong remainingBits = grayCode;
            int directionIndex = 0;
            while (remainingBits != 0UL)
            {
                if ((remainingBits & 1UL) != 0UL)
                {
                    value ^= directions[coordinateOffset + directionIndex];
                }

                remainingBits >>= 1;
                directionIndex++;
            }

            state[coordinate] = value;
            coordinateOffset += 64;
            coordinate++;
        }

        index = targetIndex;
    }

    void EnsureAvailable(ulong count)
    {
        if (count > ulong.MaxValue - index)
        {
            throw RandomExhaustedException.Create("The Sobol sequence index would overflow.");
        }
    }

    void ValidatePowerOfTwoBlock(ulong pointCount)
    {
        EnsureAvailable(pointCount);
        if ((index & (pointCount - 1UL)) != 0UL)
        {
            throw RandomStateException.Create("The current Sobol index is not aligned to the requested power-of-two block size.");
        }
    }

    static double ToUnitDouble(ulong value)
    {
        double result = PortableMath.ScaleByPowerOfTwo((double)value, -64);
        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }

    void Initialize(int dimension, DigitalScrambler? scrambler)
    {
        this.dimension = dimension;
        index = 0UL;
        directions = new ulong[dimension * 64];
        initialState = new ulong[dimension];
        state = new ulong[dimension];
        int coordinate = 0;
        int offset = 0;
        while (coordinate < dimension)
        {
            SobolDirectionNumbers.Fill(coordinate + 1, directions.AsSpan(offset, 64));
            if (scrambler != null)
            {
                initialState[coordinate] = scrambler.Shift(coordinate);
                if (scrambler.HasLinearMatrix())
                {
                    int directionIndex = 0;
                    while (directionIndex < 64)
                    {
                        directions[offset + directionIndex] = scrambler.TransformDirection(coordinate, directions[offset + directionIndex]);
                        directionIndex++;
                    }
                }
            }

            state[coordinate] = initialState[coordinate];
            offset += 64;
            coordinate++;
        }
    }

    static void ValidateDimension(int dimension)
    {
        if (dimension < 1 || dimension > SobolDirectionNumbers.DimensionCount())
        {
            throw RandomArgumentException.Create("dimension", "The Sobol dimension must be between one and 21201 inclusive.");
        }
    }
}
