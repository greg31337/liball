namespace unicode;

/// <summary>
/// Describes one potentially unsafe Unicode scalar found by a security check.
/// </summary>
public class UnicodeSecurityDiagnostic
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
    /// The Unicode scalar value associated with this record.
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// The security condition represented by this diagnostic.
    /// </summary>
    public UnicodeSecurityFlag Flag;
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
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <param name="flag">The security condition represented by the diagnostic.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    public UnicodeSecurityDiagnostic(
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodeSecurityFlag flag,
        string rule,
        string message)
    {
        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodePoint = codePoint;
        Flag = flag;
        Rule = rule;
        Message = message;
    }
}
