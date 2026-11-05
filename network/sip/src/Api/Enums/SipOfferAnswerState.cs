namespace sip;

/// <summary>Identifies the state of an offer/answer exchange.</summary>
public enum SipOfferAnswerState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>No offer is outstanding.</summary>
    Stable = 1,
    /// <summary>A local offer awaits an answer.</summary>
    LocalOfferPending = 2,
    /// <summary>A remote offer awaits an answer.</summary>
    RemoteOfferPending = 3,
    /// <summary>The exchange completed.</summary>
    Completed = 4,
    /// <summary>The exchange failed.</summary>
    Failed = 5,
    /// <summary>An offerless remote request requires a local offer.</summary>
    LocalOfferRequired = 6,
    /// <summary>An offerless local request requires a remote offer.</summary>
    RemoteOfferRequired = 7
}
