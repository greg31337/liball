namespace sip;

/// <summary>
/// Describes one issue found while validating SIP data.
/// </summary>
public class SipValidationIssue
{
    /// <summary>The severity of the issue.</summary>
    public SipValidationSeverity Severity;

    /// <summary>The stable issue code.</summary>
    public string? Code;

    /// <summary>The explanatory issue message.</summary>
    public string? Message;

    /// <summary>The input byte offset, or negative one when unavailable.</summary>
    public int ByteOffset = -1;

    /// <summary>The logical path to the affected value.</summary>
    public string? Path;

    /// <summary>The affected header field name, or null when the issue is not header-specific.</summary>
    public string? HeaderName;
}
