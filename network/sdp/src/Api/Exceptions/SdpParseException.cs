namespace sdp;

/// <summary>
/// Represents a throwing parser entry-point failure caused by invalid configuration or unrecoverable processing state.
/// </summary>
public class SdpParseException : global::System.Exception
{
    /// <summary>Contains the stable failure code.</summary>
    public string Code = string.Empty;

    /// <summary>
    /// Initializes a parser exception without including remote SDP values in its message.
    /// </summary>
    /// <param name="code">The stable failure code.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when unavailable.</param>
    public SdpParseException(string code, global::System.Exception? innerException)
        : base("SDP parsing failed.", innerException)
    {
        Code = code;
    }
}
