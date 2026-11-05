namespace sip;

/// <summary>Holds an ordered location-service query result.</summary>
public class SipLocationResult
{
    /// <summary>Indicates that the query completed successfully.</summary>
    public bool Success;
    /// <summary>The ordered matching bindings.</summary>
    public SipRegistrationBinding[] Bindings = new SipRegistrationBinding[0];
    /// <summary>The ordered bindings removed by an expiry operation.</summary>
    public SipRegistrationBinding[] ExpiredBindings = new SipRegistrationBinding[0];
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
}
