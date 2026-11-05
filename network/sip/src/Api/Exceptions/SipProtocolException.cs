namespace sip;

/// <summary>Indicates that a SIP protocol rule was violated.</summary>
public class SipProtocolException : System.Exception
{
    /// <summary>The related SIP status code, or zero when none is available.</summary>
    public int StatusCode;
    /// <summary>The related SIP method token, when available.</summary>
    public string? MethodToken;

    /// <summary>Initializes a SIP protocol failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="statusCode">The related SIP status code, or zero when none is available.</param>
    /// <param name="methodToken">The related SIP method token, when available.</param>
    public SipProtocolException(string message, int statusCode, string? methodToken) : base(message)
    {
        StatusCode = statusCode;
        MethodToken = methodToken;
    }
}
