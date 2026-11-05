using System;

namespace uuid;

internal class ObjectIdGenerator
{
    internal static readonly ObjectIdGenerator Shared = Create();

    internal byte[] ProcessUnique = new byte[ObjectIdLimits.ProcessUniqueByteLength];
    internal int NextCounter;

    internal static ObjectIdGenerator Create()
    {
        return Create(RandomTools.SecureSource);
    }

    internal static ObjectIdGenerator Create(IRandomSource? source)
    {
        ObjectIdGenerator generator = new ObjectIdGenerator();
        generator.ProcessUnique = RandomTools.NewBytes(ObjectIdLimits.ProcessUniqueByteLength, source);
        generator.NextCounter = NewRandomCounter(source);
        return generator;
    }

    internal static ObjectIdGenerator Create(byte[]? processUniqueBytes, int counter)
    {
        byte[] processUnique = ByteArrayTools.CopyLength(processUniqueBytes, ObjectIdLimits.ProcessUniqueByteLength);
        ThrowIfInvalidCounter(counter);
        ObjectIdGenerator generator = new ObjectIdGenerator();
        generator.ProcessUnique = processUnique;
        generator.NextCounter = counter;
        return generator;
    }

    internal static ObjectIdValue New()
    {
        return Shared.Next();
    }

    internal static ObjectIdValue NewAt(DateTimeOffset time)
    {
        return Shared.NextAt(time);
    }

    internal static ObjectIdValue NewAtUnixSeconds(long unixSeconds)
    {
        return Shared.NextAtUnixSeconds(unixSeconds);
    }

    internal ObjectIdValue Next()
    {
        return NextAt(DateTimeOffset.UtcNow);
    }

    internal ObjectIdValue NextAt(DateTimeOffset time)
    {
        return NextAtUnixSeconds(UnixTimeTools.ToUnixSeconds(time));
    }

    internal ObjectIdValue NextAtUnixSeconds(long unixSeconds)
    {
        lock (this)
        {
            int counter = NextCounter;
            NextCounter = NextCounter + 1;
            if (NextCounter > ObjectIdLimits.MaxCounter)
            {
                NextCounter = 0;
            }

            return FromParts(unixSeconds, ProcessUnique, counter);
        }
    }

    internal static ObjectIdValue FromParts(long unixSeconds, byte[]? processUniqueBytes, int counter)
    {
        ThrowIfInvalidUnixSeconds(unixSeconds);
        ThrowIfInvalidCounter(counter);
        byte[] processUnique = ByteArrayTools.CopyLength(processUniqueBytes, ObjectIdLimits.ProcessUniqueByteLength);

        byte[] bytes = new byte[ObjectIdLimits.ByteLength];
        BigEndianTools.WriteUInt32(bytes, 0, (uint)unixSeconds);

        int index = 0;
        while (index < processUnique.Length)
        {
            bytes[index + 4] = processUnique[index];
            index = index + 1;
        }

        BigEndianTools.WriteUInt24(bytes, 9, counter);
        return new ObjectIdValue(bytes);
    }

    internal static int NewRandomCounter()
    {
        return NewRandomCounter(RandomTools.SecureSource);
    }

    internal static int NewRandomCounter(IRandomSource? source)
    {
        byte[] bytes = RandomTools.NewBytes(3, source);
        return BigEndianTools.ReadUInt24(bytes, 0);
    }

    internal static void ThrowIfInvalidUnixSeconds(long unixSeconds)
    {
        if (unixSeconds < 0 || unixSeconds > ObjectIdLimits.MaxUnixSeconds)
        {
            throw new ArgumentOutOfRangeException("unixSeconds");
        }
    }

    internal static void ThrowIfInvalidCounter(int counter)
    {
        if (counter < 0 || counter > ObjectIdLimits.MaxCounter)
        {
            throw new ArgumentOutOfRangeException("counter");
        }
    }
}
