namespace cbor;

/// <summary>Reports a failure to convert between CBOR and JSON.</summary>
public class CborJsonException : System.Exception
{
    /// <summary>The stable failure code.</summary>
    public string ErrorCode;
    /// <summary>The source character offset, or negative one when unavailable.</summary>
    public long ErrorOffset;
    /// <summary>The path to the value associated with the failure.</summary>
    public string? ErrorPath;

    /// <summary>Creates a CBOR JSON exception with structured failure metadata.</summary>
    /// <param name="errorCode">The stable failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="errorOffset">The source character offset, or negative one when unavailable.</param>
    /// <param name="errorPath">The value path, or null when unavailable.</param>
    public CborJsonException(
        string errorCode,
        string message,
        long errorOffset,
        string? errorPath) : base(message)
    {
        ErrorCode = errorCode;
        ErrorOffset = errorOffset;
        ErrorPath = errorPath;
    }
}
