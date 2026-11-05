namespace cbor;

/// <summary>
/// Reports a failure to parse or format CBOR diagnostic notation.
/// </summary>
public class CborDiagnosticException : System.Exception
{
    /// <summary>The stable failure code.</summary>
    public string ErrorCode;
    /// <summary>The source character offset, or negative one when it is unavailable.</summary>
    public long ErrorOffset;
    /// <summary>The path to the value associated with the failure.</summary>
    public string? ErrorPath;

    /// <summary>Creates a CBOR diagnostic exception with structured failure metadata.</summary>
    /// <param name="errorCode">The stable failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="errorOffset">The source character offset, or negative one when unavailable.</param>
    /// <param name="errorPath">The value path, or null when unavailable.</param>
    public CborDiagnosticException(string errorCode, string message, long errorOffset, string? errorPath) : base(message)
    {
        ErrorCode = errorCode;
        ErrorOffset = errorOffset;
        ErrorPath = errorPath;
    }
}
