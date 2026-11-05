namespace sip;

/// <summary>Describes selection of a registered binding, flow, and Path route set.</summary>
public class SipOutboundRouteRequest
{
    /// <summary>The candidate location-service bindings.</summary>
    public SipRegistrationBinding[] Bindings = new SipRegistrationBinding[0];
    /// <summary>The destination URI, including a GRUU when one was addressed.</summary>
    public SipUri? Destination;
    /// <summary>An optional SIP instance identifier used to restrict selection.</summary>
    public string? InstanceId;
    /// <summary>The current monotonic time used to exclude expired bindings.</summary>
    public long NowMilliseconds;
    /// <summary>Requires a SIPS routing target.</summary>
    public bool RequireSecure;
    /// <summary>Allows a push request when an Outbound flow is unavailable.</summary>
    public bool AllowPush;
    /// <summary>The application correlation identifier.</summary>
    public string? CorrelationId;
}
