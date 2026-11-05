namespace http;

/// <summary>
/// Reports invalid or ambiguous HTTP/1 message framing.
/// </summary>
public class Http1FramingException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The framing classification associated with the failure.</summary>
    public Http1BodyFraming BodyFraming;

    /// <summary>Creates an HTTP/1 framing exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="bodyFraming">The framing classification associated with the failure.</param>
    public Http1FramingException(
        string message,
        string diagnosticCode,
        Http1BodyFraming bodyFraming) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        BodyFraming = bodyFraming;
    }
}
