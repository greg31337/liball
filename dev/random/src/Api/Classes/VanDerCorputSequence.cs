using System;

namespace random;

/// <summary>
/// Generates a one-dimensional Van der Corput low-discrepancy sequence.
/// </summary>
/// <remarks>
/// Index zero produces zero. Each later value is the radical inverse of its index
/// in the configured integer base. The sequence is mutable and is not safe for
/// concurrent use without external synchronization.
/// </remarks>
public class VanDerCorputSequence : IVanDerCorputSequence
{
    int numberBase;
    ulong index;

    /// <summary>
    /// Creates a Van der Corput sequence positioned at index zero.
    /// </summary>
    /// <param name="numberBase">The radical-inverse base, which must be at least two.</param>
    /// <exception cref="RandomArgumentException">The base is less than two.</exception>
    public VanDerCorputSequence(int numberBase)
    {
        if (numberBase < 2)
        {
            throw RandomArgumentException.Create("numberBase", "The base must be at least two.");
        }

        this.numberBase = numberBase;
        index = 0UL;
    }

    /// <summary>
    /// Returns the integer base used for the radical inverse.
    /// </summary>
    /// <returns>The configured base.</returns>
    public int Base()
    {
        return numberBase;
    }

    /// <summary>
    /// Returns the exact index of the next value.
    /// </summary>
    /// <returns>The next sequence index, or <c>ulong.MaxValue</c> at the terminal position.</returns>
    public ulong Index()
    {
        return index;
    }

    /// <summary>
    /// Returns the radical inverse at the current index and advances by one.
    /// </summary>
    /// <returns>A value in the closed-open interval <c>[0, 1)</c>.</returns>
    /// <exception cref="RandomExhaustedException">The sequence is at its terminal index.</exception>
    public double Next()
    {
        if (index == ulong.MaxValue)
        {
            throw RandomExhaustedException.Create("The Van der Corput sequence index is exhausted.");
        }

        double value = Evaluate(index, numberBase);
        index++;
        return value;
    }

    /// <summary>
    /// Fills a destination with consecutive sequence values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and final index match repeated calls to <see cref="Next"/>.</remarks>
    /// <exception cref="RandomExhaustedException">The destination would advance beyond the terminal index.</exception>
    public void Fill(Span<double> destination)
    {
        EnsureAvailable((ulong)destination.Length);
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = Evaluate(index, numberBase);
            index++;
            destinationIndex++;
        }
    }

    /// <summary>
    /// Advances past a number of sequence values without evaluating them.
    /// </summary>
    /// <param name="count">The number of values to skip.</param>
    /// <exception cref="RandomExhaustedException">The skip would advance beyond the terminal index.</exception>
    public void Skip(ulong count)
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

    internal static double Evaluate(ulong sequenceIndex, int numberBase)
    {
        ulong remaining = sequenceIndex;
        ulong radix = (ulong)numberBase;
        double inverseBase = 1.0 / (double)numberBase;
        double placeValue = inverseBase;
        double result = 0.0;
        while (remaining != 0UL)
        {
            ulong digit = remaining % radix;
            result += (double)digit * placeValue;
            remaining /= radix;
            placeValue *= inverseBase;
        }

        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }

    void EnsureAvailable(ulong count)
    {
        if (count > ulong.MaxValue - index)
        {
            throw RandomExhaustedException.Create("The Van der Corput sequence index would overflow.");
        }
    }
}
