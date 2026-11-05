using System;

namespace random;

/// <summary>
/// Generates randomized or centered Latin hypercube point designs.
/// </summary>
/// <remarks>
/// Each call creates a complete independent batch design. Repeated calls do not
/// extend a previous Latin hypercube. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class LatinHypercubeSampler : ILatinHypercubeSampler
{
    IRandomSource generator;
    int dimension;
    bool centered;

    /// <summary>
    /// Creates a randomized or centered Latin hypercube sampler.
    /// </summary>
    /// <param name="generator">The generator used for deterministic permutations and randomized offsets.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="centered">Whether every value is placed at the center of its one-dimensional stratum.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is not positive.</exception>
    public LatinHypercubeSampler(IRandomSource generator, int dimension, bool centered)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        this.generator = generator;
        this.dimension = dimension;
        this.centered = centered;
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
    /// Reports whether points are centered within their finest strata.
    /// </summary>
    /// <returns><c>true</c> for centered designs; otherwise, <c>false</c>.</returns>
    public bool IsCentered()
    {
        return centered;
    }

    /// <summary>
    /// Returns one complete strength-one Latin hypercube design.
    /// </summary>
    /// <param name="pointCount">The positive number of points.</param>
    /// <returns>A new point-major array containing the design.</returns>
    /// <exception cref="RandomArgumentException">The point count or required storage is invalid.</exception>
    public double[] Sample(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        double[] result = new double[requiredLength];
        SampleInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Writes one complete strength-one Latin hypercube design.
    /// </summary>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>Point <c>p</c>, coordinate <c>d</c> is stored at <c>destination[p * Dimension() + d]</c>.</remarks>
    /// <exception cref="RandomArgumentException">The point count or destination length is invalid.</exception>
    public void SampleInto(int pointCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int coordinate = 0;
        while (coordinate < dimension)
        {
            int[] strata = PermutationSampler.Permutation(generator, pointCount);
            int point = 0;
            while (point < pointCount)
            {
                double offset = 0.5;
                if (!centered)
                {
                    offset = FloatingUniform.NextDoubleClosedOpen(generator);
                }

                destination[point * dimension + coordinate] = StratifiedValue(strata[point], pointCount, offset);
                point++;
            }

            coordinate++;
        }
    }

    /// <summary>
    /// Returns one strength-two orthogonal-array Latin hypercube design.
    /// </summary>
    /// <param name="pointCount">The point count <c>p^2</c> for a prime <c>p</c>.</param>
    /// <returns>A new point-major array containing the orthogonal design.</returns>
    /// <remarks>The configured dimension must not exceed <c>p + 1</c>.</remarks>
    /// <exception cref="RandomArgumentException">The point count is not a supported prime square, the dimension is too large, or storage is invalid.</exception>
    public double[] SampleOrthogonal(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        int prime = OrthogonalPrime(pointCount);
        ValidateOrthogonalDimension(prime);
        double[] result = new double[requiredLength];
        SampleOrthogonalInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Writes one strength-two orthogonal-array Latin hypercube design.
    /// </summary>
    /// <param name="pointCount">The point count <c>p^2</c> for a prime <c>p</c>.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>
    /// The configured dimension must not exceed <c>p + 1</c>. Every pair of
    /// coordinates contains one point in each cell of the coarse <c>p * p</c> grid.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The point count is not a supported prime square, the dimension is too large, or the destination is invalid.</exception>
    public void SampleOrthogonalInto(int pointCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        int prime = OrthogonalPrime(pointCount);
        ValidateOrthogonalDimension(prime);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int coordinate = 0;
        while (coordinate < dimension)
        {
            FillOrthogonalCoordinate(prime, pointCount, coordinate, destination);
            coordinate++;
        }
    }

    /// <summary>
    /// Returns the required point-major storage length for a design.
    /// </summary>
    /// <param name="pointCount">The positive number of points.</param>
    /// <returns>The point count multiplied by the dimension.</returns>
    /// <exception cref="RandomArgumentException">The point count is not positive or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int pointCount)
    {
        Guard.PositiveCount(pointCount, "pointCount");
        if (pointCount > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create("pointCount", "The requested design contains too many coordinates.");
        }

        return pointCount * dimension;
    }

    void FillOrthogonalCoordinate(int prime, int pointCount, int coordinate, Span<double> destination)
    {
        int[] symbolPermutation = PermutationSampler.Permutation(generator, prime);
        int[] finePermutations = new int[pointCount];
        int symbol = 0;
        while (symbol < prime)
        {
            int[] permutation = PermutationSampler.Permutation(generator, prime);
            int fine = 0;
            while (fine < prime)
            {
                finePermutations[symbol * prime + fine] = permutation[fine];
                fine++;
            }

            symbol++;
        }

        int[] occurrences = new int[prime];
        int point = 0;
        while (point < pointCount)
        {
            int first = point / prime;
            int second = point % prime;
            int baseSymbol;
            if (coordinate == 0)
            {
                baseSymbol = first;
            }
            else if (coordinate == 1)
            {
                baseSymbol = second;
            }
            else
            {
                baseSymbol = (first + (coordinate - 1) * second) % prime;
            }

            int permutedSymbol = symbolPermutation[baseSymbol];
            int occurrence = occurrences[permutedSymbol];
            int fineStratum = finePermutations[permutedSymbol * prime + occurrence];
            occurrences[permutedSymbol] = occurrence + 1;
            int stratum = permutedSymbol * prime + fineStratum;
            double offset = 0.5;
            if (!centered)
            {
                offset = FloatingUniform.NextDoubleClosedOpen(generator);
            }

            destination[point * dimension + coordinate] = StratifiedValue(stratum, pointCount, offset);
            point++;
        }
    }

    void ValidateOrthogonalDimension(int prime)
    {
        if (dimension > prime + 1)
        {
            throw RandomArgumentException.Create("dimension", "A strength-two orthogonal design requires the dimension to be at most p plus one.");
        }
    }

    static int OrthogonalPrime(int pointCount)
    {
        int root = 1;
        while ((long)root * (long)root < (long)pointCount)
        {
            root++;
        }

        if ((long)root * (long)root != (long)pointCount || !IsPrime(root))
        {
            throw RandomArgumentException.Create("pointCount", "A strength-two orthogonal design requires a prime-square point count.");
        }

        return root;
    }

    static bool IsPrime(int value)
    {
        if (value < 2)
        {
            return false;
        }

        int divisor = 2;
        while ((long)divisor * (long)divisor <= (long)value)
        {
            if (value % divisor == 0)
            {
                return false;
            }

            divisor++;
        }

        return true;
    }

    static double StratifiedValue(int stratum, int pointCount, double offset)
    {
        double result = ((double)stratum + offset) / (double)pointCount;
        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }
}
