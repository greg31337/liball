namespace pop3;

/// <summary>Identifies a POP3 authentication method or selection policy.</summary>
public enum Pop3AuthenticationMethod
{
    /// <summary>Select an acceptable authentication method automatically.</summary>
    Automatic = 0,

    /// <summary>Authenticate with the USER and PASS commands.</summary>
    UserPassword = 1,

    /// <summary>Authenticate with the legacy APOP command.</summary>
    Apop = 2,

    /// <summary>Authenticate with a caller-supplied SASL mechanism.</summary>
    Sasl = 3,

    /// <summary>Authenticate with the built-in SASL PLAIN mechanism.</summary>
    SaslPlain = 4
}
