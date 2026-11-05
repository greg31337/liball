namespace sip;

/// <summary>Holds an application authorization-policy decision.</summary>
public class SipAuthorizationDecision
{
    /// <summary>Indicates that the operation is authorized.</summary>
    public bool Allowed;
    /// <summary>The established identity.</summary>
    public string? Identity;
    /// <summary>The decision reason.</summary>
    public string? Reason;
    /// <summary>The scopes applied to the decision.</summary>
    public string[] Scopes = new string[0];
    /// <summary>The SIP rejection status code, or zero when not applicable.</summary>
    public int StatusCode;
}
