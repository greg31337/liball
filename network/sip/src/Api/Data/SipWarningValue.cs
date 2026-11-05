namespace sip;

/// <summary>
/// Holds one parsed Warning header field value.
/// </summary>
public class SipWarningValue
{
    /// <summary>The three-digit warning code.</summary>
    public int Code;

    /// <summary>The warning agent host, host-port, or pseudonym.</summary>
    public string? Agent;

    /// <summary>The decoded warning text.</summary>
    public string? Text;
}
