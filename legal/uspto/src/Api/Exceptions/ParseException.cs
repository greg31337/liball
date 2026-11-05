namespace uspto;

/// <summary>
/// Reports malformed or incompatible structured data.
/// </summary>
public class ParseException : global::System.Exception
{
    /// <summary>The expected document representation.</summary>
    public DocumentFormat Format;

    /// <summary>A credential-free source or file name.</summary>
    public string? SourceName;

    /// <summary>The JSON path, XML element path, or delimited field associated with the failure.</summary>
    public string? DataPath;

    /// <summary>The zero-based byte or character offset associated with the failure.</summary>
    public long Offset;

    /// <summary>Whether <see cref="Offset"/> contains a value.</summary>
    public bool HasOffset;

    /// <summary>The one-based source line associated with the failure.</summary>
    public int LineNumber;

    /// <summary>Whether <see cref="LineNumber"/> contains a value.</summary>
    public bool HasLineNumber;

    /// <summary>Initializes a parse exception and its bounded source location.</summary>
    /// <param name="message">The parse failure message.</param>
    /// <param name="format">The expected document representation.</param>
    /// <param name="sourceName">A credential-free source or file name.</param>
    /// <param name="dataPath">The structured data path associated with the failure.</param>
    /// <param name="offset">The source offset, if present.</param>
    /// <param name="hasOffset">Whether <c>offset</c> is present.</param>
    /// <param name="lineNumber">The source line number, if present.</param>
    /// <param name="hasLineNumber">Whether <c>lineNumber</c> is present.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public ParseException(string message, DocumentFormat format, string? sourceName, string? dataPath, long offset, bool hasOffset, int lineNumber, bool hasLineNumber, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Format = format;
        SourceName = sourceName;
        DataPath = dataPath;
        Offset = offset;
        HasOffset = hasOffset;
        LineNumber = lineNumber;
        HasLineNumber = hasLineNumber;
    }
}
