using System;

namespace fake;

internal static class UlidGenerator
{
    internal const int ByteLength = 16;
    internal const int TextLength = 26;
    internal const long MaximumUnixMilliseconds = 281474976710655L;
    const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    const string MaximumText = "7ZZZZZZZZZZZZZZZZZZZZZZZZZ";

    internal static byte[] GenerateRandomBytes(IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        FillRandomValue(random, unixMilliseconds, value);
        return value;
    }

    internal static int WriteRandomBytes(IRandomSource random, DateTimeOffset timestamp, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, ByteLength);
        FillRandomValue(random, unixMilliseconds, destination.Slice(0, ByteLength));
        return ByteLength;
    }

    internal static string GenerateRandomText(IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        FillRandomValue(random, unixMilliseconds, value);
        char[] destination = new char[TextLength];
        FormatUnchecked(value, destination);
        return new string(destination);
    }

    internal static int WriteRandomText(IRandomSource random, DateTimeOffset timestamp, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        FillRandomValue(random, unixMilliseconds, value);
        FormatUnchecked(value, destination);
        return TextLength;
    }

    internal static byte[] GenerateMonotonicBytes(UlidMonotonicState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        return value;
    }

    internal static int WriteMonotonicBytes(UlidMonotonicState state, IRandomSource random, DateTimeOffset timestamp, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, ByteLength);
        state.Next(random, unixMilliseconds, destination.Slice(0, ByteLength));
        return ByteLength;
    }

    internal static string GenerateMonotonicText(UlidMonotonicState state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        char[] destination = new char[TextLength];
        FormatUnchecked(value, destination);
        return new string(destination);
    }

    internal static int WriteMonotonicText(UlidMonotonicState state, IRandomSource random, DateTimeOffset timestamp, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        FormatUnchecked(value, destination);
        return TextLength;
    }

    internal static string Maximum(int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        return MaximumText;
    }

    internal static byte[] MaximumBytes(int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        byte[] value = new byte[ByteLength];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = byte.MaxValue;
            index++;
        }
        return value;
    }

    internal static string Format(ReadOnlySpan<byte> value, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        RequireBytes(value);
        char[] destination = new char[TextLength];
        FormatUnchecked(value, destination);
        return new string(destination);
    }

    internal static int Format(ReadOnlySpan<byte> value, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        RequireBytes(value);
        RequireDestination(destination.Length, TextLength);
        FormatUnchecked(value, destination);
        return TextLength;
    }

    internal static byte[] Parse(ReadOnlySpan<char> value, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireText(value);
        byte[] destination = new byte[ByteLength];
        ParseUnchecked(value, destination);
        return destination;
    }

    internal static int Parse(ReadOnlySpan<char> value, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireText(value);
        RequireDestination(destination.Length, ByteLength);
        ParseUnchecked(value, destination);
        return ByteLength;
    }

    internal static bool Validate(ReadOnlySpan<char> value)
    {
        if (value.Length != TextLength)
        {
            return false;
        }
        int first = DecodeValue(value[0]);
        if (first < 0 || first > 7)
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (DecodeValue(value[index]) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
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
        ParseUnchecked(value, bytes);
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

    internal static bool IncrementRandomness(Span<byte> value)
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

    static void FillRandomValue(IRandomSource random, long unixMilliseconds, Span<byte> value)
    {
        random.FillFromDestination(value.Slice(6, 10));
        ApplyTimestamp(value, unixMilliseconds);
    }

    static void FormatUnchecked(ReadOnlySpan<byte> value, Span<char> destination)
    {
        int accumulator = 0;
        int availableBits = 2;
        int byteIndex = 0;
        int textIndex = 0;
        while (byteIndex < ByteLength)
        {
            accumulator = (accumulator << 8) | value[byteIndex];
            availableBits += 8;
            while (availableBits >= 5)
            {
                availableBits -= 5;
                destination[textIndex] = Alphabet[(accumulator >> availableBits) & 31];
                textIndex++;
            }
            byteIndex++;
        }
    }

    static void ParseUnchecked(ReadOnlySpan<char> value, Span<byte> destination)
    {
        int accumulator = DecodeValue(value[0]);
        int availableBits = 3;
        int textIndex = 1;
        int byteIndex = 0;
        while (textIndex < TextLength)
        {
            accumulator = (accumulator << 5) | DecodeValue(value[textIndex]);
            availableBits += 5;
            if (availableBits >= 8)
            {
                availableBits -= 8;
                destination[byteIndex] = (byte)((accumulator >> availableBits) & byte.MaxValue);
                byteIndex++;
            }
            textIndex++;
        }
    }

    static int DecodeValue(char value)
    {
        char canonical = value;
        if (canonical >= 'a' && canonical <= 'z')
        {
            canonical = (char)(canonical - 'a' + 'A');
        }
        return Alphabet.IndexOf(canonical);
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
            throw new ArgumentOutOfRangeException("timestamp", value, "timestamp must fit the unsigned 48-bit ULID Unix millisecond field.");
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
            throw new ArgumentException("value must be a valid 26-character ULID.", "value");
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
