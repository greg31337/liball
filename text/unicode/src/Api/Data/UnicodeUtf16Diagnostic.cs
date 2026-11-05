namespace unicode;

/// <summary>
/// Describes one malformed UTF-16 code unit found while reading text.
/// </summary>
public class UnicodeUtf16Diagnostic
{
    /// <summary>
    /// The zero-based UTF-16 code-unit index in the original string.
    /// </summary>
    public int Utf16Index;
    /// <summary>
    /// The length of the range in UTF-16 code units.
    /// </summary>
    public int Utf16Length;
    /// <summary>
    /// The malformed UTF-16 code unit, stored as an integer.
    /// </summary>
    public int CodeUnit;
    /// <summary>
    /// The stable machine-readable rule identifier.
    /// </summary>
    public string Rule;
    /// <summary>
    /// A plain-English explanation of the diagnostic.
    /// </summary>
    public string Message;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="utf16Index">The zero-based UTF-16 code-unit index to test.</param>
    /// <param name="utf16Length">The length in UTF-16 code units.</param>
    /// <param name="codeUnit">The UTF-16 code unit to inspect.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    public UnicodeUtf16Diagnostic(
        int utf16Index,
        int utf16Length,
        int codeUnit,
        string rule,
        string message)
    {
        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodeUnit = codeUnit;
        Rule = rule;
        Message = message;
    }
}
