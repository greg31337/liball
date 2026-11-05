namespace sip;

/// <summary>Holds one registered contact binding and routing metadata.</summary>
public class SipRegistrationBinding
{
    /// <summary>The address of record.</summary>
    public SipUri? AddressOfRecord;
    /// <summary>The registered contact.</summary>
    public SipContactValue? Contact;
    /// <summary>The registration Call-ID.</summary>
    public string? CallId;
    /// <summary>The registration CSeq.</summary>
    public int CSeq = -1;
    /// <summary>The monotonic expiry timestamp in milliseconds.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>The quality value in thousandths.</summary>
    public int QualityThousandths = 1000;
    /// <summary>The observed source endpoint.</summary>
    public SipEndpointAddress? ReceivedAddress;
    /// <summary>The stored Path route set.</summary>
    public SipRouteValue[] Path = new SipRouteValue[0];
    /// <summary>The stored Service-Route set.</summary>
    public SipRouteValue[] ServiceRoute = new SipRouteValue[0];
    /// <summary>The associated outbound flow.</summary>
    public SipFlow? Flow;
    /// <summary>The outbound instance identifier.</summary>
    public string? InstanceId;
    /// <summary>The registration identifier, or negative one when absent.</summary>
    public int RegistrationId = -1;
}
