namespace sip;

/// <summary>Holds the result of an Outbound, GRUU, push, or dialog-target operation.</summary>
public class SipOutboundResult
{
    /// <summary>Indicates that the operation completed successfully.</summary>
    public bool Success;
    /// <summary>The recommended SIP failure status, or 200 on success.</summary>
    public int StatusCode;
    /// <summary>The generated detached Contact.</summary>
    public SipContactValue? Contact;
    /// <summary>The selected detached URI.</summary>
    public SipUri? Uri;
    /// <summary>The selected detached proxy target.</summary>
    public SipProxyTarget? Target;
    /// <summary>The selected detached registration binding.</summary>
    public SipRegistrationBinding? Binding;
    /// <summary>The updated detached dialog snapshot.</summary>
    public SipDialogSnapshot? Dialog;
    /// <summary>The option tags required by the generated registration Contact.</summary>
    public string[] SupportedOptionTags = new string[0];
    /// <summary>Indicates that a GRUU rather than the ordinary Contact URI was selected.</summary>
    public bool GruuSelected;
    /// <summary>Indicates that the selected GRUU was temporary.</summary>
    public bool TemporaryGruuSelected;
    /// <summary>Indicates that matching push support was advertised by the network.</summary>
    public bool PushSupported;
    /// <summary>Indicates that an application push request was accepted.</summary>
    public bool PushRequested;
    /// <summary>The detached application push decision.</summary>
    public SipPushNotificationResult? PushResult;
    /// <summary>Indicates that a target-refresh message changed the remote target.</summary>
    public bool TargetUpdated;
    /// <summary>The failure description, or null on success.</summary>
    public string? ErrorMessage;
}
