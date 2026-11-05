using System;

namespace fake;

internal class SortableTimestampIdState
{
    object Sync;
    internal bool Initialized;
    internal long LastUnixMilliseconds;
    internal byte[] LastValue;

    internal SortableTimestampIdState()
    {
        Sync = new object();
        Initialized = false;
        LastUnixMilliseconds = 0L;
        LastValue = new byte[SortableTimestampIdGenerator.ByteLength];
    }

    internal void Next(IRandomSource random, long requestedUnixMilliseconds, Span<byte> destination)
    {
        if (destination.Length != SortableTimestampIdGenerator.ByteLength)
        {
            throw new ArgumentException("destination must contain exactly 16 bytes.", "destination");
        }
        lock (Sync)
        {
            byte[] next = new byte[SortableTimestampIdGenerator.ByteLength];
            if (!Initialized || requestedUnixMilliseconds > LastUnixMilliseconds)
            {
                random.FillFromDestination(new Span<byte>(next).Slice(6, 10));
                SortableTimestampIdGenerator.ApplyTimestamp(next, requestedUnixMilliseconds);
            }
            else
            {
                new ReadOnlySpan<byte>(LastValue).CopyTo(next);
                if (!SortableTimestampIdGenerator.IncrementTail(next))
                {
                    throw FakeException.Create("Sortable timestamp identifier tail overflowed within one millisecond.");
                }
            }
            new ReadOnlySpan<byte>(next).CopyTo(destination);
            new ReadOnlySpan<byte>(next).CopyTo(LastValue);
            LastUnixMilliseconds = SortableTimestampIdGenerator.UnixMilliseconds(next);
            Initialized = true;
        }
    }
}
