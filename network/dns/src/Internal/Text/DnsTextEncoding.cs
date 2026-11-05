using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace dns;

internal static class DnsTextEncoding
{
    internal static string FormatHex(byte[] data)
    {
        const string Digits = "0123456789ABCDEF";
        StringBuilder builder = new StringBuilder(data.Length * 2);
        int index = 0;
        while (index < data.Length)
        {
            byte value = data[index];
            builder.Append(Digits[value >> 4]);
            builder.Append(Digits[value & 15]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string FormatBase64(byte[] data)
    {
        return Convert.ToBase64String(data);
    }

    internal static string FormatBase32Hex(byte[] data)
    {
        const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        StringBuilder builder = new StringBuilder();
        int accumulator = 0;
        int bitCount = 0;
        int index = 0;
        while (index < data.Length)
        {
            accumulator = (accumulator << 8) | data[index];
            bitCount = bitCount + 8;
            while (bitCount >= 5)
            {
                bitCount = bitCount - 5;
                builder.Append(Digits[(accumulator >> bitCount) & 31]);
            }

            accumulator = accumulator & ((1 << bitCount) - 1);
            index = index + 1;
        }

        if (bitCount > 0)
        {
            builder.Append(Digits[(accumulator << (5 - bitCount)) & 31]);
        }

        return builder.ToString();
    }

    internal static string FormatCharacterString(byte[] data)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int index = 0;
        while (index < data.Length)
        {
            byte value = data[index];
            if (value == '"' || value == '\\')
            {
                builder.Append('\\');
                builder.Append((char)value);
            }
            else if (value < 32 || value > 126)
            {
                builder.Append('\\');
                builder.Append((char)('0' + (value / 100)));
                builder.Append((char)('0' + ((value / 10) % 10)));
                builder.Append((char)('0' + (value % 10)));
            }
            else
            {
                builder.Append((char)value);
            }

            index = index + 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    internal static string FormatAddress(byte[] address)
    {
        if (address.Length != 4 && address.Length != 16)
        {
            return string.Empty;
        }

        return new IPAddress(address).ToString();
    }

    internal static string FormatUnsigned(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatSigned(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatUnsignedLong(ulong value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDnsTime(uint value)
    {
        return DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime.ToString(
            "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }
}
