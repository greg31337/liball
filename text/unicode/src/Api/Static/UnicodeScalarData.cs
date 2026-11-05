namespace unicode;

/// <summary>
/// Provides Unicode scalar and UTF-16 constants.
/// </summary>
public static class UnicodeScalarData
{
    /// <summary>
    /// The lowest possible Unicode code point, U+0000.
    /// </summary>
    public const int MinCodePoint = UnicodeScalar.MinCodePoint;
    /// <summary>
    /// The highest possible Unicode code point, U+10FFFF.
    /// </summary>
    public const int MaxCodePoint = UnicodeScalar.MaxCodePoint;
    /// <summary>
    /// The first UTF-16 high-surrogate code unit, U+D800.
    /// </summary>
    public const int HighSurrogateStart = UnicodeScalar.HighSurrogateStart;
    /// <summary>
    /// The last UTF-16 high-surrogate code unit, U+DBFF.
    /// </summary>
    public const int HighSurrogateEnd = UnicodeScalar.HighSurrogateEnd;
    /// <summary>
    /// The first UTF-16 low-surrogate code unit, U+DC00.
    /// </summary>
    public const int LowSurrogateStart = UnicodeScalar.LowSurrogateStart;
    /// <summary>
    /// The last UTF-16 low-surrogate code unit, U+DFFF.
    /// </summary>
    public const int LowSurrogateEnd = UnicodeScalar.LowSurrogateEnd;
    /// <summary>
    /// The Unicode replacement character, U+FFFD.
    /// </summary>
    public const int ReplacementCharacter = UnicodeScalar.ReplacementCharacter;
    /// <summary>
    /// The largest integer that fits in one UTF-16 code unit.
    /// </summary>
    public const int MaxUtf16CodeUnit = UnicodeScalar.MaxUtf16CodeUnit;
}

