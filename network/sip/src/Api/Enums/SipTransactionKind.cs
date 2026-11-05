namespace sip;

/// <summary>Identifies a SIP transaction state-machine family.</summary>
public enum SipTransactionKind
{
    /// <summary>The transaction kind is unknown.</summary>
    Unknown = 0,
    /// <summary>An INVITE transaction.</summary>
    Invite = 1,
    /// <summary>A non-INVITE transaction.</summary>
    NonInvite = 2
}
