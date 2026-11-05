namespace sip;

/// <summary>Indicates that a SIP operation was rejected by policy.</summary>
public class SipPolicyException : System.Exception
{
    /// <summary>The related SIP status code, or zero when none is available.</summary>
    public int StatusCode;
    /// <summary>The policy rejection reason, when available.</summary>
    public string? Reason;

    /// <summary>Initializes a SIP policy failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="statusCode">The related SIP status code, or zero when none is available.</param>
    /// <param name="reason">The policy rejection reason, when available.</param>
    public SipPolicyException(string message, int statusCode, string? reason) : base(message)
    {
        StatusCode = statusCode;
        Reason = reason;
    }
}
