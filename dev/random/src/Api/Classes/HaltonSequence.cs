using System;

namespace random;

/// <summary>
/// Generates a multidimensional Halton low-discrepancy sequence.
/// </summary>
/// <remarks>
/// Coordinate dimensions use consecutive prime bases beginning with two. Points
/// are indexed from zero and point zero contains only zeros. The sequence is
/// mutable and is not safe for concurrent use without external synchronization.
/// </remarks>
public class HaltonSequence : IHaltonSequence
{
    int[] bases;
    int[][] digitPermutations;
    ulong index;

    /// <summary>
    /// Creates a digit-permuted Halton sequence positioned at index zero.
    /// </summary>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="digitPermutations">One digit permutation for each generated prime base.</param>
    /// <remarks>
    /// Every permutation must contain each digit from zero through base minus one
    /// exactly once and must map zero to zero. The permutations are copied.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The dimension or a digit permutation is invalid.</exception>
    public HaltonSequence(int dimension, int[][] digitPermutations)
    {
        bases = GenerateBases(dimension);
        this.digitPermutations = ValidateAndCopyPermutations(digitPermutations, bases);
        index = 0UL;
    }

    /// <summary>
    /// Creates an unscrambled Halton sequence positioned at index zero.
    /// </summary>
    /// <param name="dimension">The positive point dimension.</param>
    /// <returns>An unscrambled sequence using consecutive prime bases.</returns>
    /// <exception cref="RandomArgumentException">The dimension is not positive or its prime bases cannot be represented.</exception>
    internal static HaltonSequence CreateUnscrambled(int dimension)
    {
        int[] selectedBases = GenerateBases(dimension);
        int[][] identityPermutations = new int[dimension][];
        int coordinate = 0;
        while (coordinate < dimension)
        {
            int[] permutation = new int[selectedBases[coordinate]];
            int digit = 0;
            while (digit < permutation.Length)
            {
                permutation[digit] = digit;
                digit++;
            }

            identityPermutations[coordinate] = permutation;
            coordinate++;
        }

        return new HaltonSequence(dimension, identityPermutations);
    }

    /// <summary>
    /// Returns the number of coordinates in each point.
    /// </summary>
    /// <returns>The positive point dimension.</returns>
    public int Dimension()
    {
        return bases.Length;
    }

    /// <summary>
    /// Returns an independent copy of the prime base assigned to each dimension.
    /// </summary>
    /// <returns>The consecutive prime bases beginning with two.</returns>
    public int[] Bases()
    {
        return (int[])bases.Clone();
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
    /// Returns one point and advances the sequence by one.
    /// </summary>
    /// <returns>A new array containing one point.</returns>
    /// <exception cref="RandomExhaustedException">The sequence is at its terminal index.</exception>
    public double[] Next()
    {
        double[] result = new double[bases.Length];
        NextInto(result);
        return result;
    }

    /// <summary>
    /// Writes one point and advances the sequence by one.
    /// </summary>
    /// <param name="destination">The destination with exactly <see cref="Dimension()"/> coordinates.</param>
    /// <exception cref="RandomArgumentException">The destination length does not equal the dimension.</exception>
    /// <exception cref="RandomExhaustedException">The sequence is at its terminal index.</exception>
    public void NextInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, bases.Length, "destination");
        EnsureAvailable(1UL);
        WritePoint(index, destination, 0);
        index++;
    }

    /// <summary>
    /// Returns a point-major block of consecutive points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points.</param>
    /// <returns>A new row-major array containing point count times dimension coordinates.</returns>
    /// <exception cref="RandomArgumentException">The point count is negative or the required storage is too large.</exception>
    /// <exception cref="RandomExhaustedException">The block would advance beyond the terminal index.</exception>
    public double[] Fill(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        double[] result = new double[requiredLength];
        FillInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Fills a point-major block with consecutive points.
    /// </summary>
    /// <param name="pointCount">The nonnegative number of points.</param>
    /// <param name="destination">The destination with exactly point count times dimension coordinates.</param>
    /// <remarks>
    /// Point <c>p</c>, coordinate <c>d</c> is written at
    /// <c>destination[p * Dimension() + d]</c>. Results match repeated calls to
    /// <see cref="NextInto(Span{double})"/> in increasing point order.
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
            WritePoint(index, destination, offset);
            index++;
            offset += bases.Length;
            point++;
        }
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
        if (pointCount > int.MaxValue / bases.Length)
        {
            throw RandomArgumentException.Create("pointCount", "The requested block contains too many coordinates.");
        }

        return pointCount * bases.Length;
    }

    /// <summary>
    /// Advances past a number of points without evaluating them.
    /// </summary>
    /// <param name="count">The number of points to skip.</param>
    /// <exception cref="RandomExhaustedException">The skip would advance beyond the terminal index.</exception>
    public void Skip(ulong count)
    {
        AdvanceIndex(count);
    }

    internal void AdvanceIndex(ulong count)
    {
        EnsureAvailable(count);
        index += count;
    }

    /// <summary>
    /// Restores the sequence to index zero.
    /// </summary>
    public void Reset()
    {
        index = 0UL;
    }

    void WritePoint(ulong pointIndex, Span<double> destination, int offset)
    {
        int coordinate = 0;
        while (coordinate < bases.Length)
        {
            int[] permutation = digitPermutations[coordinate];
            if (permutation == null)
            {
                destination[offset + coordinate] = VanDerCorputSequence.Evaluate(pointIndex, bases[coordinate]);
            }
            else
            {
                destination[offset + coordinate] = EvaluatePermuted(pointIndex, bases[coordinate], permutation);
            }

            coordinate++;
        }
    }

    void EnsureAvailable(ulong count)
    {
        if (count > ulong.MaxValue - index)
        {
            throw RandomExhaustedException.Create("The Halton sequence index would overflow.");
        }
    }

    static double EvaluatePermuted(ulong sequenceIndex, int numberBase, int[] permutation)
    {
        ulong remaining = sequenceIndex;
        ulong radix = (ulong)numberBase;
        double inverseBase = 1.0 / (double)numberBase;
        double placeValue = inverseBase;
        double result = 0.0;
        while (remaining != 0UL)
        {
            int digit = (int)(remaining % radix);
            result += (double)permutation[digit] * placeValue;
            remaining /= radix;
            placeValue *= inverseBase;
        }

        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }

    static int[] GenerateBases(int dimension)
    {
        Guard.PositiveCount(dimension, "dimension");
        int[] result = new int[dimension];
        int count = 0;
        int candidate = 2;
        while (count < dimension)
        {
            if (IsPrime(candidate, result, count))
            {
                result[count] = candidate;
                count++;
            }

            if (count < dimension)
            {
                if (candidate == int.MaxValue)
                {
                    throw RandomArgumentException.Create("dimension", "The requested prime bases cannot be represented.");
                }

                if (candidate == 2)
                {
                    candidate = 3;
                }
                else
                {
                    candidate += 2;
                }
            }
        }

        return result;
    }

    static bool IsPrime(int candidate, int[] primes, int primeCount)
    {
        int index = 0;
        while (index < primeCount)
        {
            int divisor = primes[index];
            if ((long)divisor * (long)divisor > (long)candidate)
            {
                break;
            }

            if (candidate % divisor == 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static int[][] ValidateAndCopyPermutations(int[][] permutations, int[] primeBases)
    {
        Guard.NotNull(permutations, "digitPermutations");
        if (permutations.Length != primeBases.Length)
        {
            throw RandomArgumentException.Create("digitPermutations", "There must be one digit permutation for each dimension.");
        }

        int[][] result = new int[primeBases.Length][];
        int coordinate = 0;
        while (coordinate < primeBases.Length)
        {
            int[] permutation = permutations[coordinate];
            if (permutation == null || permutation.Length != primeBases[coordinate])
            {
                throw RandomArgumentException.Create("digitPermutations", "Each digit permutation must match its dimension base.");
            }

            if (permutation[0] != 0)
            {
                throw RandomArgumentException.Create("digitPermutations", "Each digit permutation must map zero to zero.");
            }

            bool[] seen = new bool[permutation.Length];
            int digit = 0;
            while (digit < permutation.Length)
            {
                int mappedDigit = permutation[digit];
                if (mappedDigit < 0 || mappedDigit >= permutation.Length || seen[mappedDigit])
                {
                    throw RandomArgumentException.Create("digitPermutations", "Each digit permutation must contain every base digit exactly once.");
                }

                seen[mappedDigit] = true;
                digit++;
            }

            result[coordinate] = (int[])permutation.Clone();
            coordinate++;
        }

        return result;
    }
}
