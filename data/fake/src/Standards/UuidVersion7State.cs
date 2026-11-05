using System;

namespace fake;

internal class UuidVersion7State
{
    internal const long MaximumUnixMilliseconds = 281474976710655L;
    object Sync;
    bool Initialized;
    long LastUnixMilliseconds;
    byte[] LastValue;

    internal UuidVersion7State()
    {
        Sync = new object();
        Initialized = false;
        LastUnixMilliseconds = 0L;
        LastValue = new byte[UuidGenerator.ByteLength];
    }

    internal void Next(IRandomSource random, long requestedUnixMilliseconds, Span<byte> destination)
    {
        if (destination.Length != UuidGenerator.ByteLength)
        {
            throw new ArgumentException("destination must contain exactly 16 bytes.", "destination");
        }
        lock (Sync)
        {
            if (!Initialized || requestedUnixMilliseconds > LastUnixMilliseconds)
            {
                InitializeValue(random, requestedUnixMilliseconds, destination);
            }
            else
            {
                new ReadOnlySpan<byte>(LastValue).CopyTo(destination);
                bool incremented = UuidGenerator.IncrementVersion7Counter(destination);
                if (!incremented)
                {
                    if (LastUnixMilliseconds == MaximumUnixMilliseconds)
                    {
                        throw FakeException.Create("UUID version 7 monotonic counter exhausted at the maximum timestamp.");
                    }
                    InitializeValue(random, LastUnixMilliseconds + 1L, destination);
                }
                else
                {
                    random.FillFromDestination(destination.Slice(12, 4));
                }
            }
            LastUnixMilliseconds = UuidGenerator.Version7UnixMilliseconds(destination);
            destination.CopyTo(LastValue);
            Initialized = true;
        }
    }

    void InitializeValue(IRandomSource random, long unixMilliseconds, Span<byte> destination)
    {
        random.FillFromDestination(destination.Slice(6, 10));
        UuidGenerator.ApplyVersion7(destination, unixMilliseconds);
        destination[6] = (byte)(destination[6] & 247);
    }
}
