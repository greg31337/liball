namespace http;

/// <summary>
/// Reports a general HTTP protocol invariant violation.
/// </summary>
public class HttpProtocolException : System.Exception
{
    /// <summary>
    /// A stable machine-readable diagnostic identifier.
    /// </summary>
    public string DiagnosticCode;

    /// <summary>
    /// Creates an HTTP protocol exception.
    /// </summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    public HttpProtocolException(string message, string diagnosticCode) : base(message)
    {
        DiagnosticCode = diagnosticCode;
    }
}
