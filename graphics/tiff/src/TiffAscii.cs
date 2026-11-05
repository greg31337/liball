using System;

namespace tiff;

internal class TiffAscii
{
    internal static bool IsNulTerminated(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return false;
        }

        return bytes[bytes.Length - 1] == 0;
    }

    internal static byte[] EnsureNulTerminated(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return new byte[] { 0 };
        }

        if (IsNulTerminated(bytes))
        {
            return TiffTagValue.CopyByteArray(bytes);
        }

        byte[] copy = new byte[bytes.Length + 1];
        int index = 0;
        while (index < bytes.Length)
        {
            copy[index] = bytes[index];
            index = index + 1;
        }

        copy[copy.Length - 1] = 0;
        return copy;
    }

    internal static byte[] EncodeString(string text)
    {
        string[] values = new string[1];
        if (text == null)
        {
            values[0] = "";
        }
        else
        {
            values[0] = text;
        }

        return EncodeStrings(values);
    }

    internal static byte[] EncodeStrings(string[] values)
    {
        string[] normalized = CopyStrings(values);
        if (normalized.Length == 0)
        {
            return new byte[] { 0 };
        }

        int length = CalculateEncodedLength(normalized);
        byte[] bytes = new byte[length];
        int byteIndex = 0;
        int valueIndex = 0;
        while (valueIndex < normalized.Length)
        {
            string text = NormalizeString(normalized[valueIndex]);
            int charIndex = 0;
            while (charIndex < text.Length)
            {
                bytes[byteIndex] = EncodeChar(text[charIndex]);
                byteIndex = byteIndex + 1;
                charIndex = charIndex + 1;
            }

            bytes[byteIndex] = 0;
            byteIndex = byteIndex + 1;
            valueIndex = valueIndex + 1;
        }

        return bytes;
    }

    internal static int CalculateEncodedLength(string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return 1;
        }

        int length = 0;
        int index = 0;
        while (index < values.Length)
        {
            string text = NormalizeString(values[index]);
            length = length + text.Length + 1;
            index = index + 1;
        }

        return length;
    }

    internal static TiffAsciiResult Decode(byte[] bytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (bytes == null || bytes.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "ASCII tag value must contain at least one NUL terminator.");
            return TiffAsciiResult.Create(new string[0], diagnostics);
        }

        if (!IsNulTerminated(bytes))
        {
            diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "ASCII tag value is not NUL terminated.");
        }

        int stringCount = CountStrings(bytes);
        string[] values = new string[stringCount];
        int valueIndex = 0;
        int start = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == 0)
            {
                values[valueIndex] = DecodeRange(bytes, start, index - start, diagnostics);
                valueIndex = valueIndex + 1;
                start = index + 1;
            }

            index = index + 1;
        }

        if (start < bytes.Length)
        {
            values[valueIndex] = DecodeRange(bytes, start, bytes.Length - start, diagnostics);
        }

        return TiffAsciiResult.Create(values, diagnostics);
    }

    internal static int CountStrings(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == 0)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        if (!IsNulTerminated(bytes))
        {
            count = count + 1;
        }

        return count;
    }

    internal static string DecodeRange(byte[] bytes, int start, int length, TiffDiagnostics diagnostics)
    {
        char[] chars = new char[length];
        int index = 0;
        while (index < length)
        {
            byte value = bytes[start + index];
            if (value > 127)
            {
                diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "ASCII tag value contains a byte outside the 7-bit ASCII range.");
                chars[index] = '?';
            }
            else
            {
                chars[index] = (char)value;
            }

            index = index + 1;
        }

        return new string(chars);
    }

    internal static byte EncodeChar(char value)
    {
        if (value > 127)
        {
            return 63;
        }

        return (byte)value;
    }

    internal static string NormalizeString(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal static string[] CopyStrings(string[] values)
    {
        if (values == null)
        {
            return new string[0];
        }

        string[] copy = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = NormalizeString(values[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static string JoinStrings(string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return "";
        }

        string text = "";
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                text = text + "\0";
            }

            text = text + NormalizeString(values[index]);
            index = index + 1;
        }

        return text;
    }
}
