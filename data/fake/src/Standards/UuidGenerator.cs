using System;

namespace fake;

internal static class UuidGenerator
{
    internal const int ByteLength = 16;
    internal const int TextLength = 36;
    const string HexAlphabet = "0123456789abcdef";
    const string NilText = "00000000-0000-0000-0000-000000000000";
    const string MaxText = "ffffffff-ffff-ffff-ffff-ffffffffffff";

    internal static byte[] GenerateVersion4Bytes(IRandomSource random, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        byte[] value = new byte[ByteLength];
        random.FillFromDestination(value);
        ApplyVersion4(value);
        return value;
    }

    internal static int WriteVersion4Bytes(IRandomSource random, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        RequireDestination(destination.Length, ByteLength);
        Span<byte> value = destination.Slice(0, ByteLength);
        random.FillFromDestination(value);
        ApplyVersion4(value);
        return ByteLength;
    }

    internal static string GenerateVersion4Text(IRandomSource random, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        byte[] value = new byte[ByteLength];
        random.FillFromDestination(value);
        ApplyVersion4(value);
        char[] destination = new char[TextLength];
        FormatUnchecked(value, destination);
        return new string(destination);
    }

    internal static int WriteVersion4Text(IRandomSource random, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        random.FillFromDestination(value);
        ApplyVersion4(value);
        FormatUnchecked(value, destination);
        return TextLength;
    }

    internal static byte[] GenerateVersion7Bytes(UuidVersion7State state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        return value;
    }

    internal static int WriteVersion7Bytes(UuidVersion7State state, IRandomSource random, DateTimeOffset timestamp, Span<byte> destination, int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, ByteLength);
        state.Next(random, unixMilliseconds, destination.Slice(0, ByteLength));
        return ByteLength;
    }

    internal static string GenerateVersion7Text(UuidVersion7State state, IRandomSource random, DateTimeOffset timestamp, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        char[] destination = new char[TextLength];
        FormatUnchecked(value, destination);
        return new string(destination);
    }

    internal static int WriteVersion7Text(UuidVersion7State state, IRandomSource random, DateTimeOffset timestamp, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        long unixMilliseconds = UnixMilliseconds(timestamp);
        RequireDestination(destination.Length, TextLength);
        byte[] value = new byte[ByteLength];
        state.Next(random, unixMilliseconds, value);
        FormatUnchecked(value, destination);
        return TextLength;
    }

    internal static string Nil(int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        return NilText;
    }

    internal static byte[] NilBytes(int maximumOutputSize)
    {
        Guard.OutputSize(ByteLength, maximumOutputSize, "value");
        return new byte[ByteLength];
    }

    internal static string Max(int maximumOutputSize)
    {
        Guard.OutputSize(TextLength, maximumOutputSize, "value");
        return MaxText;
    }

    internal static byte[] MaxBytes(int maximumOutputSize)
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

        int index = 0;
        while (index < value.Length)
        {
            bool dashPosition = index == 8 || index == 13 || index == 18 || index == 23;
            if (dashPosition)
            {
                if (value[index] != '-')
                {
                    return false;
                }
            }
            else if (HexValue(value[index]) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ValidateVersion4(ReadOnlySpan<char> value)
    {
        if (!Validate(value))
        {
            return false;
        }
        if (value[14] != '4')
        {
            return false;
        }
        int variant = HexValue(value[19]);
        return variant >= 8 && variant <= 11;
    }

    internal static bool ValidateVersion4(ReadOnlySpan<byte> value)
    {
        if (value.Length != ByteLength)
        {
            return false;
        }
        return (value[6] & 240) == 64 && (value[8] & 192) == 128;
    }

    internal static bool ValidateVersion7(ReadOnlySpan<char> value)
    {
        if (!Validate(value))
        {
            return false;
        }
        if (value[14] != '7')
        {
            return false;
        }
        int variant = HexValue(value[19]);
        return variant >= 8 && variant <= 11;
    }

    internal static bool ValidateVersion7(ReadOnlySpan<byte> value)
    {
        if (value.Length != ByteLength)
        {
            return false;
        }
        return (value[6] & 240) == 112 && (value[8] & 192) == 128;
    }

    internal static void ApplyVersion4(Span<byte> value)
    {
        RequireBytes(value);
        value[6] = (byte)((value[6] & 15) | 64);
        value[8] = (byte)((value[8] & 63) | 128);
    }

    internal static void ApplyVersion7(Span<byte> value, long unixMilliseconds)
    {
        RequireBytes(value);
        ValidateUnixMilliseconds(unixMilliseconds);
        value[0] = (byte)((unixMilliseconds >> 40) & byte.MaxValue);
        value[1] = (byte)((unixMilliseconds >> 32) & byte.MaxValue);
        value[2] = (byte)((unixMilliseconds >> 24) & byte.MaxValue);
        value[3] = (byte)((unixMilliseconds >> 16) & byte.MaxValue);
        value[4] = (byte)((unixMilliseconds >> 8) & byte.MaxValue);
        value[5] = (byte)(unixMilliseconds & byte.MaxValue);
        value[6] = (byte)((value[6] & 15) | 112);
        value[8] = (byte)((value[8] & 63) | 128);
    }

    internal static long Version7UnixMilliseconds(ReadOnlySpan<byte> value)
    {
        if (!ValidateVersion7(value))
        {
            throw new ArgumentException("value must contain an RFC-variant UUID version 7.", "value");
        }
        long result = 0L;
        int index = 0;
        while (index < 6)
        {
            result = (result << 8) | value[index];
            index++;
        }
        return result;
    }

    internal static bool IncrementVersion7Counter(Span<byte> value)
    {
        int index = 11;
        while (index >= 6)
        {
            if (index == 8)
            {
                int current = value[index] & 63;
                if (current < 63)
                {
                    value[index] = (byte)(128 | current + 1);
                    return true;
                }
                value[index] = 128;
            }
            else if (index == 6)
            {
                int current = value[index] & 15;
                if (current < 15)
                {
                    value[index] = (byte)(112 | current + 1);
                    return true;
                }
                value[index] = 112;
            }
            else if (value[index] < byte.MaxValue)
            {
                value[index]++;
                return true;
            }
            else
            {
                value[index] = 0;
            }
            index--;
        }
        return false;
    }

    static void FormatUnchecked(ReadOnlySpan<byte> value, Span<char> destination)
    {
        int byteIndex = 0;
        int textIndex = 0;
        while (byteIndex < ByteLength)
        {
            if (byteIndex == 4 || byteIndex == 6 || byteIndex == 8 || byteIndex == 10)
            {
                destination[textIndex] = '-';
                textIndex++;
            }
            byte current = value[byteIndex];
            destination[textIndex] = HexAlphabet[current >> 4];
            destination[textIndex + 1] = HexAlphabet[current & 15];
            textIndex += 2;
            byteIndex++;
        }
    }

    static void ParseUnchecked(ReadOnlySpan<char> value, Span<byte> destination)
    {
        int byteIndex = 0;
        int textIndex = 0;
        while (byteIndex < ByteLength)
        {
            if (textIndex == 8 || textIndex == 13 || textIndex == 18 || textIndex == 23)
            {
                textIndex++;
            }
            int high = HexValue(value[textIndex]);
            int low = HexValue(value[textIndex + 1]);
            destination[byteIndex] = (byte)((high << 4) | low);
            textIndex += 2;
            byteIndex++;
        }
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    static long UnixMilliseconds(DateTimeOffset timestamp)
    {
        long value = timestamp.ToUnixTimeMilliseconds();
        ValidateUnixMilliseconds(value);
        return value;
    }

    static void ValidateUnixMilliseconds(long value)
    {
        if (value < 0L || value > 281474976710655L)
        {
            throw new ArgumentOutOfRangeException("timestamp", value, "timestamp must fit the unsigned 48-bit UUID version 7 Unix millisecond field.");
        }
    }

    static void RequireBytes(ReadOnlySpan<byte> value)
    {
        if (value.Length != ByteLength)
        {
            throw new ArgumentException("value must contain exactly 16 bytes.", "value");
        }
    }

    static void RequireText(ReadOnlySpan<char> value)
    {
        if (!Validate(value))
        {
            throw new ArgumentException("value must be a canonical 36-character UUID.", "value");
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
