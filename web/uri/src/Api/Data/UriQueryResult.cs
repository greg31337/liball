namespace uri;

/// <summary>
/// Describes the exception-less outcome of a query codec or parameter operation.
/// </summary>
public class UriQueryResult
{
    /// <summary>Whether the requested query operation succeeded.</summary>
    public bool Success;

    /// <summary>The ordered decoded parameters, or an empty array after failure.</summary>
    public UriQueryParameter[] Parameters;

    /// <summary>The encoded query text, or an empty string after failure.</summary>
    public string Text;

    /// <summary>The encoded text length, or zero after failure.</summary>
    public int Length;

    /// <summary>The pair delimiter selected by the operation.</summary>
    public char PairDelimiter;

    /// <summary>The failure diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a query operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="parameters">The ordered decoded parameters.</param>
    /// <param name="text">The encoded query text.</param>
    /// <param name="length">The encoded text length.</param>
    /// <param name="pairDelimiter">The selected pair delimiter.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public UriQueryResult(
        bool success,
        UriQueryParameter[] parameters,
        string text,
        int length,
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Parameters = parameters;
        Text = text;
        Length = length;
        PairDelimiter = pairDelimiter;
        Diagnostic = diagnostic;
    }
}
