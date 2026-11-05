namespace unicode;

/// <summary>
/// Describes one problem found while preparing or validating a PRECIS-style string.
/// </summary>
public class UnicodePrecisDiagnostic
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
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePrecisStatus Status;
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
    /// <param name="status">The validation status.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <exception cref="System.ArgumentException">Thrown when any of <c>rule</c>, <c>message</c> is empty or consists only of white space.</exception>
    public UnicodePrecisDiagnostic(
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodePrecisStatus status,
        string rule,
        string message)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodePoint = codePoint;
        Status = status;
        Rule = rule;
        Message = message;
    }
}
