namespace http;

/// <summary>
/// Reports malformed HTTP syntax at a known input position.
/// </summary>
public class HttpParseException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The zero-based input position at which parsing failed.</summary>
    public int Position;

    /// <summary>Creates an HTTP parsing exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="position">The zero-based input position at which parsing failed.</param>
    public HttpParseException(string message, string diagnosticCode, int position) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        Position = position;
    }
}
