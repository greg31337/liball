using System;

namespace fake;

internal static class SortableTimestampIdGenerator
{
    internal const int ByteLength = 16;
    internal const int TextLength = 32;
    internal const long MaximumUnixMilliseconds = 281474976710655L;

    internal static byte[] GenerateBytes(SortableTimestampIdState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long milliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, milliseconds, value);
        return value;
    }

    internal static int WriteBytes(SortableTimestampIdState state, IRandomSource random, DateTimeOffset timestamp, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long milliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, ByteLength);
        state.Next(random, milliseconds, destination.Slice(0, ByteLength));
        return ByteLength;
    }

    internal static string GenerateText(SortableTimestampIdState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long milliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, milliseconds, value);
        char[] destination = new char[TextLength];
        HexIdentifierCodec.Format(value, destination);
        return new string(destination);
    }

    internal static int WriteText(SortableTimestampIdState state, IRandomSource random, DateTimeOffset timestamp, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long milliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        state.Next(random, milliseconds, value);
        HexIdentifierCodec.Format(value, destination);
        return TextLength;
    }

    internal static string Format(ReadOnlySpan<byte> value, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        RequireBytes(value);
        char[] destination = new char[TextLength];
        HexIdentifierCodec.Format(value, destination);
        return new string(destination);
    }

    internal static byte[] Parse(ReadOnlySpan<char> value, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireText(value);
        byte[] destination = new byte[ByteLength];
        HexIdentifierCodec.Parse(value, destination);
        return destination;
    }

    internal static bool Validate(ReadOnlySpan<char> value)
    {
        return HexIdentifierCodec.Validate(value, TextLength);
    }

    internal static bool Validate(ReadOnlySpan<byte> value)
    {
        return value.Length == ByteLength;
    }

    internal static long UnixMilliseconds(ReadOnlySpan<byte> value)
    {
        RequireBytes(value);
        long result = 0L;
        int index = 0;
        while (index < 6)
        {
            result = (result << 8) | value[index];
            index++;
        }
        return result;
    }

    internal static long UnixMilliseconds(ReadOnlySpan<char> value)
    {
        RequireText(value);
        byte[] bytes = new byte[ByteLength];
        HexIdentifierCodec.Parse(value, bytes);
        return UnixMilliseconds(bytes);
    }

    internal static void ApplyTimestamp(Span<byte> value, long unixMilliseconds)
    {
        RequireBytes(value);
        ValidateUnixMilliseconds(unixMilliseconds);
        value[0] = (byte)((unixMilliseconds >> 40) & byte.MaxValue);
        value[1] = (byte)((unixMilliseconds >> 32) & byte.MaxValue);
        value[2] = (byte)((unixMilliseconds >> 24) & byte.MaxValue);
        value[3] = (byte)((unixMilliseconds >> 16) & byte.MaxValue);
        value[4] = (byte)((unixMilliseconds >> 8) & byte.MaxValue);
        value[5] = (byte)(unixMilliseconds & byte.MaxValue);
    }

    internal static bool IncrementTail(Span<byte> value)
    {
        RequireBytes(value);
        int index = ByteLength - 1;
        while (index >= 6)
        {
            if (value[index] < byte.MaxValue)
            {
                value[index]++;
                return true;
            }
            value[index] = 0;
            index--;
        }
        return false;
    }

    static long UnixMilliseconds(DateTimeOffset timestamp)
    {
        long value = timestamp.ToUnixTimeMilliseconds();
        ValidateUnixMilliseconds(value);
        return value;
    }

    static void ValidateUnixMilliseconds(long value)
    {
        if (value < 0L || value > MaximumUnixMilliseconds)
        {
            throw new ArgumentOutOfRangeException("timestamp", value, "timestamp must fit the unsigned 48-bit sortable identifier Unix millisecond field.");
        }
    }

    static void RequireBytes(ReadOnlySpan<byte> value)
    {
        if (!Validate(value))
        {
            throw new ArgumentException("value must contain exactly 16 bytes.", "value");
        }
    }

    static void RequireText(ReadOnlySpan<char> value)
    {
        if (!Validate(value))
        {
            throw new ArgumentException("value must be exactly 32 hexadecimal characters.", "value");
        }
    }

    static void RequireDestination(int actualLength, int requiredLength)
    {
        if (actualLength < requiredLength)
        {
            throw new ArgumentException($"destination must contain at least {requiredLength} elements.", "destination");
        }
    }
}
