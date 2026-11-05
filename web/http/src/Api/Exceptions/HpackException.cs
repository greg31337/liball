namespace http;

/// <summary>
/// Reports malformed HPACK input or invalid compression state.
/// </summary>
public class HpackException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The zero-based compressed input position associated with the failure.</summary>
    public int Position;

    /// <summary>Creates an HPACK exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="position">The zero-based compressed input position.</param>
    public HpackException(string message, string diagnosticCode, int position) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        Position = position;
    }
}
