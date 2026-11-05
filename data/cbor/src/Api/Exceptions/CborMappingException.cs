namespace cbor;

/// <summary>
/// Reports a failure to map between a CBOR value and a runtime object.
/// </summary>
public class CborMappingException : System.Exception
{
    /// <summary>The stable failure code.</summary>
    public string ErrorCode;
    /// <summary>The encoded byte offset, or negative one when it is unavailable.</summary>
    public long ErrorOffset;
    /// <summary>The path to the value or runtime member associated with the failure.</summary>
    public string? ErrorPath;

    /// <summary>Creates a CBOR mapping exception with structured failure metadata.</summary>
    /// <param name="errorCode">The stable failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="errorOffset">The encoded byte offset, or negative one when unavailable.</param>
    /// <param name="errorPath">The value or member path, or null when unavailable.</param>
    public CborMappingException(string errorCode, string message, long errorOffset, string? errorPath) : base(message)
    {
        ErrorCode = errorCode;
        ErrorOffset = errorOffset;
        ErrorPath = errorPath;
    }
}
