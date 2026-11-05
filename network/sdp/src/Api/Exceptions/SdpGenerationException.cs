namespace sdp;

/// <summary>
/// Represents a failure reported by a throwing SDP generation entry point.
/// </summary>
public class SdpGenerationException : global::System.Exception
{
    /// <summary>Contains the stable failure code.</summary>
    public string Code = string.Empty;

    /// <summary>
    /// Initializes a generation exception without including SDP field values in its message.
    /// </summary>
    /// <param name="code">The stable failure code.</param>
    /// <param name="innerException">The underlying exception, or <c>null</c> when unavailable.</param>
    public SdpGenerationException(string code, global::System.Exception? innerException)
        : base("SDP generation failed.", innerException)
    {
        Code = code;
    }
}
