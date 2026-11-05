namespace sip;

/// <summary>Indicates that SIP data could not be parsed.</summary>
public class SipParseException : System.Exception
{
    /// <summary>The zero-based byte offset at which parsing failed.</summary>
    public int ByteOffset;
    /// <summary>A parser-specific error code, when available.</summary>
    public string? ErrorCode;

    /// <summary>Initializes a SIP parsing failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="byteOffset">The zero-based byte offset at which parsing failed.</param>
    /// <param name="errorCode">A parser-specific error code, when available.</param>
    public SipParseException(string message, int byteOffset, string? errorCode) : base(message)
    {
        ByteOffset = byteOffset;
        ErrorCode = errorCode;
    }
}
