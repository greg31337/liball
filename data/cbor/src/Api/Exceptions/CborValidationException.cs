namespace cbor;

/// <summary>
/// Reports an exceptional failure while validating CBOR data or values.
/// </summary>
public class CborValidationException : System.Exception
{
    /// <summary>The stable failure code.</summary>
    public string ErrorCode;
    /// <summary>The absolute encoded byte offset, or negative one when it is unavailable.</summary>
    public long ErrorOffset;
    /// <summary>The path to the value associated with the failure.</summary>
    public string? ErrorPath;

    /// <summary>Creates a CBOR validation exception with structured failure metadata.</summary>
    /// <param name="errorCode">The stable failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="errorOffset">The absolute encoded byte offset, or negative one when unavailable.</param>
    /// <param name="errorPath">The value path, or null when unavailable.</param>
    public CborValidationException(string errorCode, string message, long errorOffset, string? errorPath) : base(message)
    {
        ErrorCode = errorCode;
        ErrorOffset = errorOffset;
        ErrorPath = errorPath;
    }
}
