namespace sip;

/// <summary>Identifies the call-control operation represented by a SIP request.</summary>
public enum SipTransferOperation
{
    /// <summary>No operation has been selected.</summary>
    None = 0,
    /// <summary>A REFER request targets a resource without replacing a dialog.</summary>
    BlindTransfer = 1,
    /// <summary>A REFER or INVITE request replaces a correlated dialog.</summary>
    AttendedTransfer = 2,
    /// <summary>An INVITE request picks up a correlated dialog by replacing it.</summary>
    CallPickup = 3,
    /// <summary>An INVITE request joins a correlated dialog.</summary>
    DialogJoin = 4,
    /// <summary>A dialog-forming request proves awareness of a target dialog.</summary>
    TargetDialogAssociation = 5
}
