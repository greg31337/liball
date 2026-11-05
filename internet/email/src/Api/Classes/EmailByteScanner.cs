using System;

namespace email;
/// <summary>
/// Provides operations for working with byte scanner.
/// </summary>
public class EmailByteScanner : IEmailByteScannerContract
{
    internal static bool IsAsciiImplementation(byte value)
    {
        return value <= 127;
    }

    /// <summary>
    /// Determines whether the current value or state is ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii; otherwise, <c>false</c>.</returns>
    public bool IsAscii(byte value)
    {
        return EmailByteScanner.IsAsciiImplementation(value);
    }

    internal static bool IsAsciiLetterImplementation(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122;
    }

    /// <summary>
    /// Determines whether the current value or state is ascii letter.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii letter; otherwise, <c>false</c>.</returns>
    public bool IsAsciiLetter(byte value)
    {
        return EmailByteScanner.IsAsciiLetterImplementation(value);
    }

    internal static bool IsAsciiDigitImplementation(byte value)
    {
        return value >= 48 && value <= 57;
    }

    /// <summary>
    /// Determines whether the current value or state is ascii digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii digit; otherwise, <c>false</c>.</returns>
    public bool IsAsciiDigit(byte value)
    {
        return EmailByteScanner.IsAsciiDigitImplementation(value);
    }

    internal static bool IsAsciiLetterOrDigitImplementation(byte value)
    {
        return IsAsciiLetterImplementation(value) || IsAsciiDigitImplementation(value);
    }

    /// <summary>
    /// Determines whether the current value or state is ascii letter or digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii letter or digit; otherwise, <c>false</c>.</returns>
    public bool IsAsciiLetterOrDigit(byte value)
    {
        return EmailByteScanner.IsAsciiLetterOrDigitImplementation(value);
    }

    internal static bool IsWspImplementation(byte value)
    {
        return value == 32 || value == 9;
    }

    /// <summary>
    /// Determines whether the current value or state is wsp.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is wsp; otherwise, <c>false</c>.</returns>
    public bool IsWsp(byte value)
    {
        return EmailByteScanner.IsWspImplementation(value);
    }

    internal static bool IsCrImplementation(byte value)
    {
        return value == 13;
    }

    /// <summary>
    /// Determines whether the current value or state is cr.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is cr; otherwise, <c>false</c>.</returns>
    public bool IsCr(byte value)
    {
        return EmailByteScanner.IsCrImplementation(value);
    }

    internal static bool IsLfImplementation(byte value)
    {
        return value == 10;
    }

    /// <summary>
    /// Determines whether the current value or state is lf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is lf; otherwise, <c>false</c>.</returns>
    public bool IsLf(byte value)
    {
        return EmailByteScanner.IsLfImplementation(value);
    }

    internal static bool IsControlImplementation(byte value)
    {
        return value <= 31 || value == 127;
    }

    /// <summary>
    /// Determines whether the current value or state is control.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is control; otherwise, <c>false</c>.</returns>
    public bool IsControl(byte value)
    {
        return EmailByteScanner.IsControlImplementation(value);
    }

    internal static bool IsVisibleAsciiImplementation(byte value)
    {
        return value >= 33 && value <= 126;
    }

    /// <summary>
    /// Determines whether the current value or state is visible ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is visible ascii; otherwise, <c>false</c>.</returns>
    public bool IsVisibleAscii(byte value)
    {
        return EmailByteScanner.IsVisibleAsciiImplementation(value);
    }

    internal static bool IsFieldNameCharacterImplementation(byte value)
    {
        return value >= 33 && value <= 126 && value != 58;
    }

    /// <summary>
    /// Determines whether the current value or state is field name character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is field name character; otherwise, <c>false</c>.</returns>
    public bool IsFieldNameCharacter(byte value)
    {
        return EmailByteScanner.IsFieldNameCharacterImplementation(value);
    }

    internal static bool IsAtextImplementation(byte value)
    {
        if (IsAsciiLetterOrDigitImplementation(value))
        {
            return true;
        }

        return value == 33 || value == 35 || value == 36 || value == 37 || value == 38 || value == 39 || value == 42 || value == 43 || value == 45 || value == 47 || value == 61 || value == 63 || value == 94 || value == 95 || value == 96 || value == 123 || value == 124 || value == 125 || value == 126;
    }

    /// <summary>
    /// Determines whether the current value or state is atext.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is atext; otherwise, <c>false</c>.</returns>
    public bool IsAtext(byte value)
    {
        return EmailByteScanner.IsAtextImplementation(value);
    }

    internal static bool IsMimeTokenCharacterImplementation(byte value)
    {
        if (!IsVisibleAsciiImplementation(value))
        {
            return false;
        }

        return value != 40 && value != 41 && value != 60 && value != 62 && value != 64 && value != 44 && value != 59 && value != 58 && value != 92 && value != 34 && value != 47 && value != 91 && value != 93 && value != 63 && value != 61;
    }

    /// <summary>
    /// Determines whether the current value or state is MIME token character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is MIME token character; otherwise, <c>false</c>.</returns>
    public bool IsMimeTokenCharacter(byte value)
    {
        return EmailByteScanner.IsMimeTokenCharacterImplementation(value);
    }

    internal static bool IsHexDigitImplementation(byte value)
    {
        return value >= 48 && value <= 57 || value >= 65 && value <= 70 || value >= 97 && value <= 102;
    }

    /// <summary>
    /// Determines whether the current value or state is hex digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is hex digit; otherwise, <c>false</c>.</returns>
    public bool IsHexDigit(byte value)
    {
        return EmailByteScanner.IsHexDigitImplementation(value);
    }

    internal static byte ToLowerAsciiImplementation(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return (byte)(value + 32);
        }

        return value;
    }

    /// <summary>
    /// Converts to the lower ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte ToLowerAscii(byte value)
    {
        return EmailByteScanner.ToLowerAsciiImplementation(value);
    }

    internal static byte ToUpperAsciiImplementation(byte value)
    {
        if (value >= 97 && value <= 122)
        {
            return (byte)(value - 32);
        }

        return value;
    }

    /// <summary>
    /// Converts to the upper ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    public byte ToUpperAscii(byte value)
    {
        return EmailByteScanner.ToUpperAsciiImplementation(value);
    }

    internal static bool EqualsAsciiIgnoreCaseImplementation(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (ToLowerAsciiImplementation(left[index]) != ToLowerAsciiImplementation(right[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Determines whether this value equals the supplied ascii ignore case.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool EqualsAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return EmailByteScanner.EqualsAsciiIgnoreCaseImplementation(left, right);
    }

    internal static int CompareAsciiIgnoreCaseImplementation(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        int length = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < length)
        {
            byte leftValue = ToLowerAsciiImplementation(left[index]);
            byte rightValue = ToLowerAsciiImplementation(right[index]);
            if (leftValue < rightValue)
            {
                return -1;
            }

            if (leftValue > rightValue)
            {
                return 1;
            }

            index++;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Compares the ascii ignore case.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    public int CompareAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return EmailByteScanner.CompareAsciiIgnoreCaseImplementation(left, right);
    }

    internal static bool StartsWithAsciiIgnoreCaseImplementation(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length > value.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (ToLowerAsciiImplementation(value[index]) != ToLowerAsciiImplementation(prefix[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Starts the s with ascii ignore case.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "prefix">The prefix used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool StartsWithAsciiIgnoreCase(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix)
    {
        return EmailByteScanner.StartsWithAsciiIgnoreCaseImplementation(value, prefix);
    }

    internal static int GetHexValueImplementation(byte value)
    {
        if (value >= 48 && value <= 57)
        {
            return value - 48;
        }

        if (value >= 65 && value <= 70)
        {
            return value - 65 + 10;
        }

        if (value >= 97 && value <= 102)
        {
            return value - 97 + 10;
        }

        return -1;
    }

    /// <summary>
    /// Gets the hex value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The current hex value.</returns>
    public int GetHexValue(byte value)
    {
        return EmailByteScanner.GetHexValueImplementation(value);
    }

    internal static int DecodeHexPairImplementation(byte high, byte low)
    {
        int highValue = GetHexValueImplementation(high);
        int lowValue = GetHexValueImplementation(low);
        if (highValue < 0 || lowValue < 0)
        {
            return -1;
        }

        return highValue * 16 + lowValue;
    }

    /// <summary>
    /// Decodes the hex pair.
    /// </summary>
    /// <param name = "high">The high used by the operation.</param>
    /// <param name = "low">The low used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public int DecodeHexPair(byte high, byte low)
    {
        return EmailByteScanner.DecodeHexPairImplementation(high, low);
    }

    internal static byte ToUpperHexImplementation(int value)
    {
        if (value < 0 || value > 15)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (value < 10)
        {
            return (byte)(48 + value);
        }

        return (byte)(65 + value - 10);
    }

    /// <summary>
    /// Converts to the upper hex.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte ToUpperHex(int value)
    {
        return EmailByteScanner.ToUpperHexImplementation(value);
    }

    internal static byte ToLowerHexImplementation(int value)
    {
        if (value < 0 || value > 15)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (value < 10)
        {
            return (byte)(48 + value);
        }

        return (byte)(97 + value - 10);
    }

    /// <summary>
    /// Converts to the lower hex.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte ToLowerHex(int value)
    {
        return EmailByteScanner.ToLowerHexImplementation(value);
    }

    internal static int FindByteImplementation(ReadOnlySpan<byte> value, byte delimiter, int startIndex)
    {
        ValidateStartIndex(value.Length, startIndex);
        int index = startIndex;
        while (index < value.Length)
        {
            if (value[index] == delimiter)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Finds the matching byte.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    public int FindByte(ReadOnlySpan<byte> value, byte delimiter, int startIndex)
    {
        return EmailByteScanner.FindByteImplementation(value, delimiter, startIndex);
    }

    internal static int FindSequenceImplementation(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex)
    {
        ValidateStartIndex(value.Length, startIndex);
        if (delimiter.Length == 0)
        {
            return startIndex;
        }

        if (delimiter.Length > value.Length - startIndex)
        {
            return -1;
        }

        int lastStart = value.Length - delimiter.Length;
        int index = startIndex;
        while (index <= lastStart)
        {
            int delimiterIndex = 0;
            while (delimiterIndex < delimiter.Length && value[index + delimiterIndex] == delimiter[delimiterIndex])
            {
                delimiterIndex++;
            }

            if (delimiterIndex == delimiter.Length)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Finds the matching sequence.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    public int FindSequence(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex)
    {
        return EmailByteScanner.FindSequenceImplementation(value, delimiter, startIndex);
    }

    internal static int FindSequenceAsciiIgnoreCaseImplementation(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex)
    {
        ValidateStartIndex(value.Length, startIndex);
        if (delimiter.Length == 0)
        {
            return startIndex;
        }

        if (delimiter.Length > value.Length - startIndex)
        {
            return -1;
        }

        int lastStart = value.Length - delimiter.Length;
        int index = startIndex;
        while (index <= lastStart)
        {
            int delimiterIndex = 0;
            while (delimiterIndex < delimiter.Length && ToLowerAsciiImplementation(value[index + delimiterIndex]) == ToLowerAsciiImplementation(delimiter[delimiterIndex]))
            {
                delimiterIndex++;
            }

            if (delimiterIndex == delimiter.Length)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Finds the matching sequence ascii ignore case.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    public int FindSequenceAsciiIgnoreCase(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex)
    {
        return EmailByteScanner.FindSequenceAsciiIgnoreCaseImplementation(value, delimiter, startIndex);
    }

    internal static int FindAnyImplementation(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiters, int startIndex)
    {
        ValidateStartIndex(value.Length, startIndex);
        int index = startIndex;
        while (index < value.Length)
        {
            int delimiterIndex = 0;
            while (delimiterIndex < delimiters.Length)
            {
                if (value[index] == delimiters[delimiterIndex])
                {
                    return index;
                }

                delimiterIndex++;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Finds the matching any.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiters">The delimiters used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    public int FindAny(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiters, int startIndex)
    {
        return EmailByteScanner.FindAnyImplementation(value, delimiters, startIndex);
    }

    static void ValidateStartIndex(int length, int startIndex)
    {
        if (startIndex < 0 || startIndex > length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }
    }
}
