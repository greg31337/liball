namespace random;

/// <summary>
/// Defines a reusable 64-bit digital scramble for Sobol coordinates.
/// </summary>
/// <remarks>
/// Factory methods consume the supplied generator only while constructing the
/// scramble. Callers should supply a dedicated deterministic child stream when
/// independently replayable scrambled designs are required. The resulting
/// scrambler retains no generator and can be reused by multiple sequences.
/// </remarks>
public class DigitalScrambler : IDigitalScrambler
{
    int dimension;
    bool hasLinearMatrix;
    bool hasDigitalShift;
    ulong[] matrixRows;
    ulong[] shifts;

    DigitalScrambler(int dimension, bool hasLinearMatrix, bool hasDigitalShift, ulong[] matrixRows, ulong[] shifts)
    {
        this.dimension = dimension;
        this.hasLinearMatrix = hasLinearMatrix;
        this.hasDigitalShift = hasDigitalShift;
        this.matrixRows = matrixRows;
        this.shifts = shifts;
    }

    /// <summary>
    /// Creates an independent random digital shift for each coordinate.
    /// </summary>
    /// <param name="generator">The explicit generator, preferably a dedicated child stream.</param>
    /// <param name="dimension">The coordinate dimension from one through 21201 inclusive.</param>
    /// <returns>A reusable digital-shift scrambler.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is unsupported.</exception>
    internal static DigitalScrambler CreateDigitalShift(IRandomSource generator, int dimension)
    {
        return Create(generator, dimension, false, true);
    }

    /// <summary>
    /// Creates an independent nonsingular lower-triangular binary matrix for each coordinate.
    /// </summary>
    /// <param name="generator">The explicit generator, preferably a dedicated child stream.</param>
    /// <param name="dimension">The coordinate dimension from one through 21201 inclusive.</param>
    /// <returns>A reusable linear-matrix scrambler with no digital shift.</returns>
    /// <remarks>Each matrix has a unit diagonal and therefore is invertible over the binary field.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is unsupported.</exception>
    internal static DigitalScrambler CreateLinearMatrix(IRandomSource generator, int dimension)
    {
        return Create(generator, dimension, true, false);
    }

    /// <summary>
    /// Creates independent lower-triangular binary matrices followed by digital shifts.
    /// </summary>
    /// <param name="generator">The explicit generator, preferably a dedicated child stream.</param>
    /// <param name="dimension">The coordinate dimension from one through 21201 inclusive.</param>
    /// <returns>A reusable linear-matrix and digital-shift scrambler.</returns>
    /// <remarks>
    /// For each coordinate, matrix rows are derived first in increasing output-bit
    /// order and the shift is derived last. A matrix row with no random entries
    /// consumes no generator word.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is unsupported.</exception>
    internal static DigitalScrambler CreateLinearMatrixWithDigitalShift(IRandomSource generator, int dimension)
    {
        return Create(generator, dimension, true, true);
    }

    internal static DigitalScrambler CreateIdentity(int dimension)
    {
        if (dimension < 1 || dimension > SobolDirectionNumbers.DimensionCount())
        {
            throw RandomArgumentException.Create("dimension", "The scrambler dimension must be between one and 21201 inclusive.");
        }

        return new DigitalScrambler(dimension, false, false, new ulong[0], new ulong[dimension]);
    }

    /// <summary>
    /// Returns the number of coordinates supported by this scramble.
    /// </summary>
    /// <returns>The positive coordinate dimension.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>
    /// Reports whether this scramble applies a lower-triangular linear matrix.
    /// </summary>
    /// <returns><c>true</c> when linear-matrix scrambling is enabled.</returns>
    public bool HasLinearMatrix()
    {
        return hasLinearMatrix;
    }

    /// <summary>
    /// Reports whether this scramble applies a digital shift.
    /// </summary>
    /// <returns><c>true</c> when a digital shift is enabled.</returns>
    public bool HasDigitalShift()
    {
        return hasDigitalShift;
    }

    /// <summary>
    /// Applies this scramble to one raw 64-bit coordinate value.
    /// </summary>
    /// <param name="coordinate">The zero-based coordinate index.</param>
    /// <param name="value">The raw binary-fraction numerator to scramble.</param>
    /// <returns>The linearly transformed value followed by the configured digital shift.</returns>
    /// <exception cref="RandomArgumentException">The coordinate is outside this scrambler's dimension.</exception>
    public ulong Scramble(int coordinate, ulong value)
    {
        if (coordinate < 0 || coordinate >= dimension)
        {
            throw RandomArgumentException.Create("coordinate", "The coordinate is outside the scrambler dimension.");
        }

        return TransformDirection(coordinate, value) ^ shifts[coordinate];
    }

    internal ulong TransformDirection(int coordinate, ulong value)
    {
        if (!hasLinearMatrix)
        {
            return value;
        }

        ulong result = 0UL;
        int row = 0;
        int offset = coordinate * 64;
        while (row < 64)
        {
            ulong selectedBits = value & matrixRows[offset + row];
            if ((BitMath.BitCount(selectedBits) & 1) != 0)
            {
                result |= 1UL << (63 - row);
            }

            row++;
        }

        return result;
    }

    internal ulong Shift(int coordinate)
    {
        return shifts[coordinate];
    }

    static DigitalScrambler Create(IRandomSource generator, int dimension, bool linearMatrix, bool digitalShift)
    {
        Guard.NotNull(generator, "generator");
        if (dimension < 1 || dimension > SobolDirectionNumbers.DimensionCount())
        {
            throw RandomArgumentException.Create("dimension", "The scrambler dimension must be between one and 21201 inclusive.");
        }

        ulong[] matrices;
        if (linearMatrix)
        {
            matrices = new ulong[dimension * 64];
        }
        else
        {
            matrices = new ulong[0];
        }

        ulong[] shifts = new ulong[dimension];
        int coordinate = 0;
        while (coordinate < dimension)
        {
            if (linearMatrix)
            {
                int offset = coordinate * 64;
                matrices[offset] = 0x8000000000000000UL;
                int row = 1;
                while (row < 64)
                {
                    ulong diagonal = 1UL << (63 - row);
                    ulong allowed = ulong.MaxValue << (63 - row);
                    matrices[offset + row] = (generator.NextUInt64() & allowed) | diagonal;
                    row++;
                }
            }

            if (digitalShift)
            {
                shifts[coordinate] = generator.NextUInt64();
            }

            coordinate++;
        }

        return new DigitalScrambler(dimension, linearMatrix, digitalShift, matrices, shifts);
    }
}
