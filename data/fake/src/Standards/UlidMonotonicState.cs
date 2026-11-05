using System;

namespace fake;

internal class UlidMonotonicState
{
    object Sync;
    internal bool Initialized;
    internal long LastUnixMilliseconds;
    internal byte[] LastValue;

    internal UlidMonotonicState()
    {
        Sync = new object();
        Initialized = false;
        LastUnixMilliseconds = 0L;
        LastValue = new byte[UlidGenerator.ByteLength];
    }

    internal void Next(IRandomSource random, long requestedUnixMilliseconds, Span<byte> destination)
    {
        if (destination.Length != UlidGenerator.ByteLength)
        {
            throw new ArgumentException("destination must contain exactly 16 bytes.", "destination");
        }
        lock (Sync)
        {
            byte[] next = new byte[UlidGenerator.ByteLength];
            if (!Initialized || requestedUnixMilliseconds > LastUnixMilliseconds)
            {
                random.FillFromDestination(new Span<byte>(next).Slice(6, 10));
                UlidGenerator.ApplyTimestamp(next, requestedUnixMilliseconds);
            }
            else
            {
                new ReadOnlySpan<byte>(LastValue).CopyTo(next);
                if (!UlidGenerator.IncrementRandomness(next))
                {
                    throw FakeException.Create("Monotonic ULID randomness overflowed within one millisecond.");
                }
            }
            new ReadOnlySpan<byte>(next).CopyTo(destination);
            new ReadOnlySpan<byte>(next).CopyTo(LastValue);
            LastUnixMilliseconds = UlidGenerator.UnixMilliseconds(next);
            Initialized = true;
        }
    }
}
