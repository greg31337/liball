namespace sip;

/// <summary>Holds the opaque information needed by an application push provider.</summary>
public class SipPushNotificationRequest
{
    /// <summary>The push notification service provider.</summary>
    public string? Provider;
    /// <summary>The provider-specific parameter.</summary>
    public string? Parameter;
    /// <summary>The provider resource identifier.</summary>
    public string? ResourceId;
    /// <summary>The proxy-unique registration reference.</summary>
    public string? RegistrationReference;
    /// <summary>The detached dormant registration binding.</summary>
    public SipRegistrationBinding? Binding;
    /// <summary>The originally addressed destination.</summary>
    public SipUri? Destination;
    /// <summary>The application correlation identifier.</summary>
    public string? CorrelationId;
}
