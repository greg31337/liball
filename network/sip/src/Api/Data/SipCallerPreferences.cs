namespace sip;

/// <summary>Holds RFC 3841 caller feature and request-handling preferences.</summary>
public class SipCallerPreferences
{
    /// <summary>The preferred callee feature sets from Accept-Contact.</summary>
    public SipExtensionHeaderValue[] AcceptContacts =
        new SipExtensionHeaderValue[0];
    /// <summary>The unwanted callee feature sets from Reject-Contact.</summary>
    public SipExtensionHeaderValue[] RejectContacts =
        new SipExtensionHeaderValue[0];
    /// <summary>The requested proxy processing directives.</summary>
    public string[] RequestDispositions = new string[0];
}
