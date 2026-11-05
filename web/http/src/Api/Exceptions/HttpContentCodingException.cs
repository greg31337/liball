namespace http;

/// <summary>
/// Reports a content-coding transformation failure.
/// </summary>
public class HttpContentCodingException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The content-coding token associated with the failure.</summary>
    public string Coding;

    /// <summary>Creates an HTTP content-coding exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="coding">The content-coding token associated with the failure.</param>
    public HttpContentCodingException(string message, string diagnosticCode, string coding) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        Coding = coding;
    }
}
