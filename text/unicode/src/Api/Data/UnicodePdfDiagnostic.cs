namespace unicode;

/// <summary>
/// Describes one Unicode-related PDF text or font-mapping problem.
/// </summary>
public class UnicodePdfDiagnostic
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
    /// The zero-based position of the related glyph mapping, or <c>-1</c> when no mapping applies.
    /// </summary>
    public int MappingIndex;
    /// <summary>
    /// The PDF character code associated with this mapping or diagnostic.
    /// </summary>
    public int CharacterCode;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePdfDiagnosticStatus Status;
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
    /// <param name="mappingIndex">The zero-based mapping index, or <c>-1</c> when no mapping applies.</param>
    /// <param name="characterCode">The PDF character code.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <exception cref="System.ArgumentException">Thrown when any of <c>rule</c>, <c>message</c> is empty or consists only of white space.</exception>
    public UnicodePdfDiagnostic(
        int utf16Index,
        int utf16Length,
        int codePoint,
        int mappingIndex,
        int characterCode,
        UnicodePdfDiagnosticStatus status,
        string rule,
        string message)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        CodePoint = codePoint;
        MappingIndex = mappingIndex;
        CharacterCode = characterCode;
        Status = status;
        Rule = rule;
        Message = message;
    }
}
