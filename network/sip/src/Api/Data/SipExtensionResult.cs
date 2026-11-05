namespace sip;

/// <summary>Holds the outcome of extension dispatch.</summary>
public class SipExtensionResult
{
    /// <summary>Indicates that an extension handled the request.</summary>
    public bool Handled;
    /// <summary>The generated response.</summary>
    public SipMessage? Response;
    /// <summary>An application event token.</summary>
    public string? Event;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
}
