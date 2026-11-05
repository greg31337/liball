using System;

namespace fake;

internal class ObjectIdState
{
    object Sync;
    internal bool Initialized;
    internal byte[] ProcessValue;
    internal int CounterValue;

    internal ObjectIdState()
    {
        Sync = new object();
        Initialized = false;
        ProcessValue = new byte[5];
        CounterValue = 0;
    }

    internal void Next(IRandomSource random, uint unixSeconds, Span<byte> destination)
    {
        if (destination.Length != ObjectIdGenerator.ByteLength)
        {
            throw new ArgumentException("destination must contain exactly 12 bytes.", "destination");
        }
        lock (Sync)
        {
            if (!Initialized)
            {
                byte[] seed = new byte[8];
                random.FillFromDestination(seed);
                new ReadOnlySpan<byte>(seed).Slice(0, 5).CopyTo(ProcessValue);
                CounterValue = (seed[5] << 16) | (seed[6] << 8) | seed[7];
                Initialized = true;
            }
            ObjectIdGenerator.ApplyTimestamp(destination, unixSeconds);
            new ReadOnlySpan<byte>(ProcessValue).CopyTo(destination.Slice(4, 5));
            destination[9] = (byte)((CounterValue >> 16) & byte.MaxValue);
            destination[10] = (byte)((CounterValue >> 8) & byte.MaxValue);
            destination[11] = (byte)(CounterValue & byte.MaxValue);
            CounterValue = (CounterValue + 1) & 16777215;
        }
    }
}
