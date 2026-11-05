namespace sdp;

/// <summary>
/// Represents a throwing negotiation entry-point failure caused by invalid configuration or unrecoverable processing state.
/// </summary>
public class SdpNegotiationException : global::System.Exception
{
    /// <summary>Contains the stable failure code.</summary>
    public string Code = string.Empty;

    /// <summary>
    /// Initializes a negotiation exception without including offer, answer, or credential values in its message.
    /// </summary>
    /// <param name="code">The stable failure code.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when unavailable.</param>
    public SdpNegotiationException(string code, global::System.Exception? innerException)
        : base("SDP negotiation failed.", innerException)
    {
        Code = code;
    }
}
