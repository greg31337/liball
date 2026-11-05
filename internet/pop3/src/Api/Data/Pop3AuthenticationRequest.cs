namespace pop3;

/// <summary>Describes the authentication method requested for a POP3 session.</summary>
public class Pop3AuthenticationRequest
{
    /// <summary>The authentication method or selection policy to use.</summary>
    public Pop3AuthenticationMethod Method;

    /// <summary>The credentials used by password-based authentication methods.</summary>
    public Pop3Credentials Credentials = new Pop3Credentials();

    /// <summary>The APOP values to validate, or <c>null</c> when APOP is not requested.</summary>
    public Pop3ApopRequest? Apop;

    /// <summary>A single SASL client used when the ordered client list is empty, or <c>null</c>.</summary>
    public IPop3SaslClient? SaslClient;

    /// <summary>Caller-supplied SASL clients in automatic-selection preference order.</summary>
    public IPop3SaslClient[] SaslClients = new IPop3SaslClient[0];

    /// <summary>Indicates whether credentials may be sent over an unprotected transport.</summary>
    public bool AllowUnprotectedCredentials;

    /// <summary>Indicates whether the legacy APOP mechanism may be selected automatically.</summary>
    public bool AllowApop;
}
