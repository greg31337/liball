namespace http;

/// <summary>
/// Reports invalid HTTP field syntax or semantics.
/// </summary>
public class HttpFieldException : System.Exception
{
    /// <summary>A stable machine-readable diagnostic identifier.</summary>
    public string DiagnosticCode;
    /// <summary>The field name associated with the failure.</summary>
    public string FieldName;

    /// <summary>Creates an HTTP field exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="diagnosticCode">The stable machine-readable diagnostic identifier.</param>
    /// <param name="fieldName">The field name associated with the failure.</param>
    public HttpFieldException(string message, string diagnosticCode, string fieldName) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        FieldName = fieldName;
    }
}
