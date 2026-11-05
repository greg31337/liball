namespace args;

/// <summary>
/// Describes a structured non-error notice produced while processing command-line input.
/// </summary>
public class ArgDiagnostic
{
    /// <summary>
    /// The stable machine-readable diagnostic code.
    /// </summary>
    public string Code;
    /// <summary>
    /// The kind of symbol associated with this diagnostic.
    /// </summary>
    public ArgSymbolKind SymbolKind;
    /// <summary>
    /// The developer-readable diagnostic message.
    /// </summary>
    public string Message;
    /// <summary>
    /// The zero-based source token index, or -1 when no source token exists.
    /// </summary>
    public int TokenIndex;
    /// <summary>
    /// The canonical symbol name associated with this diagnostic.
    /// </summary>
    public string SymbolName;

    internal ArgDiagnostic(string code, ArgSymbolKind symbolKind, string message, int tokenIndex, string symbolName)
    {
        Code = code;
        SymbolKind = symbolKind;
        Message = message;
        TokenIndex = tokenIndex;
        SymbolName = symbolName;
    }
}
