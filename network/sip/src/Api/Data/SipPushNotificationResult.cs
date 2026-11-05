namespace sip;

/// <summary>Holds an application push-notification decision.</summary>
public class SipPushNotificationResult
{
    /// <summary>Indicates that the push request was accepted by application policy.</summary>
    public bool Accepted;
    /// <summary>The SIP failure status when the request was not accepted.</summary>
    public int StatusCode;
    /// <summary>The application decision reason.</summary>
    public string? Reason;
    /// <summary>The application correlation identifier.</summary>
    public string? CorrelationId;
}
