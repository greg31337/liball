namespace unicode;

/// <summary>
/// Provides Unicode scalar and UTF-16 code-unit operations.
/// </summary>
public interface IUnicodeScalar
{
    /// <summary>
    /// Reports whether a code point is a Unicode scalar value.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsValid(int codePoint);

    /// <summary>
    /// Reports whether a value is within the Unicode range and is not a surrogate.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsScalarValue(int codePoint);

    /// <summary>
    /// Reports whether a value is a valid non-surrogate scalar in the Basic Multilingual Plane.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsBmpScalar(int codePoint);

    /// <summary>
    /// Reports whether a value is a valid Unicode scalar above the Basic Multilingual Plane.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsSupplementaryScalar(int codePoint);

    /// <summary>
    /// Reports whether an integer fits in one UTF-16 code unit.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsValidCodeUnit(int codeUnit);

    /// <summary>
    /// Reports whether an integer is in either UTF-16 surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsSurrogate(int codeUnit);

    /// <summary>
    /// Reports whether an integer is in the UTF-16 high-surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsHighSurrogate(int codeUnit);

    /// <summary>
    /// Reports whether an integer is in the UTF-16 low-surrogate range.
    /// </summary>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsLowSurrogate(int codeUnit);

    /// <summary>
    /// Reports whether two UTF-16 code units form a valid surrogate pair.
    /// </summary>
    /// <param name="highCodeUnit">The candidate high-surrogate code unit.</param>
    /// <param name="lowCodeUnit">The candidate low-surrogate code unit.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    bool IsValidSurrogatePair(int highCodeUnit, int lowCodeUnit);

    /// <summary>
    /// Encodes one Unicode scalar as a one- or two-code-unit .NET string.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>codePoint</c> is outside the supported range.</exception>
    string ToUtf16String(int codePoint);
}

