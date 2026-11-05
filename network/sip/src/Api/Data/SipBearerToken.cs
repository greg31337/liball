namespace sip;

/// <summary>Holds an external bearer token and validation metadata.</summary>
public class SipBearerToken
{
    /// <summary>The opaque bearer token.</summary>
    public string? Token;
    /// <summary>The authentication realm.</summary>
    public string? Realm;
    /// <summary>The trusted HTTPS authorization-server address.</summary>
    public string? AuthorizationServer;
    /// <summary>The required or granted scopes.</summary>
    public string[] Scopes = new string[0];
    /// <summary>The established subject identity.</summary>
    public string? Subject;
    /// <summary>The token validation error code.</summary>
    public string? Error;
    /// <summary>The ordered extension authentication parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];
}
