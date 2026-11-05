namespace sip;

/// <summary>Holds the result of matching caller preferences to one callee contact.</summary>
public class SipCallerPreferenceMatch
{
    /// <summary>Indicates that the contact remains eligible.</summary>
    public bool Eligible;
    /// <summary>Indicates that a Reject-Contact feature set explicitly matched.</summary>
    public bool Rejected;
    /// <summary>Indicates that every scored preference was explicitly matched.</summary>
    public bool ExplicitMatch;
    /// <summary>Indicates that the contact advertised no callee feature parameters.</summary>
    public bool Immune;
    /// <summary>The aggregate caller preference score in thousandths.</summary>
    public int ScoreThousandths;
    /// <summary>The matching Accept-Contact feature sets.</summary>
    public SipExtensionHeaderValue[] MatchedAcceptContacts =
        new SipExtensionHeaderValue[0];
    /// <summary>The matching Reject-Contact feature sets.</summary>
    public SipExtensionHeaderValue[] MatchedRejectContacts =
        new SipExtensionHeaderValue[0];
    /// <summary>The request-disposition directives observed on the request.</summary>
    public string[] RequestDispositions = new string[0];
    /// <summary>The failure description, or null when matching was performed.</summary>
    public string? ErrorMessage;
}
