namespace unicode;

/// <summary>
/// Describes one decoded Unicode scalar and its location in the original UTF-16 string.
/// </summary>
public class UnicodeScalarRecord
{
    /// <summary>
    /// The Unicode scalar value associated with this record.
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// The zero-based UTF-16 code-unit index in the original string.
    /// </summary>
    public int Utf16Index;
    /// <summary>
    /// The length of the range in UTF-16 code units.
    /// </summary>
    public int Utf16Length;
    /// <summary>
    /// Whether the scalar is U+FFFD produced for malformed UTF-16 input.
    /// </summary>
    public bool IsReplacement;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <param name="utf16Index">The zero-based UTF-16 code-unit index to test.</param>
    /// <param name="utf16Length">The length in UTF-16 code units.</param>
    /// <param name="isReplacement">Whether the record represents replacement of malformed UTF-16.</param>
    public UnicodeScalarRecord(int codePoint, int utf16Index, int utf16Length, bool isReplacement)
    {
        CodePoint = codePoint;
        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        IsReplacement = isReplacement;
    }
}
