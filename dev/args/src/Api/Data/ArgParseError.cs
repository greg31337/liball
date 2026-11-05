namespace args;

/// <summary>
/// Describes a structured error found while parsing or validating command-line input.
/// </summary>
public class ArgParseError
{
    /// <summary>
    /// The stable machine-readable error code.
    /// </summary>
    public string Code;
    /// <summary>
    /// The kind of symbol represented by this object.
    /// </summary>
    public ArgSymbolKind SymbolKind;
    /// <summary>
    /// The developer-readable explanation of the error.
    /// </summary>
    public string Message;
    /// <summary>
    /// The zero-based source token index, or -1 when no source token exists.
    /// </summary>
    public int TokenIndex;
    /// <summary>
    /// The canonical symbol name associated with this object.
    /// </summary>
    public string SymbolName;

    /// <summary>
    /// Creates a structured parse or validation error.
    /// </summary>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <param name="symbolKind">The associated symbol category.</param>
    /// <param name="message">The developer-readable error explanation.</param>
    /// <param name="tokenIndex">The zero-based source token index, or -1 when no source token exists.</param>
    /// <param name="symbolName">The canonical associated symbol name.</param>
    public ArgParseError(string code, ArgSymbolKind symbolKind, string message, int tokenIndex, string symbolName)
    {
        Code = code;
        SymbolKind = symbolKind;
        Message = message;
        TokenIndex = tokenIndex;
        SymbolName = symbolName;
    }
}
