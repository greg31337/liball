namespace sip;

/// <summary>
/// Holds a parsed Retry-After delay, optional comment, and parameters.
/// </summary>
public class SipRetryAfterValue
{
    /// <summary>The retry delay in seconds.</summary>
    public int Seconds;

    /// <summary>The decoded optional comment.</summary>
    public string? Comment;

    /// <summary>The optional duration in seconds, or negative one when absent.</summary>
    public int DurationSeconds = -1;

    /// <summary>The ordered Retry-After parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];
}
