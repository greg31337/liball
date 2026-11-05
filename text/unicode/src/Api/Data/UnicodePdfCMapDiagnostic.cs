namespace unicode;

/// <summary>
/// Describes one problem in a PDF ToUnicode glyph mapping.
/// </summary>
public class UnicodePdfCMapDiagnostic
{
    /// <summary>
    /// The zero-based position of the related glyph mapping, or <c>-1</c> when no mapping applies.
    /// </summary>
    public int MappingIndex;
    /// <summary>
    /// The PDF character code associated with this mapping or diagnostic.
    /// </summary>
    public int CharacterCode;
    /// <summary>
    /// The Unicode scalar value associated with this record.
    /// </summary>
    public int CodePoint;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePdfCMapStatus Status;
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
    /// <param name="mappingIndex">The zero-based mapping index, or <c>-1</c> when no mapping applies.</param>
    /// <param name="characterCode">The PDF character code.</param>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="rule">The stable diagnostic rule identifier.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <exception cref="System.ArgumentException">Thrown when any of <c>rule</c>, <c>message</c> is empty or consists only of white space.</exception>
    public UnicodePdfCMapDiagnostic(
        int mappingIndex,
        int characterCode,
        int codePoint,
        UnicodePdfCMapStatus status,
        string rule,
        string message)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(message);

        MappingIndex = mappingIndex;
        CharacterCode = characterCode;
        CodePoint = codePoint;
        Status = status;
        Rule = rule;
        Message = message;
    }
}
