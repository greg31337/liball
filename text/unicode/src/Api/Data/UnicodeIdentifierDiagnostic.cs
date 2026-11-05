namespace unicode;

/// <summary>
/// Describes one problem found while validating a Unicode identifier.
/// </summary>
public class UnicodeIdentifierDiagnostic
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
    /// The Unicode General_Category value for the associated scalar, or Other_Not_Assigned when no scalar applies.
    /// </summary>
    public UnicodeGeneralCategory GeneralCategory;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodeIdentifierStatus Status;
    /// <summary>
    /// The severity of the diagnostic.
    /// </summary>
    public UnicodeDiagnosticSeverity Severity;
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
    /// <param name="generalCategory">The Unicode General_Category value, or Other_Not_Assigned when no scalar applies.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    public UnicodeIdentifierDiagnostic(
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodeGeneralCategory generalCategory,
        UnicodeIdentifierStatus status,
        UnicodeDiagnosticSeverity severity,
        string rule,
        string message)
    {
        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodePoint = codePoint;
        GeneralCategory = generalCategory;
        Status = status;
        Severity = severity;
        Rule = rule;
        Message = message;
    }
}
