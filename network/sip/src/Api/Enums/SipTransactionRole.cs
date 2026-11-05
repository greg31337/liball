namespace sip;

/// <summary>Identifies the role of a SIP transaction.</summary>
public enum SipTransactionRole
{
    /// <summary>The role is unknown.</summary>
    Unknown = 0,
    /// <summary>A client transaction.</summary>
    Client = 1,
    /// <summary>A server transaction.</summary>
    Server = 2
}
