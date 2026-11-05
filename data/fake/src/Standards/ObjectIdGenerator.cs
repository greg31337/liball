using System;

namespace fake;

internal static class ObjectIdGenerator
{
    internal const int ByteLength = 12;
    internal const int TextLength = 24;
    internal const long MaximumUnixSeconds = 4294967295L;

    internal static byte[] GenerateBytes(ObjectIdState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        uint seconds = UnixSeconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, seconds, value);
        return value;
    }

    internal static int WriteBytes(ObjectIdState state, IRandomSource random, DateTimeOffset timestamp, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        uint seconds = UnixSeconds(timestamp);
        RequireDestination(destination.Length, ByteLength);
        state.Next(random, seconds, destination.Slice(0, ByteLength));
        return ByteLength;
    }

    internal static string GenerateText(ObjectIdState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        uint seconds = UnixSeconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, seconds, value);
        char[] destination = new char[TextLength];
        HexIdentifierCodec.Format(value, destination);
        return new string(destination);
    }

    internal static int WriteText(ObjectIdState state, IRandomSource random, DateTimeOffset timestamp, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        uint seconds = UnixSeconds(timestamp);
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        state.Next(random, seconds, value);
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

    internal static int Format(ReadOnlySpan<byte> value, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        RequireBytes(value);
        RequireDestination(destination.Length, TextLength);
        HexIdentifierCodec.Format(value, destination);
        return TextLength;
    }

    internal static byte[] Parse(ReadOnlySpan<char> value, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireText(value);
        byte[] destination = new byte[ByteLength];
        HexIdentifierCodec.Parse(value, destination);
        return destination;
    }

    internal static int Parse(ReadOnlySpan<char> value, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireText(value);
        RequireDestination(destination.Length, ByteLength);
        HexIdentifierCodec.Parse(value, destination);
        return ByteLength;
    }

    internal static bool Validate(ReadOnlySpan<char> value)
    {
        return HexIdentifierCodec.Validate(value, TextLength);
    }

    internal static bool Validate(ReadOnlySpan<byte> value)
    {
        return value.Length == ByteLength;
    }

    internal static long UnixSeconds(ReadOnlySpan<byte> value)
    {
        RequireBytes(value);
        return ((long)value[0] << 24) | ((long)value[1] << 16) | ((long)value[2] << 8) | value[3];
    }

    internal static long UnixSeconds(ReadOnlySpan<char> value)
    {
        RequireText(value);
        byte[] bytes = new byte[ByteLength];
        HexIdentifierCodec.Parse(value, bytes);
        return UnixSeconds(bytes);
    }

    internal static void ApplyTimestamp(Span<byte> value, uint unixSeconds)
    {
        if (value.Length != ByteLength)
        {
            throw new ArgumentException("value must contain exactly 12 bytes.", "value");
        }
        value[0] = (byte)((unixSeconds >> 24) & byte.MaxValue);
        value[1] = (byte)((unixSeconds >> 16) & byte.MaxValue);
        value[2] = (byte)((unixSeconds >> 8) & byte.MaxValue);
        value[3] = (byte)(unixSeconds & byte.MaxValue);
    }

    static uint UnixSeconds(DateTimeOffset timestamp)
    {
        long value = timestamp.ToUnixTimeSeconds();
        if (value < 0L || value > MaximumUnixSeconds)
        {
            throw new ArgumentOutOfRangeException("timestamp", value, "timestamp must fit the unsigned 32-bit ObjectId-shaped Unix second field.");
        }
        return (uint)value;
    }

    static void RequireBytes(ReadOnlySpan<byte> value)
    {
        if (!Validate(value))
        {
            throw new ArgumentException("value must contain exactly 12 bytes.", "value");
        }
    }

    static void RequireText(ReadOnlySpan<char> value)
    {
        if (!Validate(value))
        {
            throw new ArgumentException("value must be exactly 24 hexadecimal characters.", "value");
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
