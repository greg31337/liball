namespace unicode;

/// <summary>
/// Provides constants, validation, and UTF-16 conversion for Unicode scalar values.
/// </summary>
public class UnicodeScalar : IUnicodeScalar
{
    internal static readonly UnicodeScalar Instance = new UnicodeScalar();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeScalar()
    {
    }
    /// <summary>
    /// The lowest possible Unicode code point, U+0000.
    /// </summary>
    internal const int MinCodePoint = 0x000000;
    /// <summary>
    /// The highest possible Unicode code point, U+10FFFF.
    /// </summary>
    internal const int MaxCodePoint = 0x10FFFF;
    /// <summary>
    /// The first UTF-16 high-surrogate code unit, U+D800.
    /// </summary>
    internal const int HighSurrogateStart = 0xD800;
    /// <summary>
    /// The last UTF-16 high-surrogate code unit, U+DBFF.
    /// </summary>
    internal const int HighSurrogateEnd = 0xDBFF;
    /// <summary>
    /// The first UTF-16 low-surrogate code unit, U+DC00.
    /// </summary>
    internal const int LowSurrogateStart = 0xDC00;
    /// <summary>
    /// The last UTF-16 low-surrogate code unit, U+DFFF.
    /// </summary>
    internal const int LowSurrogateEnd = 0xDFFF;
    /// <summary>
    /// The Unicode replacement character, U+FFFD.
    /// </summary>
    internal const int ReplacementCharacter = 0xFFFD;
    /// <summary>
    /// The largest integer that fits in one UTF-16 code unit.
    /// </summary>
    internal const int MaxUtf16CodeUnit = 0xFFFF;

    /// <summary>
    /// Reports whether a code point is a Unicode scalar value.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsValid(int codePoint)
    {
        if (codePoint < MinCodePoint)
        {
            return false;
        }

        if (codePoint > MaxCodePoint)
        {
            return false;
        }

        if (IsSurrogate(codePoint))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reports whether a value is within the Unicode range and is not a surrogate.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsScalarValue(int codePoint)
    {
        return IsValid(codePoint);
    }

    /// <summary>
    /// Reports whether a value is a valid non-surrogate scalar in the Basic Multilingual Plane.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsBmpScalar(int codePoint)
    {
        if (!IsValid(codePoint))
        {
            return false;
        }

        return codePoint <= MaxUtf16CodeUnit;
    }

    /// <summary>
    /// Reports whether a value is a valid Unicode scalar above the Basic Multilingual Plane.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsSupplementaryScalar(int codePoint)
    {
        if (!IsValid(codePoint))
        {
            return false;
        }

        return codePoint > MaxUtf16CodeUnit;
    }

    /// <summary>
    /// Reports whether an integer fits in one UTF-16 code unit.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsValidCodeUnit(int codeUnit)
    {
        if (codeUnit < 0)
        {
            return false;
        }

        return codeUnit <= MaxUtf16CodeUnit;
    }

    /// <summary>
    /// Reports whether an integer is in either UTF-16 surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsSurrogate(int codeUnit)
    {
        if (codeUnit < HighSurrogateStart)
        {
            return false;
        }

        return codeUnit <= LowSurrogateEnd;
    }

    /// <summary>
    /// Reports whether an integer is in the UTF-16 high-surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsHighSurrogate(int codeUnit)
    {
        if (codeUnit < HighSurrogateStart)
        {
            return false;
        }

        return codeUnit <= HighSurrogateEnd;
    }

    /// <summary>
    /// Reports whether an integer is in the UTF-16 low-surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsLowSurrogate(int codeUnit)
    {
        if (codeUnit < LowSurrogateStart)
        {
            return false;
        }

        return codeUnit <= LowSurrogateEnd;
    }

    /// <summary>
    /// Reports whether two UTF-16 code units form a valid surrogate pair.
    /// </summary>
    /// <param name="highCodeUnit">The candidate high-surrogate code unit.</param>
    /// <param name="lowCodeUnit">The candidate low-surrogate code unit.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsValidSurrogatePair(int highCodeUnit, int lowCodeUnit)
    {
        if (!IsHighSurrogate(highCodeUnit))
        {
            return false;
        }

        return IsLowSurrogate(lowCodeUnit);
    }

    /// <summary>
    /// Encodes one Unicode scalar as a one- or two-code-unit .NET string.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>codePoint</c> is outside the supported range.</exception>
    public string ToUtf16String(int codePoint)
    {
        if (!IsValid(codePoint))
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }

        return char.ConvertFromUtf32(codePoint);
    }
}
