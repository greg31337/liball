namespace sip;

/// <summary>Identifies a SIP dialog state.</summary>
public enum SipDialogState
{
    /// <summary>No dialog state is assigned.</summary>
    None = 0,
    /// <summary>The dialog is early.</summary>
    Early = 1,
    /// <summary>The dialog is confirmed.</summary>
    Confirmed = 2,
    /// <summary>The dialog is terminated.</summary>
    Terminated = 3
}
