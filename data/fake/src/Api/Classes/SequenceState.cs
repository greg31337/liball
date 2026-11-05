using System;
using System.Numerics;

namespace fake;
/// <summary>
/// Produces a deterministic arithmetic sequence of signed 64-bit values.
/// </summary>
/// <remarks>
/// Values begin at the configured start and advance by a nonzero increment.
/// A bounded sequence stops before its exclusive boundary. A cyclic sequence
/// returns to its start instead. Paging is zero-based and does not change the
/// current position.
/// </remarks>
public class SequenceState : ISequenceState
{
    long StartValue;
    long IncrementValue;
    long BoundaryValue;
    bool HasBoundary;
    bool Cyclic;
    int MaximumCount;
    BigInteger Position;
    object Sync;
    internal SequenceState(long start, long increment, int maximumCount)
    {
        ValidateIncrement(increment);
        StartValue = start;
        IncrementValue = increment;
        BoundaryValue = 0L;
        HasBoundary = false;
        Cyclic = false;
        MaximumCount = maximumCount;
        Position = BigInteger.Zero;
        Sync = new object ();
    }

    internal static SequenceState Bounded(long start, long increment, long boundaryExclusive, bool cyclic, int maximumCount)
    {
        ValidateBoundary(start, increment, boundaryExclusive);
        SequenceState value = new SequenceState(start, increment, maximumCount);
        value.BoundaryValue = boundaryExclusive;
        value.HasBoundary = true;
        value.Cyclic = cyclic;
        return value;
    }

    /// <summary>
    /// Returns the next value and advances the current position.
    /// </summary>
    /// <returns>The value at the current sequence position.</returns>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    public long Next()
    {
        lock (Sync)
        {
            long value = ValueAt(Position);
            Position = Position + BigInteger.One;
            return value;
        }
    }

    /// <summary>
    /// Returns consecutive values and advances the current position.
    /// </summary>
    /// <remarks>The operation is atomic: failure does not advance the current position.</remarks>
    /// <param name="count">The number of values to return.</param>
    /// <returns>A new array of consecutive sequence values.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    public long[] Repeat(int count)
    {
        Guard.Count(count, MaximumCount, "count");
        long[] values = new long[count];
        lock (Sync)
        {
            FillAt(values, Position);
            Position = Position + count;
        }

        return values;
    }

    /// <summary>
    /// Fills an existing array with consecutive values and advances the current position.
    /// </summary>
    /// <remarks>The operation is atomic: failure leaves the destination and current position unchanged.</remarks>
    /// <param name="destination">The array to fill.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>destination</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    public void Fill(long[] destination)
    {
        Guard.NotNull(destination, "destination");
        Guard.Count(destination.Length, MaximumCount, "destination");
        long[] values = new long[destination.Length];
        lock (Sync)
        {
            FillAt(values, Position);
            int index = 0;
            while (index < destination.Length)
            {
                destination[index] = values[index];
                index = index + 1;
            }

            Position = Position + destination.Length;
        }
    }

    /// <summary>
    /// Returns a deterministic zero-based page without changing the current position.
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of values in the page.</param>
    /// <returns>A new array containing the requested page.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is negative or <c>pageSize</c> exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite page is beyond exhaustion or an unbounded value overflows.</exception>
    public long[] Page(long pageIndex, int pageSize)
    {
        if (pageIndex < 0L)
        {
            throw new ArgumentOutOfRangeException("pageIndex", pageIndex, "pageIndex must be zero or greater.");
        }

        Guard.Count(pageSize, MaximumCount, "pageSize");
        long[] values = new long[pageSize];
        BigInteger offset = new BigInteger(pageIndex) * pageSize;
        FillAt(values, offset);
        return values;
    }

    /// <summary>
    /// Returns the sequence to its initial position.
    /// </summary>
    public void Reset()
    {
        lock (Sync)
        {
            Position = BigInteger.Zero;
        }
    }

    void FillAt(long[] destination, BigInteger position)
    {
        if (destination.Length > 0)
        {
            ValueAt(position + destination.Length - 1);
        }

        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = ValueAt(position + index);
            index = index + 1;
        }
    }

    long ValueAt(BigInteger position)
    {
        BigInteger effectivePosition = position;
        if (Cyclic)
        {
            effectivePosition = position % CycleLength();
        }

        BigInteger value = new BigInteger(StartValue) + (new BigInteger(IncrementValue) * effectivePosition);
        if (value < long.MinValue || value > long.MaxValue)
        {
            throw FakeException.Create($"Sequence value overflowed at position {position}.");
        }

        if (HasBoundary)
        {
            if (IncrementValue > 0L && value >= BoundaryValue)
            {
                throw FakeException.Create($"Sequence exhausted at its exclusive boundary before position {position}.");
            }

            if (IncrementValue < 0L && value <= BoundaryValue)
            {
                throw FakeException.Create($"Sequence exhausted at its exclusive boundary before position {position}.");
            }
        }

        return (long)value;
    }

    BigInteger CycleLength()
    {
        BigInteger distance;
        BigInteger increment;
        if (IncrementValue > 0L)
        {
            distance = new BigInteger(BoundaryValue) - StartValue;
            increment = IncrementValue;
        }
        else
        {
            distance = new BigInteger(StartValue) - BoundaryValue;
            increment = -(new BigInteger(IncrementValue));
        }

        return (distance + increment - BigInteger.One) / increment;
    }

    static void ValidateIncrement(long increment)
    {
        if (increment == 0L)
        {
            throw new ArgumentOutOfRangeException("increment", increment, "increment must not be zero.");
        }
    }

    static void ValidateBoundary(long start, long increment, long boundaryExclusive)
    {
        if (increment > 0L && start >= boundaryExclusive)
        {
            throw new ArgumentOutOfRangeException("boundaryExclusive", boundaryExclusive, "boundaryExclusive must be greater than start for a positive increment.");
        }

        if (increment < 0L && start <= boundaryExclusive)
        {
            throw new ArgumentOutOfRangeException("boundaryExclusive", boundaryExclusive, "boundaryExclusive must be less than start for a negative increment.");
        }
    }
}
