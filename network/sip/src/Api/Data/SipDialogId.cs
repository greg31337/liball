namespace sip;

/// <summary>Holds the Call-ID and local and remote tags identifying a dialog.</summary>
public class SipDialogId
{
    /// <summary>The dialog Call-ID.</summary>
    public string? CallId;
    /// <summary>The local tag.</summary>
    public string? LocalTag;
    /// <summary>The remote tag.</summary>
    public string? RemoteTag;
}
