namespace sip;

/// <summary>Identifies the local role in a SIP dialog.</summary>
public enum SipDialogRole
{
    /// <summary>The role is unknown.</summary>
    Unknown = 0,
    /// <summary>The local endpoint is the user-agent client.</summary>
    Uac = 1,
    /// <summary>The local endpoint is the user-agent server.</summary>
    Uas = 2
}
